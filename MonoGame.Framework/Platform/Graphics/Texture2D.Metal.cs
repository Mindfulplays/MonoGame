// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;
using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using Metal;
using MetalKit;
using MonoGame.Framework.Utilities;
using UIKit;
using GD = Microsoft.Xna.Framework.Graphics.GraphicsDebug;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Wraps a <see cref="IMTLTexture"/> with the correct Metal surface format.
    /// </summary>
    public partial class Texture2D : Texture
    {
        internal void PlatformConstruct(int width, int height, bool mipmap, SurfaceFormat format, SurfaceType type,
            bool shared)
        {
            // TODO: Verify all parameters.
            _texture = GraphicsDevice.MetalDevice.CreateTexture(new MTLTextureDescriptor()
            {
                PixelFormat = SurfaceFormatToMetal_(Format),
                Width = (UIntPtr)width,
                Height = (UIntPtr)height,
                Usage = MTLTextureUsage.ShaderRead
            });
        }

        private void MetalSetData<T>(T[] data, int startIndex, int count) where T : struct
        {
            if (_texture == null) { return; }

            // TODO: Verify all parameters.
            var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var startBytes = startIndex * ReflectionHelpers.SizeOf<T>.Get();
                var Bpp = Format.GetSize();

                var dataPtr = new IntPtr(dataHandle.AddrOfPinnedObject().ToInt64() + startBytes);
                _texture.ReplaceRegion(
                    new MTLRegion(new MTLOrigin(0, 0, 0), new MTLSize((IntPtr)width, (IntPtr)height, (IntPtr)1)),
                    0, dataPtr, (UIntPtr)(width * Bpp));
            }
            finally
            {
                // This is important as the GCHandle struct is not on the heap and requires manual
                // freeing of the pinned pointer (otherwise, this memory will be leaked permamently).
                MetalGraphicsHelpers.CleanDispose(ref dataHandle);
            }
        }

        ///<summary>
        /// Cross-referencing Apple documentation
        /// <see cref="https://developer.apple.com/documentation/metal/textures/understanding_color-renderable_pixel_format_sizes"/> with <see cref="SurfaceFormat"/> enum values.
        ///
        /// TODO: Move this to MetalGraphicsHelpers.
        /// TODO: Some of the values may need verification (see below).
        ///</summary>
        internal static MTLPixelFormat SurfaceFormatToMetal_(SurfaceFormat format)
        {
            switch (format)
            {
                case SurfaceFormat.Color: return MTLPixelFormat.RGBA8Unorm;
                case SurfaceFormat.Bgr565: return MTLPixelFormat.B5G6R5Unorm;
                case SurfaceFormat.Bgra5551: return MTLPixelFormat.BGR5A1Unorm;
                case SurfaceFormat.Bgra4444: return MTLPixelFormat.RGBA8Unorm;
                case SurfaceFormat.Dxt1: return MTLPixelFormat.BC1RGBA;
                case SurfaceFormat.Dxt3: return MTLPixelFormat.BC3RGBA;
                case SurfaceFormat.Dxt5: return MTLPixelFormat.BC5_RGUnorm; // TODO: Unsupported / Verify
                case SurfaceFormat.NormalizedByte2: return MTLPixelFormat.RG8Unorm;
                case SurfaceFormat.NormalizedByte4: return MTLPixelFormat.RGBA8Unorm;
                case SurfaceFormat.Rgba1010102: return MTLPixelFormat.RGB10A2Unorm;
                case SurfaceFormat.Rg32: return MTLPixelFormat.RG32Uint;
                case SurfaceFormat.Rgba64: return MTLPixelFormat.RGBA16Unorm;
                case SurfaceFormat.Alpha8: return MTLPixelFormat.A8Unorm;
                case SurfaceFormat.Single: return MTLPixelFormat.R32Float;
                case SurfaceFormat.Vector2: return MTLPixelFormat.RG32Float;
                case SurfaceFormat.Vector4: return MTLPixelFormat.RGBA32Float;
                case SurfaceFormat.HalfSingle: return MTLPixelFormat.R16Float;
                case SurfaceFormat.HalfVector2: return MTLPixelFormat.RG16Float;
                case SurfaceFormat.HalfVector4: return MTLPixelFormat.RGBA16Float;
                // From GraphicsExtensions
                // HdrBlendable implemented as HalfVector4 (see http://blogs.msdn.com/b/shawnhar/archive/2010/07/09/surfaceformat-hdrblendable.aspx)
                case SurfaceFormat.HdrBlendable: return MTLPixelFormat.RGBA16Float;
                case SurfaceFormat.Bgr32: return MTLPixelFormat.BGRA8Unorm;
                case SurfaceFormat.Bgra32: return MTLPixelFormat.BGRA8Unorm;
                case SurfaceFormat.ColorSRgb: return MTLPixelFormat.RGBA8Unorm_sRGB;
                case SurfaceFormat.Bgr32SRgb: return MTLPixelFormat.BGRA8Unorm_sRGB;
                case SurfaceFormat.Bgra32SRgb: return MTLPixelFormat.BGRA8Unorm_sRGB;
                case SurfaceFormat.Dxt1SRgb: return MTLPixelFormat.BC1_RGBA_sRGB;
                case SurfaceFormat.Dxt3SRgb: return MTLPixelFormat.BC3_RGBA_sRGB;
                case SurfaceFormat.Dxt5SRgb: return MTLPixelFormat.BC5_RGSnorm; // TODO: Unsupported / Verify
                case SurfaceFormat.RgbPvrtc2Bpp: return MTLPixelFormat.PVRTC_RGB_2BPP;
                case SurfaceFormat.RgbPvrtc4Bpp: return MTLPixelFormat.PVRTC_RGB_4BPP;
                case SurfaceFormat.RgbaPvrtc2Bpp: return MTLPixelFormat.PVRTC_RGBA_2BPP;
                case SurfaceFormat.RgbaPvrtc4Bpp: return MTLPixelFormat.PVRTC_RGBA_4BPP;
                case SurfaceFormat.RgbEtc1: return MTLPixelFormat.ETC2_RGB8; // TODO: Unsupported / Verify
                case SurfaceFormat.Dxt1a: return MTLPixelFormat.BC1RGBA; // TODO: Unsupported / Verify
                case SurfaceFormat.RgbaAtcExplicitAlpha: return MTLPixelFormat.RGBA8Unorm; // TODO: Unsupported
                case SurfaceFormat.RgbaAtcInterpolatedAlpha: return MTLPixelFormat.RGBA8Unorm;
                case SurfaceFormat.Rgb8Etc2: return MTLPixelFormat.ETC2_RGB8;
                case SurfaceFormat.Srgb8Etc2: return MTLPixelFormat.ETC2_RGB8_sRGB;
                case SurfaceFormat.Rgb8A1Etc2: return MTLPixelFormat.ETC2_RGB8A1;
                case SurfaceFormat.Srgb8A1Etc2: return MTLPixelFormat.ETC2_RGB8A1_sRGB;
                case SurfaceFormat.Rgba8Etc2: return MTLPixelFormat.ETC2_RGB8A1; // TODO: Unsupported
                case SurfaceFormat.SRgb8A8Etc2: return MTLPixelFormat.ETC2_RGB8A1_sRGB; // TODO: Unsupported
                default: return MTLPixelFormat.RGBA8Unorm;
            }
        }

        private void PlatformSetDataBody<T>(int level, T[] data, int startIndex, int elementCount)
            where T : struct
        {
            MetalSetData(data, startIndex, elementCount);
        }

        private void PlatformSetDataBody<T>(int level, int arraySlice, Rectangle rect, T[] data, int startIndex,
            int elementCount)
            where T : struct
        {
        }

        private void PlatformSetData<T>(int level, T[] data, int startIndex, int elementCount)
            where T : struct
        {
            MetalSetData(data, startIndex, elementCount);
        }

        private void PlatformSetData<T>(int level, int arraySlice, Rectangle rect, T[] data, int startIndex,
            int elementCount)
            where T : struct
        {
        }

        private void PlatformGetData<T>(int level, int arraySlice, Rectangle rect, T[] data, int startIndex,
            int elementCount)
            where T : struct
        {
        }

        [CLSCompliant(false)]
        public static Texture2D FromStream(GraphicsDevice graphicsDevice, UIImage uiImage)
        {
            throw new NotImplementedException("tex 2d from stream not implemented");
        }

        private static Texture2D PlatformFromStream(GraphicsDevice graphicsDevice, CGImage cgImage)
        {
            throw new NotImplementedException("tex 2d from platform stream not implemented");
        }

        private void FillTextureFromStream(Stream stream)
        {
        }

        // This method allows games that use Texture2D.FromStream
        // to reload their textures after the GL context is lost.
        private void PlatformReload(Stream textureStream)
        {
        }

        private void GenerateGLTextureIfRequired()
        {
        }

        public IMTLTexture GetTexture() => _texture;

        protected override void Dispose(bool disposing)
        {
            if (_texture != null)
            {
                _texture.Dispose();
                _texture = null;
            }

            base.Dispose(disposing);
        }

        public override int GetHashCode()
        {
            HashCode code = new();
            code.Add(Width);
            code.Add(Height);
            code.Add(Format);
            if (_texture != null) { code.Add(_texture.Handle); }

            return code.ToHashCode();
        }
    }
}
