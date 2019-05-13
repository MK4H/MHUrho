using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic
{
    public interface ILevelLoader : IProgressNotifier
    {
		ILevelManager Level { get; }

		Task<ILevelManager> StartLoading();
	}
}
