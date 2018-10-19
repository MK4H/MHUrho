﻿using System;
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
	public interface IMenuController
	{
		InputType InputType { get; }

		bool Enabled { get; }

		void Enable();

		void Disable();

		void InitialSwitchToMainMenu(string loadingErrorTitle = null, string loadingErrorDescription = null);

		void SwitchToPauseMenu(IGameController gameController);

		void ResumePausedLevel();

		void EndPausedLevel();

		void SavePausedLevel(string fileName);

		ILevelLoader StartLoadingLevelForEditing(LevelRep level, ILoadingSignaler loadingSignaler);

		ILevelLoader StartLoadingLevelForPlaying(LevelRep level, PlayerSpecification players, LevelLogicCustomSettings customSettings, ILoadingSignaler loadingSignaler);

		void ExecuteActionOnCurrentScreen(MenuScreenAction action);
	}
}
