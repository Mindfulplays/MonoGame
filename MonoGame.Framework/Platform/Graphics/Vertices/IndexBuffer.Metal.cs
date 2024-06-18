// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class IndexBuffer
    {
        private void PlatformConstruct(IndexElementSize indexElementSize, int indexCount)
        {
            throw new NotImplementedException("Use GraphicsDevice.Draw*Primitives instead.");
        }

        private void PlatformGraphicsDeviceResetting()
        {
            
        }

        private void PlatformGetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount) where T : struct
        {
        }

        private void GetBufferData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount) where T : struct
        {

        }

        private void PlatformSetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, SetDataOptions options)
            where T : struct
        {
        }

        private void PlatformSetDataBody<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, SetDataOptions options) where T : struct
        {

        }
    }
}
