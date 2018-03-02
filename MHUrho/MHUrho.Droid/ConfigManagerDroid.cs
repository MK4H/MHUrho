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
            Java.IO.File file = new Java.IO.File(Path.Combine(DynamicDirPath, relativePath));
            if (file.Exists()) {
                if (file.IsFile) {
                    return new FileStream(file.AbsolutePath, FileMode.Open, FileAccess.ReadWrite);
                }
              
                throw new System.IO.IOException($"Cannot open directory as a file: {file.AbsolutePath}"); 
            }

            file = new Java.IO.File(Path.Combine(StaticFilePath, relativePath));
            if (file.Exists()) {
                if (file.IsFile) {
                    CopyStaticToDynamic(relativePath);
                    return new FileStream(Path.Combine(DynamicDirPath, relativePath), FileMode.Open, FileAccess.ReadWrite);
                }

                throw new System.IO.IOException($"Cannot open directory as a file: {file.AbsolutePath}");
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
                    var dir = new Java.IO.File(Path.Combine(DynamicDirPath, srcRelativePath));
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
            return new ConfigManagerDroid(
                new List<string>()
                {
                    Path.Combine("Data","Test","ResourceDir","DirDescription.xml")
                },
                assetManager);
        }

        protected ConfigManagerDroid(List<string> packagePaths, AssetManager assetManager)
            : base( packagePaths,
                    "TODO",
                    "TODO",
                    "/apk",
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                    System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),"Log")){
            PackagePaths = packagePaths;
            this.assetManager = assetManager;
        }

        private void CopyFile(string srcRelativePath) {
            //TODO: Exceptions

            using (var srcFile = assetManager.Open(srcRelativePath)) {
                using (var dstFile = System.IO.File.Create(Path.Combine(DynamicDirPath, srcRelativePath))){
                    srcFile.CopyTo(dstFile);
                }
            }
        }
    }
}