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
		ILoadingWatcher LoadingWatcher { get; }

		Task<ILevelManager> CurrentLoading { get; }
	

		Task<ILevelManager> LoadForEditing(LevelRep levelRep, StLevel storedLevel);

		Task<ILevelManager> LoadForPlaying(LevelRep levelRep, StLevel storedLevel, PlayerSpecification players);

		Task<ILevelManager> LoadDefaultLevel(LevelRep levelRep, IntVector2 mapSize);

	}
}
