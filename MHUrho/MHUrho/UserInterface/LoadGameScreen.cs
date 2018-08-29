using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class LoadGameScreen : MenuScreen
    {
		class Screen : FilePickScreen {

			Button loadButton;

			public Screen(MyGame game, MenuUIManager menuUIManager) 
				:base(game, menuUIManager)
			{
				UI.LoadLayoutToElement(UI.Root, game.ResourceCache, "UI/LoadLayout.xml");

				Window window = (Window)UI.Root.GetChild("LoadWindow");

				LineEdit loadLineEdit = (LineEdit)window.GetChild("LoadLineEdit", true);

				loadButton = (Button)window.GetChild("LoadButton", true);
				loadButton.Pressed += LoadButton_Pressed;

				Button deleteButton = (Button)window.GetChild("DeleteButton", true);


				ListView fileView = (ListView)window.GetChild("FileView", true);


				Button backButton = (Button)window.GetChild("BackButton", true);

				InitUIElements(window, loadLineEdit, deleteButton, backButton, fileView);
			}

			public override void Dispose()
			{
				loadButton.Pressed -= LoadButton_Pressed;
				loadButton.Dispose();
				base.Dispose();
			}

			protected override void EnableInput()
			{
				base.EnableInput();

				loadButton.Enabled = true;
			}

			protected override void DisableInput()
			{
				base.DisableInput();

				loadButton.Enabled = false;
			}

			protected override void TotalMatchSelected(string newMatchSelected)
			{
				base.TotalMatchSelected(newMatchSelected);

				loadButton.SetStyle("LoadButton");
			}

			protected override void TotalMatchDeselected()
			{
				base.TotalMatchDeselected();

				loadButton.SetStyle("DisabledButton");
			}

			void LoadButton_Pressed(PressedEventArgs args)
			{
				if (MatchSelected == null) return;

				string newRelativePath = Path.Combine(MyGame.Files.SaveGameDirPath, MatchSelected);

				MenuUIManager.MenuController.StartLoadingLevel(newRelativePath, false);
			}
		}

		public override bool Visible {
			get => screen != null;
			set {
				if (value) {
					Show();
				}
				else {
					Hide();
				}
			}
		}

		readonly MyGame game;
		readonly MenuUIManager menuUIManager;

		Screen screen;

		public LoadGameScreen(MyGame game, MenuUIManager menuUIManager)
		{
			this.game = game;
			this.menuUIManager = menuUIManager;

		}

		public override void Show()
		{
			if (screen != null) {
				return;
			}

			screen = new Screen(game, menuUIManager);
		}

		public override void Hide()
		{
			if (screen == null) {
				return;
			}

			screen.Dispose();
			screen = null;
		}
	}
}
