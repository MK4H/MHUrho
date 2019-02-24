using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHUrho.Desktop {
	class FileManagerDesktop : FileManager {

		public static FileManagerDesktop LoadFileManager() {
			var fileManager = new FileManagerDesktop(
				"PackageDirectory",
				"config.xml",
				Directory.GetCurrentDirectory(),
				Path.Combine(Directory.GetCurrentDirectory(),"DynData"),
				Path.Combine(Directory.GetCurrentDirectory(), "DynData","Log"),
				"SavedGames");

			if (!Directory.Exists(fileManager.DynamicDirPath)) {
				Directory.CreateDirectory(fileManager.DynamicDirPath);
			}

			if (!Directory.Exists(fileManager.SaveGameDirAbsolutePath))
			{
				Directory.CreateDirectory(fileManager.SaveGameDirAbsolutePath);
			}

			if (!Directory.Exists(fileManager.PackageDirectoryAbsolutePath)) {
				Directory.CreateDirectory(fileManager.PackageDirectoryAbsolutePath);
			}

			File.Create(fileManager.LogPath).Dispose();
			
			return fileManager;
		}

		protected FileManagerDesktop(string packageDirectoryPath, 
									string configFilePath, 
									string staticDirPath,
									string dynamicDirPath,
									string logFilePath,
									string saveDirPath)
			: base(packageDirectoryPath, configFilePath, staticDirPath, dynamicDirPath, logFilePath, saveDirPath) {

		}

		public override Stream OpenStaticFileRO(string relativePath) {
			if (!File.Exists(Path.Combine(DynamicDirPath, relativePath))) {
				return new FileStream(Path.Combine(StaticDirPath, relativePath), FileMode.Open, FileAccess.Read);
			}
			return new FileStream(Path.Combine(DynamicDirPath, relativePath), FileMode.Open, FileAccess.Read);
		}

		public override Stream OpenStaticFileRW(string relativePath) {
			if (!File.Exists(Path.Combine(DynamicDirPath, relativePath))) {
				CopyStaticToDynamic(relativePath);
			}
			return new FileStream(Path.Combine(DynamicDirPath, relativePath), FileMode.Open, FileAccess.Read);
		}

		public override Stream OpenDynamicFile(string relativePath, FileMode fileMode, FileAccess fileAccess) {
			return new FileStream(Path.Combine(DynamicDirPath, relativePath), fileMode, fileAccess);
		}

		public override void CopyStaticToDynamic(string srcRelativePath) {

			string source = Path.Combine(StaticDirPath, srcRelativePath);
			string target = Path.Combine(DynamicDirPath, srcRelativePath);

			Copy(source, target, true);	   
		}

		public override void Copy(string source, string target, bool overrideFiles)
		{
			if (source == null) {
				throw new ArgumentNullException(nameof(source), "Source path cannot be null");
			}
			if (!Path.IsPathRooted(source)) {
				throw new ArgumentException("Source path has to be rooted", nameof(source));
			}

			if (target == null) {
				throw new ArgumentNullException(nameof(target), "Target path cannot be null"); 

			}

			if (!Path.IsPathRooted(target)) {
				throw new ArgumentException("Target path has to be rooted", nameof(target));
			}

			source = Path.GetFullPath(source);
			target = Path.GetFullPath(target);

			var attr = File.GetAttributes(source);

			if (attr.HasFlag(FileAttributes.Directory))
			{
				foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories)) {
					Directory.CreateDirectory(dirPath.Replace(source, target));
				}
					

				//Copy all the files and Replaces any files with the same name
				foreach (string subfilePath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories)) {
					File.Copy(subfilePath, subfilePath.Replace(source, target), overrideFiles);
				}
					
			}
			else
			{
				Directory.CreateDirectory(Path.GetDirectoryName(target));
				File.Copy(source, target, overrideFiles);
			}
		}

		public override bool FileExists(string path)
		{
			return File.Exists(path);
		}

		public override bool DirectoryExists(string path)
		{
			return Directory.Exists(path);
		}

		public override IEnumerable<string> GetFSEntriesInDirectory(string dirPath, bool files, bool directories)
		{
			if (files && directories) {
				return Directory.EnumerateFileSystemEntries(dirPath);
			}
			else if (files) {
				return Directory.EnumerateFiles(dirPath);
			}
			else if (directories) {
				return Directory.EnumerateDirectories(dirPath);
			}
			else {
				return Enumerable.Empty<string>();
			}
		}

		public override IEnumerable<string> GetFSEntriesInDirectory(string dirPath, bool files, bool directories, string searchPattern, SearchOption searchOption)
		{
			if (searchPattern == null) {
				throw new ArgumentNullException(nameof(searchPattern), "Search pattern cannot be null");
			}

			if (files && directories) {
				return Directory.EnumerateFileSystemEntries(dirPath, searchPattern, searchOption);
			}
			else if (files) {
				return Directory.EnumerateFiles(dirPath, searchPattern, searchOption);
			}
			else if (directories) {
				return Directory.EnumerateDirectories(dirPath, searchPattern, searchOption);
			}
			else {
				return Enumerable.Empty<string>();
			}
		}

		public override void DeleteDynamicFile(string relativePath)
		{
			File.Delete(Path.Combine(DynamicDirPath, relativePath));
		}
	}
}
