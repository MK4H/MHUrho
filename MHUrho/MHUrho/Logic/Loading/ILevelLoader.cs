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
    public interface ILevelLoader
    {
		Task<ILevelManager> CurrentLoading { get; }
	

		Task<ILevelManager> LoadForEditing(LevelRep levelRep, StLevel storedLevel, ILoadingProgress loadingProgress);

		Task<ILevelManager> LoadForPlaying(LevelRep levelRep, StLevel storedLevel, PlayerSpecification players, LevelLogicCustomSettings customSettings, ILoadingProgress loadingProgress);

		Task<ILevelManager> LoadDefaultLevel(LevelRep levelRep, IntVector2 mapSize, ILoadingProgress loadingProgress);

	}
}
