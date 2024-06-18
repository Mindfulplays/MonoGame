// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using GD = Microsoft.Xna.Framework.Graphics.GraphicsDebug;

namespace Microsoft.Xna.Framework.Graphics
{
    public sealed partial class TextureCollection
    {
        void PlatformInit()
        {
        }

        void PlatformClear()
        {
        }

        // Called from PlatformApplyState (sets samplers + textures)
        void PlatformSetTextures(GraphicsDevice device)
        {
            if (_dirty == 0) { return; }

            for (int i = 0; i < _textures.Length; i++)
            {
                var mask = 1 << i;
                if ((_dirty & mask) == 0) { continue; }

                var tex = _textures[i];
                if (tex != null)
                {
                    tex.Apply(device.CurrentRenderEncoder);
                }
            }
        }
    }
}
