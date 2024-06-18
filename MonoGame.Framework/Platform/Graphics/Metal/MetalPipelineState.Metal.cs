// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using CoreGraphics;
using Metal;
using MetalKit;
using GD = Microsoft.Xna.Framework.Graphics.GraphicsDebug;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Maintains the pipeline state along with usage count/diry state. Pipeline states are relatively
    /// expensive to create and hence an LRU-based cache may use the <see cref="UsageCount"/> to evict
    /// old ones.
    /// </summary>
    internal class MetalPipelineState : IDisposable
    {
        internal IMTLRenderPipelineState PipelineState { get; set; }
        internal MTLRenderPipelineDescriptor PipelineDescriptor { get; set; }
        internal uint UsageCount { get; set; }

        internal bool RequiresUpdate()
        {
            UsageCount++;
            if (UsageCount == 1) { return true; }

            return false;
        }

        public void Dispose()
        {
            PipelineState?.Dispose();
            PipelineState = null;
            PipelineDescriptor?.Dispose();
            PipelineDescriptor = null;
        }
    }
}
