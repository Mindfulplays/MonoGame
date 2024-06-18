// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Metal;
using GD = Microsoft.Xna.Framework.Graphics.GraphicsDebug;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Helps convert/apply depth stencil state to the render command encoder.
    /// <seealso href="https://developer.apple.com/documentation/metal/mtlstencildescriptor"/>
    /// </summary>
    public partial class DepthStencilState
    {
        private IMTLDepthStencilState _depthStencilState;

        internal void PlatformApplyState(GraphicsDevice device, bool force = false)
        {
            if (!device.ValidateMetalState(true)) { return; }

            if (_depthStencilState == null || force)
            {
                // TODO: This is not tested and may need correct MTLStoreAction for the attachments.
                MetalGraphicsHelpers.CleanDispose(ref _depthStencilState);
                var sd = new MTLDepthStencilDescriptor();
                sd.DepthCompareFunction =
                    MetalGraphicsHelpers.ConvertCompareFunction(_depthBufferFunction);

                sd.DepthWriteEnabled = _depthBufferWriteEnable;

                if (_stencilEnable)
                {
                    // MonoGame's internal stencil == 'front face' Metal stencil.
                    sd.FrontFaceStencil = new();
                    var front = sd.FrontFaceStencil;
                    front.DepthFailureOperation =
                        MetalGraphicsHelpers.ConvertDepthStencilOperation(_stencilDepthBufferFail);
                    front.StencilFailureOperation = MetalGraphicsHelpers.ConvertDepthStencilOperation(_stencilFail);
                    front.DepthStencilPassOperation = MetalGraphicsHelpers.ConvertDepthStencilOperation(_stencilPass);
                    front.StencilCompareFunction =
                        MetalGraphicsHelpers.ConvertCompareFunction(_stencilFunction);
                    front.ReadMask = (uint)_stencilMask;
                    front.WriteMask = (uint)_stencilWriteMask;

                    // MonoGame's internal counter clockwise stencil == 'back face' Metal stencil.
                    if (_twoSidedStencilMode)
                    {
                        sd.BackFaceStencil = new();
                        var back = sd.BackFaceStencil;
                        back.DepthFailureOperation =
                            MetalGraphicsHelpers.ConvertDepthStencilOperation(_counterClockwiseStencilDepthBufferFail);
                        back.StencilFailureOperation =
                            MetalGraphicsHelpers.ConvertDepthStencilOperation(_counterClockwiseStencilFail);
                        back.DepthStencilPassOperation =
                            MetalGraphicsHelpers.ConvertDepthStencilOperation(_counterClockwiseStencilPass);
                        back.StencilCompareFunction =
                            MetalGraphicsHelpers.ConvertCompareFunction(_counterClockwiseStencilFunction);
                        back.ReadMask = (uint)_stencilMask;
                        back.WriteMask = (uint)_stencilWriteMask;
                    }
                }

                _depthStencilState = device.MetalDevice.CreateDepthStencilState(sd);
            }

            if (device.CurrentRenderPass != null && _depthStencilState != null)
            {
                device.CurrentRenderPass.SetDepthStencilState(_depthStencilState);
            }
        }
    }
}
