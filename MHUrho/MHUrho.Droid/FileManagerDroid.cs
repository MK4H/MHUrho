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
using Urho;

namespace MHUrho.Droid {
    public class FileManagerDroid : FileManager {

        private readonly AssetManager assetManager;

        public override Stream OpenStaticFileRO(string relativePath) {
            try {
                return assetManager.Open(relativePath);
            }
            catch (Exception e) {
                Urho.IO.Log.Write(LogLevel.Error, $"Could not open file {relativePath}, error: {e}");
                return null;
            }
        }

        public override Stream OpenStaticFileRW(string relativePath) {
            throw new NotImplementedException();
        }

        public override Stream OpenDynamicFile(string relativePath, FileMode fileMode, FileAccess fileAccess) {
            Java.IO.File file = new Java.IO.File(Path.Combine(DynamicDirPath, relativePath));
            if (file.Exists()) {
                if (file.IsFile) {
                    return new FileStream(file.AbsolutePath, fileMode, fileAccess);
                }
              
                throw new System.IO.IOException($"Cannot open directory as a file: {file.AbsolutePath}"); 
            }

            CopyStaticToDynamic(relativePath);

            return new FileStream(file.AbsolutePath, fileMode, fileAccess);
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

        public static FileManagerDroid LoadConfig(AssetManager assetManager) {
            //TODO: Load config files
            return new FileManagerDroid(
                new List<string>()
                {
                    Path.Combine("Data","Test","ResourceDir","DirDescription.xml")
                },
                assetManager);
        }

        protected FileManagerDroid(List<string> packagePaths, AssetManager assetManager)
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
            string path = Path.Combine(DynamicDirPath, srcRelativePath);

            using (var srcFile = assetManager.Open(srcRelativePath)) {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var dstFile = System.IO.File.Create(path)){
                    srcFile.CopyTo(dstFile);
                }
            }
        }
    }
}