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

		public override void Copy(string from, string to, bool overrideFiles)
		{
			if (from == null) {
				throw new ArgumentNullException(nameof(from), "From path cannot be null");
			}
			if (!Path.IsPathRooted(from)) {
				throw new ArgumentException("From path has to be rooted", nameof(from));
			}

			if (to == null) {
				throw new ArgumentNullException(nameof(to), "To path cannot be null"); 

			}

			if (!Path.IsPathRooted(to)) {
				throw new ArgumentException("To path has to be rooted", nameof(to));
			}

			from = Path.GetFullPath(from);
			to = Path.GetFullPath(to);

			var attr = File.GetAttributes(from);

			if (attr.HasFlag(FileAttributes.Directory))
			{
				foreach (string dirPath in Directory.GetDirectories(from, "*", SearchOption.AllDirectories)) {
					Directory.CreateDirectory(dirPath.Replace(from, to));
				}
					

				//Copy all the files and Replaces any files with the same name
				foreach (string subfilePath in Directory.GetFiles(from, "*.*", SearchOption.AllDirectories)) {
					File.Copy(subfilePath, subfilePath.Replace(from, to), overrideFiles);
				}
					
			}
			else
			{
				Directory.CreateDirectory(Path.GetDirectoryName(to));
				File.Copy(from, to, overrideFiles);
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
