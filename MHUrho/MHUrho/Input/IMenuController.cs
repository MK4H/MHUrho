using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.StartupManagement;
using MHUrho.UserInterface;
using Urho;

namespace MHUrho.Input
{
	public delegate void OnScreenChangeDelegate();

	public interface IMenuController
	{
		InputType InputType { get; }

		bool Enabled { get; }

		event OnScreenChangeDelegate ScreenChanged;

		void Enable();

		void Disable();

		void InitialSwitchToMainMenu(string loadingErrorTitle = null, string loadingErrorDescription = null);

		void SwitchToPauseMenu(IGameController gameController);

		void ResumePausedLevel();

		void EndPausedLevel();

		void SwitchToEndScreen(bool victory);

		void SavePausedLevel(string fileName);

		ILevelLoader GetLevelLoaderForEditing(LevelRep level, IProgressEventWatcher parentProgress = null, double subsectionSize = 100);

		ILevelLoader GetLevelLoaderForPlaying(LevelRep level, PlayerSpecification players, LevelLogicCustomSettings customSettings, IProgressEventWatcher parentProgress = null, double subsectionSize = 100);

		void ExecuteActionOnCurrentScreen(MenuScreenAction action);
	}
}
