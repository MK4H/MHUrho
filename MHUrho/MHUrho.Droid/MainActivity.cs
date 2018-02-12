using Android.App;
using Android.Content.PM;
using Android.Views;
using Android.Widget;
using Android.OS;
using Org.Libsdl.App;
using Urho;
using Urho.Droid;
using MHUrho;
using Android.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MHUrho.Droid
{
    [Activity(Label = "MHUrho.Droid", MainLauncher = true,
        Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
        ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Landscape)]
    public class MainActivity : Activity
    {
        MyGame myGame;

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            var decorView = Window.DecorView;

            var uiOptions = (int)decorView.SystemUiVisibility;

            uiOptions |= (int)SystemUiFlags.LowProfile;
            uiOptions |= (int)SystemUiFlags.Fullscreen;
            uiOptions |= (int)SystemUiFlags.HideNavigation;
            uiOptions |= (int)SystemUiFlags.ImmersiveSticky;

            decorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;

            var layout = new FrameLayout(this);
            var surface = UrhoSurface.CreateSurface(this);
            layout.AddView(surface);
            SetContentView(layout);

            MyGame.Config = ConfigManagerDroid.LoadConfig(Assets);

            MyGame.Config.CopyStaticToDynamic("Data/Test");

            myGame = await surface.Show<MyGame>(new ApplicationOptions("Data"));
            //to stop the game use await surface.Stop().
        }


        protected override void OnResume()
        {
            UrhoSurface.OnResume();
            base.OnResume();
        }

        protected override void OnPause()
        {
            UrhoSurface.OnPause();
            base.OnPause();
        }

        public override void OnLowMemory()
        {
            UrhoSurface.OnLowMemory();
            base.OnLowMemory();
        }

        protected override void OnDestroy()
        {
            UrhoSurface.OnDestroy();
            base.OnDestroy();
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            if (e.KeyCode == Android.Views.Keycode.Back)
            {
                this.Finish();
                return false;
            }

            return base.DispatchKeyEvent(e);
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            UrhoSurface.OnWindowFocusChanged(hasFocus);
            base.OnWindowFocusChanged(hasFocus);
        }


        
    }
}

