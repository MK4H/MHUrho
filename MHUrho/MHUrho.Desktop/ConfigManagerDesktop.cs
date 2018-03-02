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
                Path.Combine(Directory.GetCurrentDirectory(),"Data"),
                Path.Combine(Directory.GetCurrentDirectory(),"DynData"),
                Path.Combine(Directory.GetCurrentDirectory(), "DynData","Log"));

            if (!Directory.Exists(configManager.DynamicDirPath)) {
                Directory.CreateDirectory(configManager.DynamicDirPath);
            }

            File.Create(configManager.LogPath).Dispose();
            
            return configManager;
        }

        protected ConfigManagerDesktop(List<string> packagePaths, string configFilePath, string defaultConfigFilePath,
            string staticFilePath, string dynamicDirPath, string logFilePath)
            : base(packagePaths, configFilePath, defaultConfigFilePath, staticFilePath, dynamicDirPath, logFilePath) {

        }

        public override Stream GetStaticFileRO(string relativePath) {
            return new FileStream(Path.Combine(StaticFilePath, relativePath), FileMode.Open, FileAccess.Read);
        }

        public override Stream GetDynamicFile(string relativePath) {
            if (!File.Exists(Path.Combine(DynamicDirPath, relativePath))) {
                CopyStaticToDynamic(relativePath);
            }
            return new FileStream(Path.Combine(DynamicDirPath, relativePath), FileMode.Open, FileAccess.ReadWrite);
        }

        public override void CopyStaticToDynamic(string srcRelativePath) {

            string filePath = Path.Combine(StaticFilePath, srcRelativePath);

            var attr = File.GetAttributes(filePath);

            if (attr.HasFlag(FileAttributes.Directory)) {


                foreach (string dirPath in Directory.GetDirectories(filePath, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(StaticFilePath, DynamicDirPath));

                //Copy all the files and Replaces any files with the same name
                foreach (string subfilePath in Directory.GetFiles(filePath, "*.*", SearchOption.AllDirectories))
                    File.Copy(subfilePath, subfilePath.Replace(StaticFilePath, DynamicDirPath), true);
            }
            else {
                string newFilePath = Path.Combine(DynamicDirPath, srcRelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
                File.Copy(filePath, newFilePath);
            }
           
        }


    }
}
