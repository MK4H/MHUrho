using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;
using Urho.Gui;
using Urho.IO;

namespace MHUrho.Control
{
    class MenuTouchController : TouchController, IMenuController
    {
        public MenuTouchController(MyGame game) : base(game) {
            //TODO: TEMPORARY, probably move to UIManager or something

            var style = game.ResourceCache.GetXmlFile("UI/DefaultStyle.xml");

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

        public IGameController GetGameController(CameraController cameraController) {
            return new GameTouchController(cameraController, Game);
        }

        //TODO: TEMPORARY, probably move to UIManager or something
        private void Button_Pressed(PressedEventArgs obj) {
            Log.Write(LogLevel.Debug, "Button pressed");

            switch (obj.Element.Name) {
                case "StartButton":
                    LevelManager.CurrentLevel?.End();
                    LevelManager.LoadDefaultLevel(Game, new IntVector2(100, 100), new List<string>());
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

        protected override void TouchBegin(TouchBeginEventArgs e) {

        }

        protected override void TouchEnd(TouchEndEventArgs e) {

        }

        protected override void TouchMove(TouchMoveEventArgs e) {

        }
    }
}
