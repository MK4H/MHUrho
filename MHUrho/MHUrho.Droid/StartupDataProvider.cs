using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MHUrhoStandard;
using Urho;
using Urho.IO;
using Exception = System.Exception;

namespace MHUrho.Droid
{
    public class StartupDataProvider : IStartupDataProvider
    {
        private readonly AssetManager assets;
        public Stream GetFile(string relativePath)
        {
            try
            {
                //TODO: Exceptions
                return assets.Open(relativePath);
            }
            catch (Java.IO.IOException e)
            {
                Log.Write(LogLevel.Error, $"Error getting file from package: {e}");
                if (Debugger.IsAttached) Debugger.Break();
            }

            return null;
        }

        public void Init(FileSystem fs)
        {
            string srcpath = System.IO.Path.Combine("/apk", 
                "Data", "Test","Assemblies","TestPlugin.dll");
            string dstpath =
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            fs.Copy(srcpath, dstpath);
        }

        public StartupDataProvider(AssetManager assets)
        {
            this.assets = assets;
        }
    }
}