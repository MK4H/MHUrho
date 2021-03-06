﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.StartupManagement;
using MHUrho.Storage;
using Urho;
using Urho.Gui;
using Urho.IO;

namespace MHUrho.Input.Touch
{
	class MenuController : Controller, IMenuController
	{
		public InputType InputType => InputType.Touch;
		public event OnScreenChangeDelegate ScreenChanged;

		public void InitialSwitchToMainMenu(string loadingErrorTitle = null, string loadingErrorDescription = null)
		{
			throw new NotImplementedException();
		}

		public void SwitchToPauseMenu(IGameController gameController)
		{
			throw new NotImplementedException();
		}

		public void ResumePausedLevel()
		{
			throw new NotImplementedException();
		}

		public void EndPausedLevel()
		{
			throw new NotImplementedException();
		}

		public void SwitchToEndScreen(bool victory)
		{
			throw new NotImplementedException();
		}

		public void SavePausedLevel(string fileName)
		{
			throw new NotImplementedException();
		}

		public ILevelLoader GetLevelLoaderForEditing(LevelRep level,
													IProgressEventWatcher parentProgress = null,
													double subsectionSize = 100)
		{
			throw new NotImplementedException();
		}

		public ILevelLoader GetLevelLoaderForPlaying(LevelRep level,
													PlayerSpecification players,
													LevelLogicCustomSettings customSettings,
													IProgressEventWatcher parentProgress = null,
													double subsectionSize = 100)
		{
			throw new NotImplementedException();
		}

		public void ExecuteActionOnCurrentScreen(MenuScreenAction action)
		{
			throw new NotImplementedException();
		}

		public MenuController() {
			throw new NotImplementedException();
		}

		protected override void TouchBegin(TouchBeginEventArgs e) {

		}

		protected override void TouchEnd(TouchEndEventArgs e) {

		}

		protected override void TouchMove(TouchMoveEventArgs e) {

		}
	}
}
