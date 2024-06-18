// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Metal;
using System;
using MetalKit;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Holds the framebuffer or the render target's render pass descriptor and
    /// corresponding render encoder that the <see cref="GraphicsDevice"/> uses during rendering.
    ///
    /// A render pass involving the framebuffer can be interleaved with a render target's
    /// render pass. <see cref="https://whackylabs.com/metal/2020/04/30/multiple-objects-single-frame-metal/"/>
    /// for an overview. A render pass involves encoding commands in a <see cref="IMTLRenderCommandEncoder"/>
    /// obtained from a <see cref="IMTLCommandBuffer"/> for a framebuffer <see cref="MTKView.CurrentDrawable"/>
    /// or a render target <see cref="IMTLTexture"/>.
    ///
    /// |--Framebuffer--| |----RenderTarget1----| |---Framebuffer---| |---Present Drawable->Commit---|
    ///   Clear | Draw1     Clear | Draw2             Load | Draw 3
    ///
    /// Each render encoder encodes
    ///  (a) a Clear/Load for the render pass texture (framebuffer / render target)
    ///  (b) a series of Draw calls (vertices+textures for instances) along with states (clear color etc).
    /// Begin/End will take care of ensuring the render encoder is flushed into the command buffer correctly.
    /// 
    /// NOTE: If you see pink/magenta in the app it means a command buffer was created without a
    ///       render pass/render encoder. This is bad because the OS does not know what to show, so it
    ///       visibly shows an 'error' color for us to debug! There is some fallback protection in the code
    ///       for <see cref="MetalRenderPass"/> to ensure this does not happen though.
    /// 
    /// </summary>
    public class MetalRenderPass : IDisposable
    {
        /// Each render pass is unique and switching render passes is expensive, so hold an id
        /// to compare render passes in a stable manenr.
        private readonly MetalId<MetalRenderPass> _Id = new();

        private readonly bool _isFramebuffer;
        private readonly MTLLoadAction _loadAction = MTLLoadAction.DontCare;

        /// <summary>
        /// Number of render passes for this current frame (mainly for the framebuffer).
        /// Helps set the correct <see cref="MTLLoadAction"/> for the render pass descriptor.
        /// </summary>
        private int _passCount;

        /// <summary>
        /// Descriptor from which a render encoder is created.
        /// This is initialized per-frame from the View's framebuffer descriptor or once from a render target.
        /// </summary>
        internal MTLRenderPassDescriptor RenderPassDescriptor { get; private set; }

        /// <summary>
        /// Render encoder that holds the current render pass data. When the render
        /// encoding ends, the command buffer is ready to be submitted to the GPU.
        /// This is initialized at least once per frame for the framebuffer/render target's render pass.
        /// </summary>
        internal IMTLRenderCommandEncoder RenderEncoder { get; private set; }

        /// <summary>
        /// Notes the current clear color for this render pass. If the caller requested to clear
        /// the contents, then when the <see cref="BeginEncoding"/> starts, the <see cref="MTLLoadAction"/>
        /// will be set to clear contents with the correct <see cref="_clearColor"/>. For
        /// subsequent render passes in the same frame, we will use the requested <see cref="MTLLoadAction"/>
        /// i.e. Load existing contents, Don't care or Clear contents with color.
        /// This ensures that we process the <code>Clear</code> requests while still respecting the
        /// render pass requirements in the same frame.
        /// </summary>
        private MTLClearColor _clearColor = new MTLClearColor(0, 0, 0, 0);

        /// <summary>
        /// The following may be set at anytime during a <see cref="Game.Tick"/> pass.
        /// However, they will be applied only when a render encoder is created. Any
        /// state changes in the midst will be lost. Because MonoGame favors OpenGL/DirectX 9
        /// era pipeline state minimization, the Metal implementation can guarantee that
        /// there are no interleaved state changes _within_ a render pass encoding.
        ///
        /// NOTE: The overall pipeline state is managed elsewhere (that maps shaders and other
        /// heavyweight pipeline states etc). See <see cref="GraphicsDevice._PreApplyState"/>
        /// and <see cref="MetalPipelineState"/> for more details. Those objects are even
        /// more expensive than a render pass and must be created sparingly (a "few" per `Game`).
        /// </summary>
        private bool _clearTargetsOnBegin = false;

        private IMTLDepthStencilState _depthStencilState;
        private MTLScissorRect _scissorRect;
        private bool _scissorRectSet = false;

        private bool _isViewDataInit;
        private UIntPtr _sampleCount;
        internal UIntPtr SampleCount => _sampleCount;
        private MTLPixelFormat _depthPixelFormat;
        internal MTLPixelFormat DepthPixelFormat => _depthPixelFormat;
        private MTLPixelFormat _colorPixelFormat;
        internal MTLPixelFormat ColorPixelFormat => _colorPixelFormat;
        private MTLPixelFormat _stencilPixelFormat;
        internal MTLPixelFormat StencilPixelFormat => _stencilPixelFormat;
        public bool IsFramebufferPass => _isFramebuffer;

        /// <summary>
        /// Initializes a render pass for the framebuffer backing the <param name="view"></param>.
        /// </summary>
        internal MetalRenderPass(MTKView view)
        {
            _isFramebuffer = true;
            BeginFramebufferPass(view);
        }

        /// <summary>
        /// Initializes a render pass for a render target for the current frame using the
        /// provided <param name="renderPassDescriptor"/>.
        /// </summary>
        internal MetalRenderPass(MTLRenderPassDescriptor renderPassDescriptor)
        {
            RenderPassDescriptor = renderPassDescriptor;
            _sampleCount = 1;
            var attachment = renderPassDescriptor.ColorAttachments[0];
            var texture = attachment.Texture;
            _colorPixelFormat = texture?.PixelFormat ?? MTLPixelFormat.BGRA8Unorm;
            _depthPixelFormat = MTLPixelFormat.Invalid;
            _stencilPixelFormat = MTLPixelFormat.Invalid;

            _loadAction = attachment.LoadAction;
        }

        /// <summary>
        /// Begins encoding for the current render pass (framebuffer or render target) if
        /// it has not commenced already. (This is an idempotent method).
        ///
        /// <returns>true if begin encoding successfully begins the first encoding for this render pass. </returns>
        /// </summary>
        public bool BeginEncoding(GraphicsDevice device)
        {
            if (RenderEncoder == null && RenderPassDescriptor != null)
            {
                // Only create render encoder if we haven't begun encoding already.
                _CreateRenderEncoder(device);
#if DEBUG                
                if (RenderEncoder == null)
                {
                    GraphicsDebug.Spam("Unable to create render encoder.");
                }
                else
                {
                    GraphicsDebug.Spam($" ---- Begin Render Encoding {ToString()}");
                }
#endif                
                return RenderEncoder != null;

                // TODO: Set error if render encoder was not created?
            }

            return false;
        }

        /// <summary>
        /// Called once per render pass - assumes that there isn't an existing render encoder for this pass.
        /// </summary>
        private void _CreateRenderEncoder(GraphicsDevice device)
        {
            // Clear color on the render targets alone.
            var attach = RenderPassDescriptor.ColorAttachments[0];
            attach.ClearColor = _clearColor;
            // This seemingly innocuous logic here is the most important piece for this
            // render pass: We must correctly clear or load existing contents - otherwise
            // weird behavior arises (either flashing of content or outright blank screen).
            // See the class doc comment for more details.
            if (_isFramebuffer)
            {
                if (_passCount == 0) { attach.LoadAction = MTLLoadAction.Clear; }
                else { attach.LoadAction = MTLLoadAction.Load; }
            }
            else
            {
                // For render targets, we rely on if the caller called GraphicsDevice.Clear()
                // *after* setting this render target. If so, the first action must
                // clear the contents instead of loading previous contents.
                if (_clearTargetsOnBegin)
                {
                    attach.LoadAction = MTLLoadAction.Clear; 
                    GraphicsDebug.Spam($" --- Clearing on {ToString()}");
                }
                else { attach.LoadAction = _loadAction; }
            }

            attach.StoreAction = MTLStoreAction.Store;
            RenderEncoder = device.CommandBuffer.CreateRenderCommandEncoder(RenderPassDescriptor);
            if (RenderEncoder == null)
            {
                GraphicsDebug.Spam(" -- Error: No render encoder available.");
                return;
            }

            // Useful when using the Instruments Metal System Trace.
            RenderEncoder.Label = _isFramebuffer ? $"Framebuffer {_Id.Id}" : $"RenderTarget {_Id.Id}";
            RenderEncoder.SetBlendColor(device.BlendFactor.R / 255.0f, device.BlendFactor.G / 255.0f,
                device.BlendFactor.B / 255.0f, device.BlendFactor.A / 255.0f);

            if (_isFramebuffer)
            {
                // The following line does not work on iPhone mini (12/13) models.
                // Commenting it out until we need it.
                // if (_scissorRectSet) { RenderEncoder.SetScissorRect(_scissorRect); }
            }

            // Viewport etc will be set as the rendering continues.
            _clearTargetsOnBegin = false;
            GraphicsDebug.Spam($" --- Create Render Encoding {ToString()} scissor {_scissorRect}");
        }

        /// <summary>
        /// Ends encoding for the current render pass (framebuffer or render target) if
        /// it has commenced already. (This is an idempotent method).
        /// </summary>
        public void EndEncoding()
        {
            if (RenderEncoder != null)
            {
                GraphicsDebug.Spam($" ----- End Render Encoding {ToString()}");
                RenderEncoder.EndEncoding();
                RenderEncoder.Dispose();
                RenderEncoder = null;

                // Future passes for the current frame may need the framebuffer contents, so update state.
                ++_passCount;
            }
        }

        /// <summary>
        /// Begins a render pass for the current frame.
        /// The framebuffer will be cleared for the first render pass (for the current frame).
        /// The framebuffer will load its existing contents for other render passes.
        /// This allows interleaving render target render passes with framebuffer passes.
        /// </summary>
        public void BeginFramebufferPass(MTKView view)
        {
            if (!_isFramebuffer)
            {
                throw new Exception("Only framebuffer render passes can set framebuffer a render pass descriptor.");
            }

            GraphicsDebug.Spam($" ----- Begin FB pass {ToString()} ----");
            RenderPassDescriptor = view.CurrentRenderPassDescriptor;
            if (!_isViewDataInit)
            {
                _sampleCount = view.SampleCount;
                _depthPixelFormat = view.DepthStencilPixelFormat;
                _colorPixelFormat = view.ColorPixelFormat;
                _stencilPixelFormat = view.DepthStencilPixelFormat;
            }

            _passCount = 0;
        }

        /// <summary>
        /// Ends the render encoder pass and fills in the underlying command buffer.
        /// Ready for the GPU to take in the final command buffers including encoded
        /// render targets' render passes.
        /// </summary>
        public void EndFramebufferPass(GraphicsDevice device)
        {
            if (!_isFramebuffer)
            {
                throw new Exception("Only framebuffer render passes can set framebuffer a render pass descriptor.");
            }

            // Ensure we never leave a frame hanging!
            // If we did not encode into the command queue, then we will see visible tearing
            // or a pink/magenta screen (especially in the app switcher).
            if (RenderEncoder == null) { BeginEncoding(device); }
            GraphicsDebug.Spam($" ----- End FB pass {ToString()} ----");

            EndEncoding();
            RenderPassDescriptor?.Dispose();
            RenderPassDescriptor = null;
            _passCount = 0;
        }

        /// <summary>
        /// Updates the depth stencil state when a single render pass begins.
        /// </summary>
        public void SetDepthStencilState(IMTLDepthStencilState depthStencilState)
        {
            _depthStencilState = depthStencilState;
        }

        /// <summary>
        /// Sets up scissor rect when a single render pass begins.
        /// </summary>
        internal void SetScissorRect(Rectangle scissorRect)
        {
            GraphicsDebug.Spam($" ----- Setting scissor rect {scissorRect}");
            _scissorRectSet = true;
            MetalGraphicsHelpers.ToMTLRect(scissorRect, ref _scissorRect);
        }

        public void SetClearLoadAction(ref MTLClearColor color)
        {
            GraphicsDebug.Spam($" ----- Setting clear color {MetalGraphicsHelpers.ToStr(color)}");
            _clearColor = color;
            _clearTargetsOnBegin = true;
        }

        // Auto-generated `Equals` etc.
        protected bool Equals(MetalRenderPass other)
        {
            return Equals(_Id, other._Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MetalRenderPass)obj);
        }

        public override int GetHashCode()
        {
            return (_Id != null ? _Id.GetHashCode() : 0);
        }

        public void Dispose()
        {
            RenderPassDescriptor?.Dispose();
            RenderPassDescriptor = null;

            RenderEncoder?.Dispose();
            RenderEncoder = null;

            _isViewDataInit = false;
        }

        public override string ToString()
        {
            return $"RenderPass: [{(_isFramebuffer ? "fb" : "rt")} #{_Id}] color {ColorPixelFormat} / stencil {StencilPixelFormat} / depth {DepthPixelFormat}; clear? {_clearTargetsOnBegin} - {MetalGraphicsHelpers.ToStr(_clearColor)}";
        }
    }
}
