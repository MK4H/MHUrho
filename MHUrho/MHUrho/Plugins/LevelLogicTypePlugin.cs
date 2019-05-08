using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho.Gui;

namespace MHUrho.Plugins
{
	public class LevelLogicCustomSettings : IDisposable {
		public static LevelLogicCustomSettings LoadFromSavedGame { get; } = new LevelLogicCustomSettings();

		public virtual void Dispose()
		{ }
	}

	public abstract class LevelLogicTypePlugin : TypePlugin
	{
		public abstract int MaxNumberOfPlayers { get; }

		public abstract int MinNumberOfPlayers { get; }

		/// <summary>
		/// Is called when user switches to setting up the level for playing, choosing players, teams, etc.
		/// Provides a window on the screen to put custom UIElements in, to enable getting input from user when setting up the level.
		/// The instance of <see cref="LevelLogicCustomSettings"/> is then passed to the <see cref="CreateInstanceForNewPlaying(LevelLogicCustomSettings, ILevelManager)"/> method.
		/// </summary>
		/// <param name="customSettingsWindow">The window for placing creator specified UI elements to get input from user.</param>
		/// <returns><see cref="LevelLogicCustomSettings"/> or it's creator provided subtype, collecting the data from user events interactions with the <paramref name="customSettingsWindow"/>.
		/// Is subsequently passed to the <see cref="CreateInstanceForNewPlaying(LevelLogicCustomSettings, ILevelManager)"/> method.</returns>
		public abstract LevelLogicCustomSettings GetCustomSettings(Window customSettingsWindow);

		public abstract LevelLogicInstancePlugin CreateInstanceForNewPlaying(LevelLogicCustomSettings levelSettings, ILevelManager level);

		public abstract LevelLogicInstancePlugin CreateInstanceForEditorLoading(ILevelManager level);

		public abstract LevelLogicInstancePlugin CreateInstanceForNewLevel(ILevelManager level);

		public abstract LevelLogicInstancePlugin CreateInstanceForLoadingToPlaying(ILevelManager level);
	}
}
