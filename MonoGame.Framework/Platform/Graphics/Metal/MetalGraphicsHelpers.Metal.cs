// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.InteropServices;
using Foundation;
using Metal;
using GD = Microsoft.Xna.Framework.Graphics.GraphicsDebug;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Conversion utilities between <see cref="MonoGame"/> and <see cref="MetalKit"/> code.
    /// </summary>
    internal class MetalGraphicsHelpers
    {
        internal const int MAX_INLINE_BUFFER_LENGTH_BYTES = 4 * 1024; // 4 KB

        public static void ToMTLClearColor(Vector4 fromColor, ref MTLClearColor toColor)
        {
            toColor.Red = fromColor.X;
            toColor.Green = fromColor.Y;
            toColor.Blue = fromColor.Z;
            toColor.Alpha = fromColor.W;
        }

        public static void ToMTLRect(Rectangle mgRect, ref MTLScissorRect mtlRect)
        {
            mtlRect.X = (UIntPtr)mgRect.Left;
            mtlRect.Y = (UIntPtr)mgRect.Top;
            mtlRect.Width = (UIntPtr)mgRect.Width;
            mtlRect.Height = (UIntPtr)mgRect.Height;
        }

        public static MTLBlendOperation ToMTLBlendOperation(BlendFunction mgBlendFunc)
        {
            switch (mgBlendFunc)
            {
                case BlendFunction.Add: return MTLBlendOperation.Add;
                case BlendFunction.Subtract: return MTLBlendOperation.Subtract;
                case BlendFunction.ReverseSubtract: return MTLBlendOperation.ReverseSubtract;
                case BlendFunction.Min: return MTLBlendOperation.Min;
                case BlendFunction.Max: return MTLBlendOperation.Max;
                default: throw new ArgumentOutOfRangeException(nameof(mgBlendFunc), mgBlendFunc, null);
            }
        }

        /// <summary>Convertes MonoGame <see cref="Blend"/> to Metal's <see cref="MTLBlendFactor"/> </summary>
        public static MTLBlendFactor ToMTLBlendFactor(Blend mgBlendFactor)
        {
            switch (mgBlendFactor)
            {
                case Blend.One: return MTLBlendFactor.One;
                case Blend.Zero: return MTLBlendFactor.Zero;
                case Blend.SourceColor: return MTLBlendFactor.SourceColor;
                // Careful here: There are things like `MTLBlendFactor.OneMinusSource1Color`
                //               (note `Source1` not `Source`) which don't work when there
                //               is exactly one source.
                case Blend.InverseSourceColor: return MTLBlendFactor.OneMinusSourceColor;
                case Blend.SourceAlpha: return MTLBlendFactor.SourceAlpha;
                case Blend.InverseSourceAlpha: return MTLBlendFactor.OneMinusSourceAlpha;
                case Blend.DestinationColor: return MTLBlendFactor.DestinationColor;
                case Blend.InverseDestinationColor: return MTLBlendFactor.OneMinusDestinationColor;
                case Blend.DestinationAlpha: return MTLBlendFactor.DestinationAlpha;
                case Blend.InverseDestinationAlpha: return MTLBlendFactor.OneMinusDestinationAlpha;
                case Blend.BlendFactor: return MTLBlendFactor.BlendColor;
                case Blend.InverseBlendFactor: return MTLBlendFactor.OneMinusBlendColor;
                case Blend.SourceAlphaSaturation: return MTLBlendFactor.SourceAlphaSaturated;
                default: throw new ArgumentOutOfRangeException(nameof(mgBlendFactor), mgBlendFactor, null);
            }
        }

        /// <summary>
        /// Converts MonoGame <see cref="ColorWriteChannels"/> to Metal's <see cref="MTLColorWriteMask"/>
        /// </summary>
        public static MTLColorWriteMask ToMTLWriteMask(ColorWriteChannels colorWriteChannels)
        {
            switch (colorWriteChannels)
            {
                case ColorWriteChannels.None: return MTLColorWriteMask.None;
                case ColorWriteChannels.Red: return MTLColorWriteMask.Red;
                case ColorWriteChannels.Green: return MTLColorWriteMask.Green;
                case ColorWriteChannels.Blue: return MTLColorWriteMask.Blue;
                case ColorWriteChannels.Alpha: return MTLColorWriteMask.Alpha;
                case ColorWriteChannels.All: return MTLColorWriteMask.All;
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorWriteChannels), colorWriteChannels, null);
            }
        }

        /// <summary>
        /// Helps dispose <see cref="NSObject"/> and other Metal interfaces.
        /// </summary>
        public static void CleanDispose<T>(ref T o) where T : class
        {
            if (o is IDisposable disposable) { disposable.Dispose(); }

            o = null;
        }

        /// <summary>
        /// Helps dispose <see cref="GCHandle"/> by freeing the data pointer held internally.
        /// </summary>
        public static void CleanDispose(ref GCHandle handle)
        {
            handle.Free();
        }

        /// <summary>
        /// Converts <see cref="MTLClearColor"/> to string - useful for printing color to debug stuff.
        /// </summary>
        public static string ToStr(MTLClearColor c)
        {
            return $"rgba({(c.Red*255):#.0}, {(c.Green*255):#.0}, {(c.Blue * 255):#.0}, {(c.Alpha * 255):#.0})";
        }

        /// <summary>
        /// Converts MonoGame internal sampler or depth stencil compare function to Metal's equivalent.
        /// </summary>
        public static MTLCompareFunction ConvertCompareFunction(CompareFunction compareFunction)
        {
            switch (compareFunction)
            {
                case CompareFunction.Always: return MTLCompareFunction.Always;
                case CompareFunction.Never: return MTLCompareFunction.Never;
                case CompareFunction.Less: return MTLCompareFunction.Less;
                case CompareFunction.LessEqual: return MTLCompareFunction.LessEqual;
                case CompareFunction.Equal: return MTLCompareFunction.Equal;
                case CompareFunction.GreaterEqual: return MTLCompareFunction.GreaterEqual;
                case CompareFunction.Greater: return MTLCompareFunction.Greater;
                case CompareFunction.NotEqual: return MTLCompareFunction.NotEqual;
                default:
                    throw new ArgumentOutOfRangeException(nameof(compareFunction), compareFunction, null);
            }
        }

        public static MTLStencilOperation ConvertDepthStencilOperation(StencilOperation stencilOperation)
        {
            switch (stencilOperation)
            {
                case StencilOperation.Keep: return MTLStencilOperation.Keep;
                case StencilOperation.Zero: return MTLStencilOperation.Zero;
                case StencilOperation.Replace: return MTLStencilOperation.Replace;
                // Note Saturation = Clamp; Increment = Wrap;
                case StencilOperation.Increment: return MTLStencilOperation.IncrementWrap;
                case StencilOperation.Decrement: return MTLStencilOperation.DecrementWrap;
                case StencilOperation.IncrementSaturation: return MTLStencilOperation.IncrementClamp;
                case StencilOperation.DecrementSaturation: return MTLStencilOperation.DecrementClamp;
                case StencilOperation.Invert: return MTLStencilOperation.Invert;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stencilOperation), stencilOperation, null);
            }
        }

        public static MTLCullMode ConvertCullMode(CullMode cullMode)
        {
            switch (cullMode)
            {
                case CullMode.None: return MTLCullMode.None;
                case CullMode.CullClockwiseFace: return MTLCullMode.Front;
                case CullMode.CullCounterClockwiseFace: return MTLCullMode.Back;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cullMode), cullMode, null);
            }
        }

        public static MTLTriangleFillMode ConvertFillMode(FillMode fillMode)
        {
            switch (fillMode)
            {
                case FillMode.Solid: return MTLTriangleFillMode.Fill;
                case FillMode.WireFrame: return MTLTriangleFillMode.Lines;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fillMode), fillMode, null);
            }
        }

        public static MTLPrimitiveType ConvertPrimitiveType(PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.LineList: return MTLPrimitiveType.Line;
                case PrimitiveType.LineStrip: return MTLPrimitiveType.LineStrip;
                case PrimitiveType.PointList: return MTLPrimitiveType.Point;
                case PrimitiveType.TriangleList: return MTLPrimitiveType.Triangle;
                case PrimitiveType.TriangleStrip: return MTLPrimitiveType.TriangleStrip;
                default: return MTLPrimitiveType.Triangle;
            }
        }

        public static MTLSamplerMinMagFilter ConvertMinSamplerFilter(TextureFilter filter)
        {
            switch (filter)
            {
                case TextureFilter.MinLinearMagPointMipPoint:
                case TextureFilter.MinLinearMagPointMipLinear:
                case TextureFilter.LinearMipPoint:
                case TextureFilter.Linear:
                default:
                {
                    return MTLSamplerMinMagFilter.Linear;
                }
                case TextureFilter.MinPointMagLinearMipLinear:
                case TextureFilter.PointMipLinear:
                case TextureFilter.Point:
                case TextureFilter.MinPointMagLinearMipPoint:
                {
                    return MTLSamplerMinMagFilter.Nearest;
                }
            }
        }

        public static MTLSamplerMinMagFilter ConvertMagSamplerFilter(TextureFilter filter)
        {
            switch (filter)
            {
                case TextureFilter.MinPointMagLinearMipPoint:
                case TextureFilter.MinPointMagLinearMipLinear:
                case TextureFilter.LinearMipPoint:
                case TextureFilter.Linear:
                default:
                {
                    return MTLSamplerMinMagFilter.Linear;
                }
                case TextureFilter.MinLinearMagPointMipLinear:
                case TextureFilter.MinLinearMagPointMipPoint:
                case TextureFilter.PointMipLinear:
                case TextureFilter.Point:
                {
                    return MTLSamplerMinMagFilter.Nearest;
                }
            }
        }

        public static MTLSamplerMipFilter ConvertMipSamplerFilter(TextureFilter filter)
        {
            switch (filter)
            {
                case TextureFilter.MinPointMagLinearMipLinear:
                case TextureFilter.MinLinearMagPointMipLinear:
                case TextureFilter.LinearMipPoint:
                case TextureFilter.PointMipLinear:
                {
                    return MTLSamplerMipFilter.Linear;
                }
                case TextureFilter.MinLinearMagPointMipPoint:
                case TextureFilter.MinPointMagLinearMipPoint:
                {
                    return MTLSamplerMipFilter.Nearest;
                }
                case TextureFilter.Point:
                case TextureFilter.Linear:
                default:
                {
                    // Default to no mipmaps.
                    return MTLSamplerMipFilter.NotMipmapped;
                }
            }
        }

        public static MTLSamplerAddressMode ConvertSamplerAddressMode(TextureAddressMode addressMode)
        {
            switch (addressMode)
            {
                case TextureAddressMode.Wrap: return MTLSamplerAddressMode.Repeat;
                case TextureAddressMode.Clamp: return MTLSamplerAddressMode.ClampToEdge;
                // Same as Wrap but flipped.
                case TextureAddressMode.Mirror: return MTLSamplerAddressMode.MirrorRepeat;
                // Border color only available on iOS 14+. Crashes on low-end devices.
                // case TextureAddressMode.Border: return MTLSamplerAddressMode.ClampToBorderColor;
                default:
                    throw new ArgumentOutOfRangeException(nameof(addressMode), addressMode, null);
            }
        }

        public static MTLSamplerBorderColor ConvertBorderColor(Color color)
        {
            if (color.A > 0) { return MTLSamplerBorderColor.TransparentBlack; }

            if (color.R == 0 && color.G == 0 && color.B == 0) return MTLSamplerBorderColor.OpaqueBlack;
            return MTLSamplerBorderColor.OpaqueWhite;
        }
    }
}
