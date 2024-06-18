// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using Metal;
using GD = Microsoft.Xna.Framework.Graphics.GraphicsDebug;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class VertexDeclaration
    {
        private MTLVertexDescriptor _vertexDesc;
        internal string HashKey => $"vertexid_{GetHashCode()}";

        /// <summary>
        /// Converts MonoGame internal <see cref="T"/> to Metal's
        /// <see cref="https://developer.apple.com/documentation/metal/mtlvertexdescriptor"/>
        /// </summary>
        internal void Apply(Shader shader, MTLRenderPipelineDescriptor desc)
        {
            if (_vertexDesc == null)
            {
                _vertexDesc = new MTLVertexDescriptor();

                int offset = 0;
                for (var i = 0; i < InternalVertexElements.Length; i++)
                {
                    var el = InternalVertexElements[i];
                    var vertexIndex = shader.GetAttribLocation(el.VertexElementUsage, i);
                    _vertexDesc.Attributes[vertexIndex].Format = _ToMtlVertexFormat(el.VertexElementFormat);
                    _vertexDesc.Attributes[vertexIndex].BufferIndex = 1;
                    _vertexDesc.Attributes[vertexIndex].Offset = (UIntPtr)offset;
                    
                    // Adjust next element's offset based on the current element's size.
                    offset += _ByteSize(el.VertexElementFormat);
                    //GD.C(
                    //    $"Adding {i} / {vertexIndex} vertex element: {el.VertexElementFormat} / {_vertexDesc.Attributes[vertexIndex].Format} / current {_vertexDesc.Attributes[vertexIndex].Offset} / next : {offset}");
                }

                // Layouts[0] points to [[stage_in]]
                // See https://github.com/KhronosGroup/SPIRV-Cross/issues/792#issuecomment-1585946911
                _vertexDesc.Layouts[1].Stride = (UIntPtr)VertexStride;
                _vertexDesc.Layouts[1].StepRate = 1;
                _vertexDesc.Layouts[1].StepFunction = MTLVertexStepFunction.PerVertex;
            }

            desc.VertexDescriptor = _vertexDesc;
        }

        /// <summary>
        /// Calculates the size per-vertex-element.
        /// </summary>
        private int _ByteSize(VertexElementFormat mgVertexFormat)
        {
            switch (mgVertexFormat)
            {
                case VertexElementFormat.Single: return 1 * sizeof(float);
                case VertexElementFormat.Vector2: return 2 * sizeof(float);
                case VertexElementFormat.Vector3: return 3 * sizeof(float);
                case VertexElementFormat.Vector4: return 4 * sizeof(float);
                case VertexElementFormat.Color: return 4 * sizeof(byte);
                case VertexElementFormat.Byte4: return 4 * sizeof(byte);
                case VertexElementFormat.Short2: return 2 * sizeof(float) / 2;
                case VertexElementFormat.Short4: return 4 * sizeof(float) / 2;
                case VertexElementFormat.NormalizedShort2: return 2 * sizeof(float) / 2;
                case VertexElementFormat.NormalizedShort4: return 4 * sizeof(float) / 2;
                case VertexElementFormat.HalfVector2: return 2 * sizeof(float) / 2;
                case VertexElementFormat.HalfVector4: return 4 * sizeof(float) / 2;
                default: return 1 * sizeof(float);
            }
        }

        /// <summary>
        /// Converts MonoGame internal vertex element format to
        /// <see cref="https://developer.apple.com/documentation/metal/mtlvertexformat"/>.
        /// </summary>
        private MTLVertexFormat _ToMtlVertexFormat(VertexElementFormat mgVertexFormat)
        {
            switch (mgVertexFormat)
            {
                case VertexElementFormat.Single: return MTLVertexFormat.Float;
                case VertexElementFormat.Vector2: return MTLVertexFormat.Float2;
                case VertexElementFormat.Vector3: return MTLVertexFormat.Float3;
                case VertexElementFormat.Vector4: return MTLVertexFormat.Float4;
                case VertexElementFormat.Color:
                    return BitConverter.IsLittleEndian
                        ? MTLVertexFormat.UChar4Normalized
                        : MTLVertexFormat.UChar4NormalizedBgra;
                case VertexElementFormat.Byte4:
                    return BitConverter.IsLittleEndian
                        ? MTLVertexFormat.UChar4Normalized
                        : MTLVertexFormat.UChar4NormalizedBgra;
                case VertexElementFormat.Short2: return MTLVertexFormat.Short2;
                case VertexElementFormat.Short4: return MTLVertexFormat.Short4;
                case VertexElementFormat.NormalizedShort2: return MTLVertexFormat.Short2Normalized;
                case VertexElementFormat.NormalizedShort4: return MTLVertexFormat.ShortNormalized;
                case VertexElementFormat.HalfVector2: return MTLVertexFormat.Half2;
                case VertexElementFormat.HalfVector4: return MTLVertexFormat.Half4;
                default: return MTLVertexFormat.Float;
            }
        }
    }
}
