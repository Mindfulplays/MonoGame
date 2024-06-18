// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.IO;
using Foundation;
using UIKit;
using CoreAnimation;
using ObjCRuntime;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using GD = Microsoft.Xna.Framework.Graphics.GraphicsDebug;

namespace Microsoft.Xna.Framework
{
    class iOSGamePlatform : GamePlatform
    {
        private iOSGameViewController _viewController;
        private UIWindow _mainWindow;
        private List<NSObject> _applicationObservers;

        public iOSGamePlatform(Game game) :
            base(game)
        {
            game.Services.AddService(typeof(iOSGamePlatform), this);

            //This also runs the TitleContainer static constructor, ensuring it is done on the main thread
            Directory.SetCurrentDirectory(TitleContainer.Location);

            _applicationObservers = new List<NSObject>();

#if !TVOS
            UIApplication.SharedApplication.SetStatusBarHidden(true, UIStatusBarAnimation.Fade);
#endif

            // Create a full-screen window
            _mainWindow = new UIWindow(UIScreen.MainScreen.Bounds);
            //_mainWindow.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;

            game.Services.AddService(typeof(UIWindow), _mainWindow);

            _viewController = new iOSGameViewController(this);
            game.Services.AddService(typeof(UIViewController), _viewController);
            Window = new iOSGameWindow(_viewController);
            Window.Title = "MonoGame Window";
            _mainWindow.Add(_viewController.View);

            _viewController.InterfaceOrientationChanged += ViewController_InterfaceOrientationChanged;
        }

        public override void TargetElapsedTimeChanged()
        {
        }

        public override GameRunBehavior DefaultRunBehavior { get { return GameRunBehavior.Asynchronous; } }

        // FIXME: VideoPlayer 'needs' this to set up its own movie player view
        //        controller.
        public iOSGameViewController ViewController { get { return _viewController; } }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (_viewController != null)
                {
                    _viewController.View.RemoveFromSuperview();
                    _viewController.RemoveFromParentViewController();
                    _viewController.Dispose();
                    _viewController = null;
                }

                if (_mainWindow != null)
                {
                    _mainWindow.Dispose();
                    _mainWindow = null;
                }
            }
        }

        public override void BeforeInitialize()
        {
            base.BeforeInitialize();

            _viewController.View.LayoutSubviews();
            var gdm = (GraphicsDeviceManager)Game.Services.GetService(typeof(IGraphicsDeviceManager));
            if (gdm?.GraphicsDevice == null) { GD.C("Error setting up - no graphics device."); }

            GD.C("Setting up");

            try { _viewController.View.SetupGraphicsDevice(gdm.GraphicsDevice); }
            catch (Exception e)
            {
                GD.C($" ---- RENDER EXCEPTION \n       {e}");
                throw;
            }
        }

        public override void RunLoop()
        {
            throw new NotSupportedException("The iOS platform does not support synchronous run loops");
        }

        public override void StartRunLoop()
        {
            // Show the window
            _mainWindow.MakeKeyAndVisible();

            // In iOS 8+ we need to set the root view controller *after* Window MakeKey
            // This ensures that the viewController's supported interface orientations
            // will be respected at launch
            _mainWindow.RootViewController = _viewController;

            BeginObservingUIApplication();

            _viewController.View.BecomeFirstResponder();
        }

        public override bool BeforeDraw(GameTime gameTime)
        {
            if (Game.Window is iOSGameWindow window) { window._UpdateClientBounds(); }

            return true;
        }

        public override bool BeforeUpdate(GameTime gameTime)
        {
            return true;
        }

        public override void EnterFullScreen()
        {
            // Do nothing: iOS games are always full screen
        }

        public override void ExitFullScreen()
        {
            // Do nothing: iOS games are always full screen
        }

        public override void Exit()
        {
            // Do Nothing: iOS games do not "exit" or shut down.
        }

        private void BeginObservingUIApplication()
        {
            var events = new Tuple<NSString, Action<NSNotification>>[]
            {
                Tuple.Create(
                    UIApplication.DidBecomeActiveNotification,
                    new Action<NSNotification>(Application_DidBecomeActive)),
                Tuple.Create(
                    UIApplication.WillResignActiveNotification,
                    new Action<NSNotification>(Application_WillResignActive)),
                Tuple.Create(
                    UIApplication.WillTerminateNotification,
                    new Action<NSNotification>(Application_WillTerminate)),
            };

            foreach (var entry in events)
                _applicationObservers.Add(NSNotificationCenter.DefaultCenter.AddObserver(entry.Item1, entry.Item2));
        }

#region Notification Handling

        private void Application_DidBecomeActive(NSNotification notification)
        {
            // See note below in `Application_WillResignActive`
            // IsActive = true;
#if TVOS
            _viewController.ControllerUserInteractionEnabled = false;
#endif
            //TouchPanel.Reset();
        }

        private void Application_WillResignActive(NSNotification notification)
        {
            // NOTE: In the OpenGL iOSGamePlatform, we used to set IsActive to be false.
            //       This causes the App Switcher view to stop drawing / freeze.
            //       In Metal-based apps, we can keep presenting frames to keep the app
            //       switcher UI fresh. The OS will take care of suppressing Drawing
            //       at the right moment (for example, going to home screen or turning off
            //       the screen/timeout, or when switching between apps).
            // IsActive = false;
        }

        private void Application_WillTerminate(NSNotification notification)
        {
        }

#endregion Notification Handling

#region Helper Property

        private DisplayOrientation CurrentOrientation
        {
            get
            {
#if TVOS
                return DisplayOrientation.LandscapeLeft;
#else
                return OrientationConverter.ToDisplayOrientation(_viewController.InterfaceOrientation);
#endif
            }
        }

#endregion

        private void ViewController_InterfaceOrientationChanged(object sender, EventArgs e)
        {
            var orientation = CurrentOrientation;

            // FIXME: The presentation parameters for the GraphicsDevice should
            //        be managed by the GraphicsDevice itself.  Not by
            //        iOSGamePlatform.
            var gdm = (GraphicsDeviceManager)Game.Services.GetService(typeof(IGraphicsDeviceManager));

            TouchPanel.DisplayOrientation = orientation;

            if (gdm != null)
            {
                if (Game.Window is iOSGameWindow window)
                {
                    var rect = window._UpdateClientBounds();
                    gdm.PreferredBackBufferWidth = rect.Width;
                    gdm.PreferredBackBufferHeight = rect.Height;
                    var presentParams = gdm.GraphicsDevice.PresentationParameters;
                    presentParams.BackBufferWidth = rect.Width;
                    presentParams.BackBufferHeight = rect.Height;
                    presentParams.DisplayOrientation = orientation;
                    gdm.GraphicsDevice.Viewport = new(0, 0, rect.Width, rect.Height);
                    GD.C($" -- Orientation changed {rect.Width}x{rect.Height}");

                    // Recalculate our views.
                    ViewController.View.LayoutSubviews();

                    gdm.ApplyChanges();
                }
            }
        }

        public override void BeginScreenDeviceChange(bool willBeFullScreen)
        {
        }

        public override void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight)
        {
        }

        // Prevent unnecessary drawing if there is nothing to draw.
        // This is important for Metal as an empty CommandQueue/Buffer without
        // a render encoder will cause tearing.
        internal bool ShouldDraw() => Game.IsActive;

        internal void Tick()
        {
            if (!Game.IsActive)
                return;

            Game.Tick();
            Threading.Run();
        }
    }
}
