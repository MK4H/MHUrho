using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHUrho.Desktop {
    class ConfigManagerDesktop : ConfigManager {

        public static ConfigManagerDesktop LoadConfig() {
            //TODO: Load config files
            var configManager = new ConfigManagerDesktop(
                new List<string>()
                {
                    Path.Combine("Test","ResourceDir","DirDescription.xml")
                },
                "TODO",
                "TODO",
                Directory.GetCurrentDirectory(),
                Path.Combine(Directory.GetCurrentDirectory(),"DynData"),
                Path.Combine(Directory.GetCurrentDirectory(), "DynData","Log"));

            if (!Directory.Exists(configManager.DynamicDirPath)) {
                Directory.CreateDirectory(configManager.DynamicDirPath);
            }

            File.Create(configManager.LogPath).Dispose();
            
            return configManager;
        }

        protected ConfigManagerDesktop(List<string> packagePaths, string configFilePath, string defaultConfigFilePath,
            string staticDirPath, string dynamicDirPath, string logFilePath)
            : base(packagePaths, configFilePath, defaultConfigFilePath, staticDirPath, dynamicDirPath, logFilePath) {

        }

        public override Stream OpenStaticFileRO(string relativePath) {
            return new FileStream(Path.Combine(StaticDirPath, relativePath), FileMode.Open, FileAccess.Read);
        }

        public override Stream OpenDynamicFile(string relativePath, FileMode fileMode, FileAccess fileAccess) {
            if (!File.Exists(Path.Combine(DynamicDirPath, relativePath))) {
                CopyStaticToDynamic(relativePath);
            }
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
                File.Copy(filePath, newFilePath);
            }
           
        }


    }
}
