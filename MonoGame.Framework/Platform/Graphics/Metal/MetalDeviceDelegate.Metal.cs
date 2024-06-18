// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using CoreGraphics;
using Metal;
using MetalKit;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>Encodes device errors that ensures the Metal code checks before proceeding.</summary>
    public enum GraphicsDeviceError
    {
        /// <summary>
        /// No error.
        /// </summary>
        None = 0,

        /// <summary>
        /// Unable to create the command buffer for the current frame (a recoverable error).
        /// </summary>
        CommandBufferCreateError = 1,


        /// <summary>
        /// Unable to create the render encoder for the current frame (a recoverable error).
        /// </summary>
        NoRenderEncoderAvailable = 2,

        /// <summary>
        /// Unable to create the pipeline state (an unrecoverable error).
        /// </summary>
        RenderPipelineStateError = 3,

        /// <summary>
        /// Unable to initialize Metal (an unrecoverable error).
        /// </summary>
        InitializationError = 4,
        
    }

    /// summary>
    /// A delegate that the <see cref="iOSGameView"/> and other platform classes can use to interact
    /// with the <see cref="GraphicsDevice"/>.
    /// </summary>
    public interface IGraphicsMetalDeviceDelegate
    {
        /// <summary>
        /// Initializes the <see cref="MTLDevice"/> with the specific <see cref="MTKView"/>
        /// including setting up the default frame-buffer render-pass related objects.
        /// </summary>
        public void InitializeMetal(IMTLDevice device, MTKView view);

        /// <summary>
        /// Invoked when the view's size changes.
        /// </summary>
        public void DrawableSizeWillChange(MTKView view, CGSize size);

        /// <summary>
        /// Invoked before the render-pass for the current frame begins.
        /// </summary>
        void PreDrawFrame(MTKView view);

        /// <summary>
        /// Invoked after the render-pass for the current frame has finished.
        /// </summary>
        void PostDrawFrame(MTKView view);
    }
}
