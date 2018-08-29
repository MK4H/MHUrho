using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MHUrho.Packaging;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic
{
    public interface ILevelLoader
    {
		ILevelManager Level { get; }

		ILoadingWatcher LoadingWatcher { get; }

		Task<ILevelManager> CurrentLoading { get; }
	

		Task<ILevelManager> Load(StLevel storedLevel, bool editorMode);

		Task<ILevelManager> LoadFrom(Stream stream, bool editorMode, bool leaveOpen = false);

		Task<ILevelManager> LoadDefaultLevel(IntVector2 mapSize);

	}
}
