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
using Urho;

namespace MHUrho.Droid {
	public class FileManagerDroid : FileManager {

		readonly AssetManager assetManager;

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
			if (System.IO.File.Exists(Path.Combine(DynamicDirPath, relativePath))) {
				return OpenDynamicFile(relativePath, FileMode.Open, FileAccess.ReadWrite);
			}

			CopyStaticToDynamic(relativePath);
			return OpenDynamicFile(relativePath, FileMode.Open, FileAccess.ReadWrite);
		}

		public override Stream OpenDynamicFile(string relativePath, FileMode fileMode, FileAccess fileAccess) {
			return new FileStream(Path.Combine(DynamicDirPath, relativePath), fileMode, fileAccess);
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

		public override void Copy(string @from, string to, bool overrideFiles)
		{
			throw new NotImplementedException();
		}

		public override bool FileExists(string path)
		{
			return System.IO.File.Exists(path);
		}

		public override bool DirectoryExists(string path)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<string> GetFSEntriesInDirectory(string dirPath, bool files, bool directories)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<string> GetFSEntriesInDirectory(string dirPath,
															bool files,
															bool directories,
															string searchPattern,
															SearchOption searchOption)
		{
			throw new NotImplementedException();
		}

		public override void DeleteDynamicFile(string relativePath)
		{
			throw new NotImplementedException();
		}

		public static FileManagerDroid LoadConfig(AssetManager assetManager) {
			//TODO: Load config files
			return new FileManagerDroid(
				"PackageDirectory",
				assetManager);
		}

		protected FileManagerDroid(string packageDirectoryPath, AssetManager assetManager)
			: base( packageDirectoryPath,
					"TODO",
					"/apk",
					System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
					System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),"Log"),
					"SavedGames"){
			PackageDirectoryPath = packageDirectoryPath;
			this.assetManager = assetManager;
		}

		void CopyFile(string srcRelativePath) {
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