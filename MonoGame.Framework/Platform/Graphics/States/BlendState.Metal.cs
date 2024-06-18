// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using Metal;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class BlendState
    {
        /// <summary>
        /// Converts MonoGame internal Blend functions/factors to Metal ones.
        /// </summary>
        internal void PlatformApplyState(MTLRenderPipelineColorAttachmentDescriptor colorDesc)
        {
            colorDesc.BlendingEnabled = true;

            colorDesc.AlphaBlendOperation = MetalGraphicsHelpers.ToMTLBlendOperation(AlphaBlendFunction);
            colorDesc.RgbBlendOperation = MetalGraphicsHelpers.ToMTLBlendOperation(ColorBlendFunction);

            colorDesc.DestinationAlphaBlendFactor = MetalGraphicsHelpers.ToMTLBlendFactor(AlphaDestinationBlend);
            colorDesc.SourceAlphaBlendFactor = MetalGraphicsHelpers.ToMTLBlendFactor(AlphaSourceBlend);

            colorDesc.DestinationRgbBlendFactor = MetalGraphicsHelpers.ToMTLBlendFactor(ColorDestinationBlend);
            colorDesc.SourceRgbBlendFactor = MetalGraphicsHelpers.ToMTLBlendFactor(ColorSourceBlend);
            colorDesc.WriteMask = MetalGraphicsHelpers.ToMTLWriteMask(ColorWriteChannels);
        }
    }
}
