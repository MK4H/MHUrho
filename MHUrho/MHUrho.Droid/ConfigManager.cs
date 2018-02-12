using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MHUrhoStandard;
using System.IO;
using Android.Content.Res;

namespace MHUrho.Droid
{
    public class ConfigManager : IConfigManager
    {
        public List<string> PackagePaths { get; private set; } 
        public void AddPackagePath(string absolutePath)
        {
            throw new NotImplementedException();
        }

        public void RemovePackagePath(string absolutePath)
        {
            throw new NotImplementedException();
        }

        public static ConfigManager LoadConfig(AssetManager assets)
        {
            //TODO: Load config files
            return new ConfigManager(new List<string>()
                {
                    Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),"Packages")
                });
        }

        protected ConfigManager(List<string> packagePaths)
        {
            PackagePaths = packagePaths;

        }
    }
}