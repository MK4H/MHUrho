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
				new List<string>()
				{
					Path.Combine("Data","Test","ResourceDir","DirDescription.xml")
				},
				"config.xml",
				Directory.GetCurrentDirectory(),
				Path.Combine(Directory.GetCurrentDirectory(),"DynData"),
				Path.Combine(Directory.GetCurrentDirectory(), "DynData","Log"));

			if (!Directory.Exists(fileManager.DynamicDirPath)) {
				Directory.CreateDirectory(fileManager.DynamicDirPath);
			}

			File.Create(fileManager.LogPath).Dispose();
			
			return fileManager;
		}

		protected FileManagerDesktop(List<string> packagePaths, 
									string configFilePath, 
									string staticDirPath,
									string dynamicDirPath,
									string logFilePath)
			: base(packagePaths, configFilePath, staticDirPath, dynamicDirPath, logFilePath) {

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

			string filePath = Path.Combine(StaticDirPath, srcRelativePath);

			var attr = File.GetAttributes(filePath);

			if (attr.HasFlag(FileAttributes.Directory)) {


				foreach (string dirPath in Directory.GetDirectories(filePath, "*", SearchOption.AllDirectories))
					Directory.CreateDirectory(dirPath.Replace(StaticDirPath, DynamicDirPath));

				//Copy all the files and Replaces any files with the same name
				foreach (string subfilePath in Directory.GetFiles(filePath, "*.*", SearchOption.AllDirectories))
					File.Copy(subfilePath, subfilePath.Replace(StaticDirPath, DynamicDirPath), true);
			}
			else {
				string newFilePath = Path.Combine(DynamicDirPath, srcRelativePath);
				Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
				File.Copy(filePath, newFilePath, true);
			}
		   
		}

		public override bool FileExists(string path)
		{
			return File.Exists(path);
		}

	}
}
