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
	/// <summary>
	/// Coordinates the loading of the level.
	/// </summary>
    public interface ILevelLoader : IProgressNotifier
    {
		/// <summary>
		/// The loaded level
		/// </summary>
		ILevelManager Level { get; }

		/// <summary>
		/// Starts the loading task.
		/// </summary>
		/// <returns>The task representing the loading process.</returns>
		Task<ILevelManager> StartLoading();
	}
}
