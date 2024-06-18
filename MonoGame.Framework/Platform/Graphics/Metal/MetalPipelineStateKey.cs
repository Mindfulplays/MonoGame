// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// A stable key that identifies a unique pipeline state based on its dependencies such as
    /// shaders, blend states etc.
    /// </summary>
    internal class MetalPipelineStateKey
    {
        internal Shader VertexShader { get; set; }
        internal Shader PixelShader { get; set; }
        internal BlendState BlendState { get; set; }
        internal MetalRenderPass RenderPass { get; set; }
        public MetalPipelineStateKey()
        {
        }

        public MetalPipelineStateKey(MetalPipelineStateKey that)
        {
            this.Update(that.VertexShader, that.PixelShader, that.BlendState, that.RenderPass);
        }
        public MetalPipelineStateKey Update(Shader vertexShader, Shader pixelShader, BlendState blendState, MetalRenderPass renderPass)
        {
            VertexShader = vertexShader;
            PixelShader = pixelShader;
            BlendState = blendState;
            RenderPass = renderPass;
            return this;
        }

        // Generated using ReSharper. If you add more members above, please regenerate the code appropriately below.
        protected bool Equals(MetalPipelineStateKey other)
        {
            return Equals(VertexShader, other.VertexShader) && Equals(PixelShader, other.PixelShader) && Equals(BlendState, other.BlendState) && Equals(RenderPass, other.RenderPass);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MetalPipelineStateKey)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VertexShader, PixelShader, BlendState, RenderPass);
        }

        public override string ToString()
        {
            return
                $"Vertex Shader: {VertexShader.HashKey} / Pixel Shader: {PixelShader.HashKey} / blend state: {BlendState} / render pass # {RenderPass}";
        }

        public string GetComparison(MetalPipelineStateKey other)
        {
            var ret = $" ({GetHashCode()} vs {other.GetHashCode()} / equals? {this.Equals(other)}) ";
            if (!Equals(VertexShader, other.VertexShader))
            {
                ret += $" [Vertex shader differs: {VertexShader} vs {other.VertexShader}] ";
            }

            if (!Equals(PixelShader, other.PixelShader))
            {
                ret += $" [Pixel shader differs: {PixelShader} vs {other.PixelShader}] ";
            }

            if (!Equals(BlendState, other.BlendState))
            {
                ret += $" [Blend State differs: {BlendState} vs {other.BlendState}]";
            }

            if (!Equals(RenderPass, other.RenderPass))
            {
                ret += $" [Render pass differs: {RenderPass} vs {other.RenderPass}] ";
            }

            return ret;
        }
    }
}
