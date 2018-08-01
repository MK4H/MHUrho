using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class SaveGameScreen : FilePickScreen
    {
		Button saveButton;

		public SaveGameScreen(MyGame game, MenuUIManager menuUIManager)
			: base(game, menuUIManager)
		{
			UI.LoadLayoutToElement(UI.Root, game.ResourceCache, "UI/SaveLayout.xml");

			Window window = (Window)UI.Root.GetChild("SaveWindow");

			LineEdit saveNameEdit = (LineEdit)window.GetChild("SaveName", true);

			saveButton = (Button) window.GetChild("SaveButton", true);
			saveButton.Pressed += SaveButton_Pressed;

			Button deleteButton = (Button) window.GetChild("DeleteButton", true);


			ListView fileView = (ListView) window.GetChild("FileView", true);


			Button backButton = (Button) window.GetChild("BackButton", true);


			InitUIElements(window, saveNameEdit, deleteButton, backButton, fileView);

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
				MenuUIManager.PopUpConfirmation.RequestConfirmation("Overriding file",
																	$"Do you really want to override the file \"{MatchSelected}\"?",
																	OverrideFile);
			}
			else {
				string newFilePath = Path.Combine(MyGame.Files.SaveGameDirPath, LineEdit.Text);
				MenuUIManager.MenuController.SavePausedLevel(newFilePath);
				MenuUIManager.SwitchBack();
			}
		}

		void OverrideFile(bool confirmed)
		{
			EnableInput();
			if (!confirmed) return;

			string newFilePath = Path.Combine(MyGame.Files.SaveGameDirPath, LineEdit.Text);
			MenuUIManager.MenuController.SavePausedLevel(newFilePath);
			MenuUIManager.SwitchBack();
		}

		
	}
}
