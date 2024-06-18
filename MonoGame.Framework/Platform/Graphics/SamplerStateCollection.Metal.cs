// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using Metal;
using GD = Microsoft.Xna.Framework.Graphics.GraphicsDebug;

namespace Microsoft.Xna.Framework.Graphics
{
    public sealed partial class SamplerStateCollection
    {
        private void PlatformSetSamplerState(int index)
        {
        }

        private void PlatformClear()
        {
            if (_actualSamplers?.Length > 0)
            {
                for (int i = 0; i < _actualSamplers.Length; i++)
                {
                    var sampler = _actualSamplers[i];
                    sampler.Clear();
                }
            }
        }

        private void PlatformDirty()
        {
            PlatformClear();
        }

        // Called from PlatformApplyState (sets samplers after textures are set).
        internal void PlatformSetSamplers(GraphicsDevice device)
        {
            for (int i = 0; i < _actualSamplers.Length; i++)
            {
                var sampler = _actualSamplers[i];
                var texture = device.Textures[i];
                if (sampler != null && texture != null) { sampler.Activate(device, texture); }
            }
        }
    }
}
