// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class VertexBuffer
    {
        private void PlatformConstruct()
        {
            throw new NotImplementedException("Use GraphicsDevice.Draw*Primitives instead.");
        }

        private void PlatformGraphicsDeviceResetting()
        {
        }

        private void PlatformGetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride)
            where T : struct
        {
        }

        private void PlatformSetData<T>(
            int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride, SetDataOptions options,
            int bufferSize, int elementSizeInBytes)
            where T : struct
        {
        }

        private void PlatformSetDataBody<T>(
            int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride, SetDataOptions options,
            int bufferSize, int elementSizeInBytes)
            where T : struct
        {
        }
    }
}
