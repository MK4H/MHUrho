using System;
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

namespace MHUrho.Input
{
	class MenuTouchController : TouchController, IMenuController
	{
		public InputType InputType => InputType.Touch;
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

		public void SavePausedLevel(string fileName)
		{
			throw new NotImplementedException();
		}

		public void StartLoadingLevelForEditing(LevelRep level)
		{
			throw new NotImplementedException();
		}

		public void StartLoadingLevelForPlaying(LevelRep level, PlayerSpecification players, LevelLogicCustomSettings customSettings)
		{
			throw new NotImplementedException();
		}

		public void ExecuteActionOnCurrentScreen(MenuScreenAction action)
		{
			throw new NotImplementedException();
		}

		public MenuTouchController() {
			//TODO: TEMPORARY, probably move to UIManager or something

			var style = PackageManager.Instance.GetXmlFile("UI/DefaultStyle.xml");

			var button = UI.Root.CreateButton("StartButton");
			button.SetStyleAuto(style);
			button.Size = new IntVector2(100, 100);
			button.Position = new IntVector2(0, 0);
			button.Pressed += Button_Pressed;
			button.SetColor(Color.Green);

			button = UI.Root.CreateButton("SaveButton");
			button.SetStyleAuto(style);
			button.Size = new IntVector2(100, 100);
			button.Position = new IntVector2(0, 200);
			button.Pressed += Button_Pressed;
			button.SetColor(Color.Yellow);

			button = UI.Root.CreateButton("LoadButton");
			button.SetStyleAuto(style);
			button.Size = new IntVector2(100, 100);
			button.Position = new IntVector2(0, 400);
			button.Pressed += Button_Pressed;
			button.SetColor(Color.Blue);

			button = UI.Root.CreateButton("EndButton");
			button.SetStyleAuto(style);
			button.Size = new IntVector2(100, 100);
			button.Position = new IntVector2(0, 600);
			button.Pressed += Button_Pressed;
			button.SetColor(Color.Red);
		}

		//TODO: TEMPORARY, probably move to UIManager or something
		void Button_Pressed(PressedEventArgs obj)
		{
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
