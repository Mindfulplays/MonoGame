// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Metal;
using System;
using GD = Microsoft.Xna.Framework.Graphics.GraphicsDebug;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// See <see cref="https://developer.apple.com/documentation/metal/render_passes/customizing_render_pass_setup#3723797"/> for details on render target setup.
    /// The main difference is the <see cref="IMTLTexture.Usage"/> being set to <see cref="MTLTextureUsage.RenderTarget"/>.
    /// </summary>
    /// 
    public partial class RenderTarget2D
    {
        private MetalRenderPass _renderPass;

        private void PlatformConstruct(GraphicsDevice graphicsDevice, int width, int height, bool mipMap,
            DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage, bool shared)
        {
            try
            {
                // TODO: Fill in additional parameters.
                _texture = GraphicsDevice.MetalDevice.CreateTexture(new MTLTextureDescriptor()
                {
                    PixelFormat = SurfaceFormatToMetal_(Format),
                    Width = (UIntPtr)width,
                    Height = (UIntPtr)height,
                    // SampleCount is left unfilled as this is a render target.
                    Usage = MTLTextureUsage.ShaderRead | MTLTextureUsage.RenderTarget,
                    TextureType = MTLTextureType.k2D,
                    CpuCacheMode = MTLCpuCacheMode.DefaultCache,
                    StorageMode = MTLStorageMode.Private
                });
                var renderPassDescriptor = MTLRenderPassDescriptor.CreateRenderPassDescriptor();
                var renderPassTexture = renderPassDescriptor.ColorAttachments[0];
                renderPassTexture.Texture = _texture;
                renderPassTexture.LoadAction = RenderTargetUsage == RenderTargetUsage.DiscardContents
                    ? MTLLoadAction.Clear
                    : MTLLoadAction.Load;
                // Important to set the store action to `Store` so that the contents are retained: Otherwise,
                // they are wiped out even if `DiscardContents` is requested.
                renderPassTexture.StoreAction = MTLStoreAction.Store;
                renderPassTexture.ClearColor = new MTLClearColor(0, 0, 0, 0);
                _renderPass = new(renderPassDescriptor);
                GD.Spam($"Created render target {width}x{height} {Format} {_texture.PixelFormat} {RenderTargetUsage}");
            }
            catch (Exception e)
            {
                GD.C($"Unable to create render target {e}");
            }
        }

        private void PlatformGraphicsDeviceResetting()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                MetalGraphicsHelpers.CleanDispose(ref _renderPass);
            }

            base.Dispose(disposing);
        }

        public MetalRenderPass GetMetalRenderPass()
        {
            return _renderPass;
        }

        public override string ToString()
        {
            return $" (RT {width}x{height} {Format} {_texture?.PixelFormat} {RenderTargetUsage} / {_renderPass?.ToString()})";
        }
    }
}
