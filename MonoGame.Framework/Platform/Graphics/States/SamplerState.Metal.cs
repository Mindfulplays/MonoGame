// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Metal;
using SpriteKit;
using System;
using System.Diagnostics;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class SamplerState
    {
        private IMTLSamplerState _samplerState;

        internal void Activate(GraphicsDevice device, Texture target)
        {
            if (!device.ValidateMetalState(true)) { return; }

            if (_samplerState == null)
            {
                MTLSamplerDescriptor samplerDescriptor = new()
                {
                    MinFilter = MetalGraphicsHelpers.ConvertMinSamplerFilter(Filter),
                    MagFilter = MetalGraphicsHelpers.ConvertMagSamplerFilter(Filter),
                    MipFilter = MetalGraphicsHelpers.ConvertMipSamplerFilter(Filter),
                    // s=u (x horizontal), t=v (y vertical), r=w (depth)
                    SAddressMode = MetalGraphicsHelpers.ConvertSamplerAddressMode(AddressU),
                    TAddressMode = MetalGraphicsHelpers.ConvertSamplerAddressMode(AddressV),
                    RAddressMode = MetalGraphicsHelpers.ConvertSamplerAddressMode(AddressW),
                    MaxAnisotropy = (uint)_maxAnisotropy,
                    CompareFunction = MetalGraphicsHelpers.ConvertCompareFunction(
                        FilterMode == TextureFilterMode.Default ? CompareFunction.Never : ComparisonFunction),
                    LodMaxClamp = MaxMipLevel > 0 ? (float)MaxMipLevel : 1000.0f // Matches OpenGL behavior.
                };

                // TODO: Border color is unsupported on low-end devices.
                // Border color currently available only on iOS 14+. Ignoring for now.
                // BorderColor = MetalGraphicsHelpers.ConvertBorderColor(_borderColor),
                // Also check MetalGraphicsHelpers.ConvertSamplerAddressMode too.

                _samplerState = device.MetalDevice.CreateSamplerState(samplerDescriptor);
            }

            if (_samplerState != null)
            {
                device.CurrentRenderEncoder.SetFragmentSamplerState(_samplerState, (UIntPtr)target.SamplerIndex);
            }
        }

        internal void Clear()
        {
            MetalGraphicsHelpers.CleanDispose(ref _samplerState);
        }
    }
}
