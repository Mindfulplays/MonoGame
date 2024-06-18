// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Extends the RenderTarget interface to add Metal-specific objects to be used by the
    /// Metal <see cref="GraphicsDevice"/>.
    /// </summary>
    internal partial interface IRenderTarget
    {
        /// <summary>
        /// Obtains a holder for <see cref="https://developer.apple.com/documentation/metal/render_passes/customizing_render_pass_setup#3723797">Render Pass Descriptor</see>
        /// used during the render pass: This allows rendering onto a specific <see cref="MTLRenderPassColorAttachmentDescriptor">color attachment</see>.
        /// Also allows storing the render encoder too.
        /// </summary>
        MetalRenderPass GetMetalRenderPass();
    }
}
