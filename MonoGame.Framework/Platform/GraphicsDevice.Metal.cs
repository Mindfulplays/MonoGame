// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.InteropServices;
using CoreGraphics;
using Metal;
using MetalKit;
using MonoGame.Framework.Utilities;
using UIKit;
using GD = Microsoft.Xna.Framework.Graphics.GraphicsDebug;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Implements MonoGame GraphicsDevice for Apple's Metal-based hardware.
    /// 
    /// Manages overall <see cref="https://developer.apple.com/documentation/metal/mtldevice"/>
    /// and <see cref="https://developer.apple.com/documentation/metal/mtlcommandqueue"/>.
    /// Drives the per-frame (single) render pass using
    /// <see cref="https://developer.apple.com/documentation/metal/mtlcommandbuffer"/>
    /// and <see cref="https://developer.apple.com/documentation/metal/mtlrendercommandencoder"/>.
    ///
    /// The overall flow is something like this:
    /// PlatformInitialize (once-per MTKView): Initialize <code>MTLDevice</code> and its
    /// <code>CommandQueue</code>.
    ///
    /// Each render pass is enqueued automatically from the <code>MTKView</code> via <code>SetNeedsDisplay</code>:
    /// PreDraw: Setup <code>RenderEncoder</code> from a <code>CommandBuffer</code> obtained
    /// from the <code>CommandQueue</code>.
    /// Draw: From <see cref="GamePlatform"></see>'s <code>Draw</code> which will call the <see cref="Game.Draw"/>
    /// allowing the user's implementation to batch rendering commands.
    /// 
    /// Metal allows rendering commands being queued from any thread - although MonoGame is decisively single-threaded.
    /// However, some specific benefits of thread-safe behavior can be reaped (such as Texture.SetData not relying
    /// upon nor blocking onthe UI thread ).
    /// 
    /// TODO: After adding macOS support, add supported Apple platforms (and
    /// perhaps include visionOS, watchOS, tvOS too).
    /// </summary>
    public partial class GraphicsDevice : IGraphicsMetalDeviceDelegate
    {
        private GraphicsDeviceError _deviceError = GraphicsDeviceError.None;
        public IMTLDevice _device;
        private MTKTextureLoader _textureLoader;

        // Create a lookup key once and reuse it for all other frames.
        private readonly MetalPipelineStateKey _pipelineLookupKey = new();
        private readonly MetalBufferCache _vertexBufferCache = new();
        private readonly MetalBufferCache _indexBufferCache = new();
        private readonly MetalPipelineStateCache _stateCache = new();
        private bool _init;
        private bool _clearColorDirty;

        internal MTKTextureLoader MetalTextureLoader => _textureLoader;
        internal IMTLDevice MetalDevice => _device;

        private IMTLCommandQueue _commandQueue;
        private IMTLCommandBuffer _commandBuffer;
        internal IMTLCommandBuffer CommandBuffer => _commandBuffer;

        private MetalRenderPass _framebufferRenderPass;
        private MetalRenderPass _renderTargetRenderPass;

        private MTLClearColor _clearColor;
        private int _frameCount;
        internal int CurrentFrame => _frameCount;
        internal MTLViewport MetalViewport { get; private set; }

        private string deviceInfo_ = "";

        void PlatformSetup()
        {
            MaxTextureSlots = 16;
            MaxVertexTextureSlots = 16;
        }

        void PlatformInitialize()
        {
            GD.C("Initialze Platform");
            _stateCache.Clear();
            Viewport = new Viewport(0, 0, PresentationParameters.BackBufferWidth,
                PresentationParameters.BackBufferHeight);

            _pixelShader = null;
            _vertexShader = null;
            try
            {
                this.PlatformApplyBlend(true);
                this.DepthStencilState.PlatformApplyState(this, true);
                this.RasterizerState.PlatformApplyState(this, true);
            }
            catch (Exception e)
            {
                GD.C($"Exception in PlatformInitialize {e}");
                _deviceError = GraphicsDeviceError.InitializationError;
            }
        }

        public void InitializeMetal(IMTLDevice device, MTKView view)
        {
            GD.C("Initialze Metal");
            if (!_init)
            {
                _device = device;
                _textureLoader ??= new(_device);
                deviceInfo_ = "";
                deviceInfo_ += $"Setup Metal device:  {device.Name}\n";
                if (UIDevice.CurrentDevice.CheckSystemVersion(16, 0))
                {
                    if (device.SupportsFamily(MTLGpuFamily.Apple7)) { deviceInfo_ += " -- Apple7"; }
                    if (device.SupportsFamily(MTLGpuFamily.Apple6)) { deviceInfo_ += " -- Apple6"; }
                    if (device.SupportsFamily(MTLGpuFamily.Apple5)) { deviceInfo_ += " -- Apple5"; }
                    if (device.SupportsFamily(MTLGpuFamily.Apple4)) { deviceInfo_ += " -- Apple4"; }
                    if (device.SupportsFamily(MTLGpuFamily.Apple3)) { deviceInfo_ += " -- Apple3"; }
                    if (device.SupportsFamily(MTLGpuFamily.Apple2)) { deviceInfo_ += " -- Apple2"; }
                    if (device.SupportsFamily(MTLGpuFamily.Apple1)) { deviceInfo_ += " -- Apple1"; }
                    deviceInfo_ += $" -- regid {device.RegistryId} / current alloc: {device.CurrentAllocatedSize} / unified memory? {device.HasUnifiedMemory} / rw-texture: {device.ReadWriteTextureSupport}\n";
                    deviceInfo_ += $" -- max buffer: {device.MaxBufferLength} / max thread group {device.MaxThreadgroupMemoryLength}\n";
                    deviceInfo_ += $"  -- View: Sample count {view.SampleCount} / format {view.ColorPixelFormat}\n";
                }
                GD.I(deviceInfo_);
                GD.DeviceInfo = deviceInfo_;

                _commandQueue ??= _device.CreateCommandQueue();
                _framebufferRenderPass = new(view);
                _vertexBufferCache.ResetHeap();
                _indexBufferCache.ResetHeap();
                _clearColorDirty = true;
                _init = true;
            }
        }

        /// <summary>
        /// Fetches the current render pass which is
        /// (a) first, an ongoing render target's render pass, or else
        /// (b) the default framebuffer render pass.
        /// </summary>
        internal MetalRenderPass CurrentRenderPass => _renderTargetRenderPass ?? _framebufferRenderPass;

        /// <summary>
        /// Fetches the current render pass' render encoder (either the render target's, if not,
        /// the framebuffer's).
        /// </summary>
        internal IMTLRenderCommandEncoder CurrentRenderEncoder
        {
            get
            {
                var ret = CurrentRenderPass?.RenderEncoder;
                return ret;
            }
        }

        /// <summary>
        /// Checks for global errors and returns true if there are no errors.
        /// If optional <param name="checkState" /> is <code>true</code> then
        /// render states such as render encoder etc are also checked and if
        /// everything is setup correctly, returns true.
        /// </summary>
        internal bool ValidateMetalState(bool checkState)
        {
            if (!_init || MetalDevice == null) { return false; }

            if (_deviceError != GraphicsDeviceError.None)
            {
                GD.Spam($"--Invalid metal state: Device error {_deviceError}");
                return false;
            }

            if (checkState)
            {
                if (CurrentRenderPass == null)
                {
                    GD.Spam($"--Invalid metal state: No render pass available.");
                    return false;
                }
            }

            return true;
        }

        void PlatformApplyBlend(bool force = false)
        {
            if (!ValidateMetalState(true)) { return; }
        }

        void PlatformBeginApplyState()
        {
            // Nothing to do here.
        }

        /// <summary>
        /// Sets up the pipeline states and the render pass to start drawing triangles
        /// for a specific combination of vertex/fragment shaders, blend states and
        /// render pass.
        /// </summary>
        private void _SetupMetalDrawing(VertexDeclaration vertexDeclaration)
        {
            if (!ValidateMetalState( /* checkState */ false)) { return; }

            // At the beginning of a single frame (i.e. a render pass), we must already have setup
            // a command buffer + render encoder.
            if (_commandBuffer == null || CurrentRenderPass == null)
            {
                GD.E(
                    $"Unable to create command buffer {_commandBuffer != null}.");
                _deviceError = GraphicsDeviceError.CommandBufferCreateError;
                return;
            }

            var state = _stateCache.ObtainPipelineState(
                _pipelineLookupKey.Update(VertexShader, PixelShader, BlendState, CurrentRenderPass));
            if (state.RequiresUpdate())
            {
                state.PipelineDescriptor = new()
                {
                    SampleCount = CurrentRenderPass.SampleCount,
                    DepthAttachmentPixelFormat = CurrentRenderPass.DepthPixelFormat,
                    StencilAttachmentPixelFormat = CurrentRenderPass.StencilPixelFormat
                };
                GD.Spam($" -- Creating new pipeline state for {CurrentRenderPass}");
                vertexDeclaration.Apply(VertexShader, state.PipelineDescriptor);

                var colorRT = state.PipelineDescriptor.ColorAttachments[0];
                colorRT.PixelFormat = CurrentRenderPass.ColorPixelFormat;
                BlendState.PlatformApplyState(colorRT);
                state.PipelineDescriptor.VertexFunction = VertexShader.Program;
                state.PipelineDescriptor.FragmentFunction = PixelShader.Program;
                state.PipelineState = _device.CreateRenderPipelineState(state.PipelineDescriptor, out var error);
                if (state.PipelineState == null)
                {
                    _deviceError = GraphicsDeviceError.RenderPipelineStateError;
                    GD.E($"-- error notes: {state.PipelineDescriptor.VertexDescriptor}");
                    VertexShader.PrintInfo();
                    GD.E($"Unable to create render pipeline state {error}.");
                    return;
                }
            }

            // Obtain render encoder from the current render pass (if successful, otherwise, we skip this frame).
            _commandBuffer.PushDebugGroup("ApplyState");

            // Begin encoding if it hasn't commenced already yet.
            CurrentRenderPass.BeginEncoding(this);
            CurrentRenderEncoder.SetRenderPipelineState(state.PipelineState);
        }

        private void PlatformApplyState(bool applyShaders)
        {
            try
            {
                if (!ValidateMetalState(true)) { return; }

                if (!applyShaders) { return; }

                Textures.SetTextures(this);
                SamplerStates.PlatformSetSamplers(this);

                _pixelConstantBuffers.SetConstantBuffers(this, PixelShader);
                _vertexConstantBuffers.SetConstantBuffers(this, VertexShader);
            }
            catch (Exception e)
            {
                GD.C($"Exception PlatformApplyState {e}");
                _deviceError = GraphicsDeviceError.RenderPipelineStateError;
            }
        }

        private void _PostDrawPrimitives(VertexDeclaration vertexDeclaration)
        {
            _vertexBuffersDirty = false;
            _vertexShaderDirty = false;
            _pixelShaderDirty = false;
            _commandBuffer.PopDebugGroup();
        }

        private void PlatformDrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData,
            int vertexOffset, int numVertices, short[] indexData, int indexOffset, int primitiveCount,
            VertexDeclaration vertexDeclaration)
            where T : struct
        {
            if (!ValidateMetalState(false)) { return; }

            _SetupMetalDrawing(vertexDeclaration);
            if (!ValidateMetalState(false)) { return; }

            ApplyState(true);
            if (!ValidateMetalState(false)) { return; }

            var vertexBufferLength = numVertices * ReflectionHelpers.SizeOf<T>.Get();
            if (vertexBufferLength >= MetalGraphicsHelpers.MAX_INLINE_BUFFER_LENGTH_BYTES)
            {
                GD.Spam($" >>> (New/cached large buffer) Drawing {numVertices} vertices / {indexData.Length} indices ({vertexBufferLength} bytes).");
                var vertexBuffer = _vertexBufferCache.CreateBuffer(this, vertexData, numVertices,
                    vertexOffset, /* allowReuse*/ false);
                CurrentRenderEncoder.SetVertexBuffer(vertexBuffer, (nuint)vertexOffset, 1);
            }
            else
            {
                GD.Spam($" >>> (Small buffer) Drawing {numVertices} vertices / {indexData.Length} indices (of which only {vertexBufferLength} bytes copied over).");
                // Very efficient to directly pass the data as part of the encoded buffer as opposed
                // to a separate buffer if it's small enough. (Order of magnitude faster in profiling this code).
                var alloc = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
                try
                {
                    CurrentRenderEncoder.SetVertexBytes(
                        (IntPtr)(alloc.AddrOfPinnedObject().ToInt64() +
                                 (vertexOffset * ReflectionHelpers.SizeOf<T>.Get())), (nuint)vertexBufferLength, 1);
                }
                finally { alloc.Free(); }
            }

            var indexCount = GetElementCountArray(primitiveType, primitiveCount);
            var indexBuffer = _indexBufferCache.CreateBuffer(this, indexData, indexCount,
                indexOffset, /* allowReuse */ indexCount < 16);
            CurrentRenderEncoder.DrawIndexedPrimitives(MetalGraphicsHelpers.ConvertPrimitiveType(primitiveType),
                (UIntPtr)indexCount,
                MTLIndexType.UInt16, // short
                indexBuffer, (nuint)indexOffset);

            if (!ValidateMetalState(false)) { return; }

            _PostDrawPrimitives(vertexDeclaration);
        }

        private void PlatformClear(ClearOptions options, Vector4 color, float depth, int layer)
        {
            try
            {
                MTLClearColor prevClearColor = _clearColor;
                MetalGraphicsHelpers.ToMTLClearColor(color, ref _clearColor);
                if (!_clearColorDirty)
                {
                    // ReSharper disable CompareOfFloatsByEqualityOperator - Intentional.
                    if (_clearColor.Red != prevClearColor.Red ||
                        _clearColor.Green != prevClearColor.Green ||
                        _clearColor.Blue != prevClearColor.Blue ||
                        _clearColor.Alpha != prevClearColor.Alpha) { _clearColorDirty = true; }
                }

                if (!ValidateMetalState(false)) { return; }

                if ((options & ClearOptions.Target) != 0)
                {
                    if (CurrentRenderPass != null)
                    {
                        // This silliness is required because we have to clear the screen the first time the render pass is set.
                        // Otherwise, it's too late, so we have to end the current encoding, clear, begin another one.
                        CurrentRenderPass.EndEncoding();
                        CurrentRenderPass.SetClearLoadAction(ref _clearColor);
                        CurrentRenderPass.BeginEncoding(this);
                        GD.Spam($"--Request clearing on {CurrentRenderPass}");
                    }
                    else
                    {
                        GD.Spam(" - No render pass available - not clearing.");
                    }

                    ApplyState(false);
                    if (!ValidateMetalState(false)) { return; }
                }
            }
            catch (Exception e)
            {
                GD.E($"Error clearing: \n{e}");
                _deviceError = GraphicsDeviceError.RenderPipelineStateError;
            }
        }

        private void PlatformDispose()
        {
            _frameCount = 0;
            _device = null; // Let the device dispose of itself via the View.

            _stateCache.Clear();
            _vertexBufferCache.ResetHeap();
            _indexBufferCache.ResetHeap();

            MetalGraphicsHelpers.CleanDispose(ref _framebufferRenderPass);
            MetalGraphicsHelpers.CleanDispose(ref _renderTargetRenderPass);

            MetalGraphicsHelpers.CleanDispose(ref _commandQueue);
        }

        private void PlatformPresent()
        {
        }

        private void OnPresentationChanged()
        {
        }

        private void PlatformSetViewport(ref Viewport viewport)
        {
            if (!ValidateMetalState(true) || CurrentRenderEncoder == null) { return; }

            if (CurrentRenderPass.IsFramebufferPass)
            {
                GD.Spam($"--> (FB) Set Viewport {viewport}");
                MetalViewport = new(viewport.X, PresentationParameters.BackBufferHeight - viewport.Y - viewport.Height,
                    viewport.Width, viewport.Height,
                    viewport.MinDepth, viewport.MaxDepth);
                CurrentRenderEncoder.SetViewport(MetalViewport);
            }
            else
            {
                GD.Spam($"--> (RT) Set Viewport {viewport}");
                MetalViewport = new(viewport.X, viewport.Y, viewport.Width, viewport.Height,
                    viewport.MinDepth, viewport.MaxDepth);
                CurrentRenderEncoder.SetViewport(MetalViewport);
            }
        }

        private void PlatformResolveRenderTargets()
        {
        }

        private void PlatformApplyDefaultRenderTarget()
        {
            MetalApplyRenderTarget_(null);
        }

        private void MetalApplyRenderTarget_(IRenderTarget rtNext)
        {
            if (_commandBuffer == null)
            {
                GD.Spam(" - No command buffer available, unable to set render target.");
                return;
            }

            SwitchRenderPass_(rtNext?.GetMetalRenderPass());
        }

        /// <summary>
        /// When switching render targets, ensure we cleanly flush the current render
        /// target's render encoder.
        /// </summary>
        private void SwitchRenderPass_(MetalRenderPass renderPassNext)
        {
            if (!ValidateMetalState(true))
            {
                GD.Spam($"Unable to switch render pass {CurrentRenderPass?.ToString()} -> {renderPassNext?.ToString()}");
                return;
            }

            // No need to switch if we are using the same render pass.
            if (renderPassNext?.Equals(CurrentRenderPass) == true)
            {
                GD.Spam($"--Same render target {CurrentRenderPass?.ToString()}.");
                return;
            }

            // End encoding on the current pass....
            CurrentRenderPass?.EndEncoding();
            GD.Spam($" -- Switching render pass {CurrentRenderPass?.ToString()} -> {renderPassNext?.ToString()}");

            _renderTargetRenderPass = renderPassNext;

            // The following uses the new render target's render pass.
            if (CurrentRenderPass != null) { CurrentRenderPass.BeginEncoding(this); }
            else
            {
                // This should never hit: We either have the default framebuffer render pass or
                // a render target render pass but never neither.
                throw new Exception("Invalid render state - no render pass available.");
            }
        }

        private IRenderTarget PlatformApplyRenderTargets()
        {
            IRenderTarget target = null;
            try
            {
                for (int i = 0; i < _currentRenderTargetCount; ++i)
                {
                    if (_currentRenderTargetBindings[i].RenderTarget is IRenderTarget renderTarget)
                    {
                        GD.Spam($"Setting rendertarget -> {renderTarget}");
                        MetalApplyRenderTarget_(renderTarget);
                        target = renderTarget;
                        break;
                    }
                }

#if DEBUG                
                if (target == null)
                {
                    GD.Spam($" -- No render target for {CurrentRenderPass}");
                }
#endif                
            }
            catch (Exception e)
            {
                GD.C($"Render target Error: {e}");
                _deviceError = GraphicsDeviceError.RenderPipelineStateError;
            }

            return target;
        }

        private void PlatformDrawIndexedPrimitives(PrimitiveType primitiveType, int baseVertex, int startIndex,
            int primitiveCount)
        {
            GD.Spam($"--> UNUSED DrawIndexedPrimitives");
        }

        private void PlatformDrawPrimitives(PrimitiveType primitiveType, int vertexStart, int vertexCount)
        {
            GD.Spam($"--> UNUSED DrawPrimitives");
        }

        private void PlatformDrawUserPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset,
            VertexDeclaration vertexDeclaration, int vertexCount) where T : struct
        {
            GD.Spam($"--> UNUSED DrawUserPrimitives");
        }

        private void PlatformDrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset,
            int numVertices, int[] indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration)
            where T : struct
        {
            // Fill this in first (A)
            GD.Spam($"--> UNUSED DrawUserIndexPrimitives");
            
        }

        private void PlatformDrawInstancedPrimitives(PrimitiveType primitiveType, int baseVertex, int startIndex,
            int primitiveCount, int baseInstance, int instanceCount)
        {
            GD.Spam($"--> UNUSED DrawInstancedPrimitives");
        }

        private void PlatformGetBackBufferData<T>(Rectangle? rect, T[] data, int startIndex, int count) where T : struct
        {
        }

        private static Rectangle PlatformGetTitleSafeArea(int x, int y, int width, int height)
        {
            return new Rectangle(x, y, width, height);
        }

        public void DrawableSizeWillChange(MTKView view, CGSize size)
        {
        }

        private string _frameTimeTracker = "";

        /// <summary>
        /// Invoked once per-frame before any rendering begins. If there is a recoverable error, then
        /// try recovering otherwise, bail out.
        /// </summary>
        public void PreDrawFrame(MTKView view)
        {
            _frameTimeTracker = GD.StartTimer("DrawFrame");
            var preDrawFrametimer = GD.StartTimer("PreDraw");
            ++_frameCount;

            if (_deviceError is GraphicsDeviceError.NoRenderEncoderAvailable
                or GraphicsDeviceError.CommandBufferCreateError)
            {
                // Clear recoverable errors but ensure pipeline state cache is clean too.
                GD.C($"---- ERROR Recovering for frame {_frameCount}. -----");
                _stateCache.Clear();
                _deviceError = GraphicsDeviceError.None;
            }

            _commandBuffer = _commandQueue.CommandBuffer();
            if (_commandBuffer == null)
            {
                _deviceError = GraphicsDeviceError.CommandBufferCreateError;
                return;
            }

            _commandBuffer.Label = $"Pass# {_frameCount}";
            if (_clearColorDirty)
            {
                view.ClearColor = _clearColor;
                _clearColorDirty = false;
            }

            _framebufferRenderPass.BeginFramebufferPass(view);

            _framebufferRenderPass.SetClearLoadAction(ref _clearColor);

            _scissorRectangleDirty = false;

            _framebufferRenderPass.SetScissorRect(ScissorRectangle);
            GD.StopTimer(preDrawFrametimer);
        }

        public void PostDrawFrame(MTKView view)
        {
            if (!ValidateMetalState(false)) { return; }

            if (_commandBuffer != null && _framebufferRenderPass != null)
            {
                var postDrawTimer = GD.StartTimer("PostDraw");
                _framebufferRenderPass.EndFramebufferPass(this);

                var drawable = view.CurrentDrawable;
                if (drawable == null)
                {
                    GD.Spam(" - No drawable available to present.");
                    return;
                }

                _commandBuffer.PresentDrawable(drawable);

                // Finalize rendering here & push the command buffer to the GPU

                _commandBuffer.Commit();
                view.SetNeedsDisplay();
                MetalGraphicsHelpers.CleanDispose(ref _commandBuffer);
                GD.StopTimer(postDrawTimer);
            }
            else
            {
                GD.Spam($" - No command buffer {_commandBuffer == null} or no framebuffer {_framebufferRenderPass == null}.");
            }
            GD.StopTimer(_frameTimeTracker);
        }
    }
}
