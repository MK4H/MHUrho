﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MHUrho;
using System.IO;
using Java.IO;
using Android.Content.Res;
using System.Diagnostics;

namespace MHUrho.Droid {
    public class ConfigManagerDroid : ConfigManager {

        private readonly AssetManager assetManager;

        public override Stream GetStaticFileRO(string relativePath) {
            return assetManager.Open(relativePath);
        }

        public override Stream GetDynamicFile(string relativePath) {
            Java.IO.File file = new Java.IO.File(Path.Combine(DynamicFilePath, relativePath));
            if (file.Exists()) {
                return new FileStream(file.AbsolutePath, FileMode.Open, FileAccess.ReadWrite);
            }

            file = new Java.IO.File(Path.Combine(StaticFilePath, relativePath));
            if (file.Exists()) {
                CopyStaticToDynamic(relativePath);
                return new FileStream(Path.Combine(DynamicFilePath, relativePath), FileMode.Open, FileAccess.ReadWrite);
            }

            throw new System.IO.FileNotFoundException("File was not found in dynamic nor in static files",
                relativePath);
        }

        public override void CopyStaticToDynamic(string srcRelativePath) {
            try {
                string[] subassets = assetManager.List(srcRelativePath);
                if (subassets.Length == 0) {
                    CopyFile(srcRelativePath);
                }
                else {
                    var dir = new Java.IO.File(Path.Combine(DynamicFilePath, srcRelativePath));
                    if (!dir.Exists()) {
                        dir.Mkdirs();
                    }

                    foreach (var file in subassets) {
                        CopyStaticToDynamic(Path.Combine(srcRelativePath, file));
                    }
                }
            }
            catch (System.IO.IOException e) {
                Urho.IO.Log.Write(Urho.LogLevel.Error, $"Copy of static file to dynamic directory failed: {e}");
                if (Debugger.IsAttached) Debugger.Break();
            }
            catch (Java.IO.IOException e) {
                Urho.IO.Log.Write(Urho.LogLevel.Error, $"Copy of static file to dynamic directory failed: {e}");
                if (Debugger.IsAttached) Debugger.Break();
            }
        }

        public static ConfigManagerDroid LoadConfig(AssetManager assetManager) {
            //TODO: Load config files
            return new ConfigManagerDroid(new List<string>()
                {
                    Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),"Packages")
                },assetManager);
        }

        protected ConfigManagerDroid(List<string> packagePaths, AssetManager assetManager)
            : base(packagePaths,"TODO","TODO","/apk",System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)){
            PackagePaths = packagePaths;
            this.assetManager = assetManager;
        }

        private void CopyFile(string srcRelativePath) {
            //TODO: Exceptions

            using (var srcFile = assetManager.Open(srcRelativePath)) {
                using (var dstFile = System.IO.File.Create(Path.Combine(DynamicFilePath, srcRelativePath))){
                    srcFile.CopyTo(dstFile);
                }
            }
        }
    }
}