// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Runtime.InteropServices;
using GD = Microsoft.Xna.Framework.Graphics.GraphicsDebug;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Caches constant buffer allocations that can be applied to the <see cref="GraphicsDevice.CurrentRenderEncoder"/>.
    /// </summary>
    internal partial class ConstantBuffer
    {
        private readonly MetalBufferCache _bufferCache = new();

        private void PlatformInitialize()
        {
            PlatformClear();
        }

        private void PlatformClear()
        {
            _bufferCache.ResetHeap();
        }

        /// <summary>
        /// Applies the constant buffer at <see cref="index"/> to the vertex or fragment <see cref="Shader"/>.
        /// The index is extracted during the effect compilation stage
        /// <seealso href="https://github.com/search?q=repo%3AMonoGame%2FMonoGame+shaderprofile&type=code"/>.
        /// </summary>
        public void PlatformApply(GraphicsDevice device, Shader shader, int index)
        {
            if (_buffer == null || _buffer.Length == 0 || device.CurrentRenderEncoder == null) { return; }

            // It's more efficient to directly pass the data as part of the encoded buffer as opposed
            // to a separate buffer if it's small enough. (Order of magnitude faster in profiling this code).
            if (_buffer.Length >= MetalGraphicsHelpers.MAX_INLINE_BUFFER_LENGTH_BYTES)
            {
                var argBuffer = _bufferCache.CreateBuffer(device, _buffer, _buffer.Length, 0, true);
                if (shader.Stage == ShaderStage.Pixel)
                {
                    device.CurrentRenderEncoder.SetFragmentBuffer(argBuffer, 0, (nuint)index);
                }
                else { device.CurrentRenderEncoder.SetVertexBuffer(argBuffer, 0, (nuint)index); }
            }
            else
            {
                var alloc = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
                try
                {
                    if (shader.Stage == ShaderStage.Pixel)
                    {
                        device.CurrentRenderEncoder.SetFragmentBytes(alloc.AddrOfPinnedObject(),
                            (nuint)_buffer.Length, (nuint)index);
                    }
                    else
                    {
                        device.CurrentRenderEncoder.SetVertexBytes(alloc.AddrOfPinnedObject(),
                            (nuint)_buffer.Length, (nuint)index);
                    }
                }
                finally { alloc.Free(); }
            }
        }
    }
}
