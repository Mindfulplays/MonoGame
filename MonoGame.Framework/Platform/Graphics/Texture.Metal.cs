// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Metal;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class Texture
    {
        internal IMTLTexture _texture;

        private void PlatformGraphicsDeviceResetting()
        {
        }

        // TODO: Fill in correct sampler index.
        public int SamplerIndex { get; set; } = 0;

        public void Apply(IMTLRenderCommandEncoder renderEncoder)
        {
            if (_texture == null) { return; }

            renderEncoder.UseResource(_texture, MTLResourceUsage.Read);
            renderEncoder.SetFragmentTexture(_texture, (nuint)SamplerIndex);
        }
    }
}
