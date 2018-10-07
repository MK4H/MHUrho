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

		public abstract LevelLogicCustomSettings GetCustomSettings(Window customSettingsWindow);

		public abstract LevelLogicInstancePlugin CreateInstanceForNewPlaying(LevelLogicCustomSettings levelSettings, ILevelManager level);

		public abstract LevelLogicInstancePlugin CreateInstanceForEditorLoading(ILevelManager level);

		public abstract LevelLogicInstancePlugin CreateInstanceForNewLevel(ILevelManager level);

		public abstract LevelLogicInstancePlugin CreateInstanceForLoadingToPlaying(ILevelManager level);
	}
}
