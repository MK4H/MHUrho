using System.Collections.Generic;
using Urho;
using MHUrhoStandard;
using System.IO;


namespace MHUrho.Desktop
{
    class Program
    {
        static void Main(string[] args)
        {
            //TODO: Load Package paths from config file
            StaticDataProvider.PackagePaths = new List<string>()
            {
                Directory.GetCurrentDirectory() + "/Data"
            };
           

            new MyGame(new ApplicationOptions("Data")).Run();
        }
    }
}
