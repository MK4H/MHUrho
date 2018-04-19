using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;
using Urho.Gui;
using Urho.IO;

namespace MHUrho.Input
{
	class MenuMandKController : MandKController, IMenuController
	{
		public MenuMandKController(MyGame game) : base(game) {

			var style = game.ResourceCache.GetXmlFile("UI/DefaultStyle.xml");

			//TODO: TEMPORARY, probably move to UIManager or something
			var button = UI.Root.CreateButton("StartButton");
			button.SetStyleAuto(style);
			button.Size = new IntVector2(50, 50);
			button.Position = new IntVector2(100, 100);
			button.Pressed += Button_Pressed;
			button.HoverBegin += Button_HoverBegin;
			button.HoverEnd += Button_HoverEnd;
			button.SetColor(Color.Green);
			button.FocusMode = FocusMode.ResetFocus;

			button = UI.Root.CreateButton("SaveButton");
			button.SetStyleAuto(style);
			button.Size = new IntVector2(50, 50);
			button.Position = new IntVector2(150, 100);
			button.Pressed += Button_Pressed;
			button.HoverBegin += Button_HoverBegin;
			button.HoverEnd += Button_HoverEnd;
			button.SetColor(Color.Yellow);
			button.FocusMode = FocusMode.ResetFocus;

			button = UI.Root.CreateButton("LoadButton");
			button.SetStyleAuto(style);
			button.Size = new IntVector2(50, 50);
			button.Position = new IntVector2(200, 100);
			button.Pressed += Button_Pressed;
			button.HoverBegin += Button_HoverBegin;
			button.HoverEnd += Button_HoverEnd;
			button.SetColor(Color.Blue);
			button.FocusMode = FocusMode.ResetFocus;

			button = UI.Root.CreateButton("EndButton");
			button.SetStyleAuto(style);
			button.Size = new IntVector2(50, 50);
			button.Position = new IntVector2(250, 100);
			button.Pressed += Button_Pressed;
			button.HoverBegin += Button_HoverBegin;
			button.HoverEnd += Button_HoverEnd;
			button.SetColor(Color.Red);
			button.FocusMode = FocusMode.ResetFocus;

		}

		public IGameController GetGameController(CameraController cameraController, ILevelManager levelManager, Player player) {
			return new GameMandKController(Game, levelManager, player, cameraController);
		}

		protected override void KeyUp(KeyUpEventArgs e) {

		}

		protected override void KeyDown(KeyDownEventArgs e) {

		}

		protected override void MouseButtonDown(MouseButtonDownEventArgs e) {

		}

		protected override void MouseButtonUp(MouseButtonUpEventArgs e) { 

		}

		protected override void MouseMoved(MouseMovedEventArgs e) {

		}

		protected override void MouseWheel(MouseWheelEventArgs e) {

		}

		//TODO: TEMPORARY, probably move to UIManager or something
		private void Button_HoverEnd(HoverEndEventArgs obj) {
			Log.Write(LogLevel.Debug, "Hover end");
		}

		//TODO: TEMPORARY, probably move to UIManager or something
		private void Button_HoverBegin(HoverBeginEventArgs obj) {
			Log.Write(LogLevel.Debug, "Hover begin");
		}

		//TODO: TEMPORARY, probably move to UIManager or something
		private void Button_Pressed(PressedEventArgs obj) {
			Log.Write(LogLevel.Debug, "Button pressed");

			switch (obj.Element.Name) {
				case "StartButton":
					LevelManager.CurrentLevel?.End();
					LevelManager.LoadDefaultLevel(Game, new IntVector2(100, 100), "testRP2");
					break;
				case "SaveButton":
					//TODO: Move this elsewhere
					using (var saveFile =
						new Google.Protobuf.CodedOutputStream(MyGame.Config.OpenDynamicFile("savedGame.save", System.IO.FileMode.Create,
																							System.IO.FileAccess.Write))) {
						LevelManager.CurrentLevel.Save().WriteTo(saveFile);
					}
					break;
				case "LoadButton":
					LevelManager.CurrentLevel?.End();

					using (Stream saveFile = MyGame.Config.OpenDynamicFile("savedGame.save",
																		   System.IO.FileMode.Open,
																		   FileAccess.Read)) {
						LevelManager.Load(Game, StLevel.Parser.ParseFrom(saveFile));
					}

					break;
				case "EndButton":
					LevelManager.CurrentLevel?.End();
					break;
				default:
					break;
			}
		}

	}
}
