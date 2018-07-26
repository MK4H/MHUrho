using System;
using System.Collections.Generic;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class PauseMenu : MenuScreen
    {
		public override bool Visible {
			get => window.Visible;
			set => window.Visible = value;
		}


		readonly Window window;



		public PauseMenu(MyGame game, MenuUIManager menuUIManager)
			: base(game, menuUIManager)
		{
			this.Game = game;

			UI.LoadLayoutToElement(UI.Root, game.ResourceCache, "UI/PauseMenuLayout.xml");

			window = (Window)UI.Root.GetChild("PauseMenu");
			window.Visible = false;

			((Button)window.GetChild("Resume")).Released += (args) => {
																MenuUIManager.Clear();
																MenuUIManager.MenuController.ResumePausedLevel();
															};

			((Button)window.GetChild("Save")).Released += (args) => { MenuUIManager.SwitchToSaveGame(); };

			((Button)window.GetChild("Load")).Released += (args) => { MenuUIManager.SwitchToLoadGame(); };

			((Button)window.GetChild("Options")).Released += (args) => { MenuUIManager.SwitchToOptions(); };

			((Button)window.GetChild("Exit")).Released += (args) => {
															MenuUIManager.MenuController.EndPausedLevel();
															MenuUIManager.Clear();
															MenuUIManager.SwitchToMainMenu();
														};
		}

		public override void Show()
		{
			Visible = true;
		}

		public override void Hide()
		{
			Visible = false;
		}
	}
}
