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

		readonly AssetManager assetManager;

		public override Stream OpenStaticFileRO(string relativePath) {
			throw new NotImplementedException();
		}

		public override Stream OpenStaticFileRW(string relativePath) {
			throw new NotImplementedException();
		}

		public override Stream OpenDynamicFile(string relativePath, FileMode fileMode, FileAccess fileAccess) {
			throw new NotImplementedException();
		}

		public override void CopyStaticToDynamic(string srcRelativePath) {
			throw new NotImplementedException();
		}

		public override void Copy(string source, string target, bool overrideFiles)
		{
			throw new NotImplementedException();
		}

		public override bool FileExists(string path)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		protected FileManagerDroid(string packageDirectoryPath, AssetManager assetManager)
			: base( packageDirectoryPath,
					"TODO",
					"/apk",
					System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
					System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),"Log"),
					"SavedGames"){
			throw new NotImplementedException();
		}

		void CopyFile(string srcRelativePath) {
			throw new NotImplementedException();
		}
	}
}