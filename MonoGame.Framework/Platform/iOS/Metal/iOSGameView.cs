// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using Foundation;
using CoreGraphics;
using Metal;
using MetalKit;
using Microsoft.Xna.Framework.Graphics;
using GD = Microsoft.Xna.Framework.Graphics.GraphicsDebug;

namespace Microsoft.Xna.Framework
{
    [Register("iOSGameView")]
    partial class iOSGameView : MTKView
    {
        private readonly iOSGamePlatform _platform;

        private IGraphicsMetalDeviceDelegate _delegate;

        public iOSGameView(iOSGamePlatform platform, CGRect frame)
            : base(frame, MTLDevice.SystemDefault)
        {
            if (platform == null) { throw new ArgumentNullException("platform"); }

            _platform = platform;
            Initialize();
            GD.C("--Start game view");
        }

        private void Initialize()
        {
#if !TVOS
            MultipleTouchEnabled = true;
#endif
            Opaque = true;

            Device = MTLDevice.SystemDefault;
            SampleCount = 1;
            DepthStencilPixelFormat = MTLPixelFormat.Depth32Float_Stencil8;
            PreferredFramesPerSecond = 60;
            ClearColor = new MTLClearColor(0.0, 0.0, 0.0, 1.0);

            // Disable on-demand display, since we are a game.
            EnableSetNeedsDisplay = false;
            Paused = false;
            SetNeedsDisplay();
        }

        [Export("Draw")]
        public override void Draw(CGRect rect)
        {
            try
            {
                if (_platform.ShouldDraw())
                {
                    GD.Spam("-------- Frame Begin ---------------");
                    _delegate?.PreDrawFrame(this);
                    _platform.Tick();
                    _delegate?.PostDrawFrame(this);
                    GD.Spam("-------- Frame End ---------------");
                }
            }
            catch (Exception e)
            {
                GD.C($" ---- RENDER EXCEPTION \n       {e}");
                throw;
            }
        }

        public void SetupGraphicsDevice(IGraphicsMetalDeviceDelegate graphicsDevice)
        {
            _delegate = graphicsDevice;
            _delegate.InitializeMetal(Device, this);
        }

        public void HandleOrientationChange()
        {
            _delegate.DrawableSizeWillChange(this, this.DrawableSize);
        }
    }
}
