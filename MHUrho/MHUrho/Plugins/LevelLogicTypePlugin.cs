using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho.Gui;

namespace MHUrho.Plugins
{
	/// <summary>
	/// This class encapsulates the access of the level plugin to the CustomSettings window on the LeveLSettingsScreen.
	/// </summary>
	public class LevelLogicCustomSettings : IDisposable {
		/// <summary>
		/// This instance is given to the level when it is loaded from a game saved in play mode.
		/// </summary>
		public static LevelLogicCustomSettings LoadFromSavedGame { get; } = new LevelLogicCustomSettings();

		public virtual void Dispose()
		{ }
	}

	public abstract class LevelLogicTypePlugin : TypePlugin
	{
		/// <summary>
		/// Maximum number of players in the level controlled by this plugin.
		/// </summary>
		public abstract int MaxNumberOfPlayers { get; }


		/// <summary>
		/// Minimum number of players in the level controlled by this plugin.
		/// </summary>
		public abstract int MinNumberOfPlayers { get; }

		/// <summary>
		/// Is called when user switches to setting up the level for playing, choosing players, teams, etc.
		/// Provides a window on the screen to put custom UIElements in, to enable getting input from user when setting up the level.
		/// The instance of <see cref="LevelLogicCustomSettings"/> is then passed to the <see cref="CreateInstanceForNewPlaying(LevelLogicCustomSettings, ILevelManager)"/> method.
		/// </summary>
		/// <param name="customSettingsWindow">The window for placing creator specified UI elements to get input from user.</param>
		/// <param name="app">Instance representing the current app.</param>
		/// <returns><see cref="LevelLogicCustomSettings"/> or it's creator provided subtype, collecting the data from user events interactions with the <paramref name="customSettingsWindow"/>.
		/// Is subsequently passed to the <see cref="CreateInstanceForNewPlaying(LevelLogicCustomSettings, ILevelManager)"/> method.</returns>
		public abstract LevelLogicCustomSettings GetCustomSettings(Window customSettingsWindow, MHUrhoApp app);

		/// <summary>
		/// Creates instance plugin for the newly loaded level for playing.
		/// </summary>
		/// <param name="levelSettings">The instance returned from <see cref="GetCustomSettings(Window, MHUrhoApp)"/>.</param>
		/// <param name="level">The level that is currently being loaded.</param>
		/// <returns>New instance plugin for the given <paramref name="level"/>.</returns>
		public abstract LevelLogicInstancePlugin CreateInstanceForNewPlaying(LevelLogicCustomSettings levelSettings, ILevelManager level);

		/// <summary>
		/// Creates new instance plugin for the <paramref name="level"/> being loaded for editing.
		/// </summary>
		/// <param name="level">The level currently loading for editing the plugin will be added to.</param>
		/// <returns>New instance plugin for the given <paramref name="level"/>.</returns>
		public abstract LevelLogicInstancePlugin CreateInstanceForEditorLoading(ILevelManager level);

		/// <summary>
		/// Creates new instance plugin for the <paramref name="level"/> that was newly generated into the default state.
		/// </summary>
		/// <param name="level">The newly generated level.</param>
		/// <returns>New instance plugin for the given <paramref name="level"/>.</returns>
		public abstract LevelLogicInstancePlugin CreateInstanceForNewLevel(ILevelManager level);

		/// <summary>
		/// Creates new instance plugin for the <paramref name="level"/> being loaded for playing from saved game.
		/// </summary>
		/// <param name="level">The level currently loading for playing from a saved game the plugin will be added to.</param>
		/// <returns>New instance plugin for the given <paramref name="level"/>.</returns>
		public abstract LevelLogicInstancePlugin CreateInstanceForLoadingToPlaying(ILevelManager level);
	}
}
