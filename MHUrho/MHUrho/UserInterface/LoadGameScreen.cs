using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class LoadGameScreen : FilePickScreen
    {
		Button loadButton;

		public LoadGameScreen(MyGame game, MenuUIManager menuUIManager)
			: base(game, menuUIManager)
		{
			UI.LoadLayoutToElement(UI.Root, game.ResourceCache, "UI/LoadLayout.xml");

			Window window = (Window)UI.Root.GetChild("LoadWindow");

			LineEdit loadLineEdit = (LineEdit)window.GetChild("LoadLineEdit", true);

			loadButton = (Button)window.GetChild("LoadButton", true);
			loadButton.Pressed += LoadButton_Pressed;

			Button deleteButton = (Button)window.GetChild("DeleteButton", true);


			ListView fileView = (ListView)window.GetChild("FileView", true);


			Button backButton = (Button)window.GetChild("BackButton", true);


			PopUpConfirmation confirmationWindow = new PopUpConfirmation((Window)window.GetChild("DialogWindow"));

			InitUIElements(window, loadLineEdit, deleteButton, backButton, fileView, confirmationWindow);

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

			//TODO: Try catch
			Stream stream = MyGame.Files.OpenDynamicFile(newRelativePath, FileMode.Open, FileAccess.Read);
			MenuUIManager.MenuController.LoadLevel(stream);
		}


	}
}
