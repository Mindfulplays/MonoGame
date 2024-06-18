// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class RasterizerState
    {
        internal void PlatformApplyState(GraphicsDevice device, bool force = false)
        {
            if (device?.CurrentRenderEncoder is not { } e) { return; }

            e.SetCullMode(MetalGraphicsHelpers.ConvertCullMode(_cullMode));

            // Set clamp = 0 to disable clamping.
            e.SetDepthBias(_depthBias, _slopeScaleDepthBias, /* clamp */ 0.0f);
            e.SetTriangleFillMode(MetalGraphicsHelpers.ConvertFillMode(_fillMode));
        }
    }
}
