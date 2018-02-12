using System.Collections.Generic;
using Urho;
using MHUrho;
using System.IO;


namespace MHUrho.Desktop
{
    class Program
    {
        static void Main(string[] args)
        {


        

            new MyGame(new ApplicationOptions("Data")).Run();
        }
    }
}
