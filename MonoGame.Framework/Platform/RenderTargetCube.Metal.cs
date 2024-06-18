// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Metal;
using System;

namespace Microsoft.Xna.Framework.Graphics
{
    // TODO: Fill this.
    public partial class RenderTargetCube
    {
        private void PlatformConstruct(GraphicsDevice graphicsDevice1, int width, int height, bool mipMap,
            DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
        {
        }

        public MetalRenderPass GetMetalRenderPass()
        {
            throw new NotImplementedException();
        }
    }
}
