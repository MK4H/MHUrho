using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class SaveGameScreen : MenuScreen
	{
		class Screen : FilePickScreen {

			Button saveButton;

			public Screen(MyGame game, MenuUIManager menuUIManager)
				: base(game, menuUIManager)
			{
				UI.LoadLayoutToElement(MenuUIManager.MenuRoot, game.ResourceCache, "UI/SaveLayout.xml");

				Window window = (Window)MenuUIManager.MenuRoot.GetChild("SaveWindow");

				LineEdit saveNameEdit = (LineEdit)window.GetChild("SaveName", true);

				saveButton = (Button)window.GetChild("SaveButton", true);
				saveButton.Pressed += SaveButton_Pressed;

				Button deleteButton = (Button)window.GetChild("DeleteButton", true);


				ListView fileView = (ListView)window.GetChild("FileView", true);


				Button backButton = (Button)window.GetChild("BackButton", true);


				InitUIElements(window, saveNameEdit, deleteButton, backButton, fileView);

			}

			public override void Dispose()
			{
				
				saveButton.Pressed -= SaveButton_Pressed;
				saveButton.Dispose();

				base.Dispose();
			}

			protected override void EnableInput()
			{
				base.EnableInput();

				saveButton.Enabled = true;
			}

			protected override void DisableInput()
			{
				base.DisableInput();

				saveButton.Enabled = false;
			}

			void SaveButton_Pressed(PressedEventArgs args)
			{
				//TODO: Pop up invalid name
				if (LineEdit.Text == "") return;

				string newAbsoluteFilePath = Path.Combine(MyGame.Files.SaveGameDirAbsolutePath, LineEdit.Text);


				if (MyGame.Files.FileExists(newAbsoluteFilePath)) {
					DisableInput();
					MenuUIManager.ConfirmationPopUp.RequestConfirmation("Overriding file",
																		$"Do you really want to override the file \"{MatchSelected}\"?").ContinueWith(OverrideFile);
				}
				else {
					string newFilePath = Path.Combine(MyGame.Files.SaveGameDirPath, LineEdit.Text);
					MenuUIManager.MenuController.SavePausedLevel(newFilePath);
					MenuUIManager.SwitchBack();
				}
			}


			void OverrideFile(Task<bool> confirmed)
			{
				EnableInput();
				if (!confirmed.Result) return;

				string newFilePath = Path.Combine(MyGame.Files.SaveGameDirPath, LineEdit.Text);
				MenuUIManager.MenuController.SavePausedLevel(newFilePath);
				MenuUIManager.SwitchBack();
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

		MyGame Game => MyGame.Instance;
		readonly MenuUIManager menuUIManager;
		Screen screen;

		public SaveGameScreen(MenuUIManager menuUIManager)
		{
			this.menuUIManager = menuUIManager;
		}


		

		public override void Show()
		{
			if (screen != null) {
				return;
			}

			screen = new Screen(Game, menuUIManager);
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
