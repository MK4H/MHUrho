using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class SaveGameScreen : MenuScreen
    {
		struct NameTextPair : IComparable<NameTextPair>{
			public readonly string Name;
			public readonly Text Text;

			public NameTextPair(string name)
			{
				Name = name;
				Text = new Text
						{
							Value = name
						};
				//Text.SetStyle("FileViewText");
				Text.SetStyleAuto();
			}

			public int CompareTo(NameTextPair other)
			{
				return string.Compare(Name, other.Name, StringComparison.CurrentCultureIgnoreCase);
			}
		}

		public override bool Visible {
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		Window window;
		LineEdit saveNameEdit;
		Button saveButton;
		Button deleteButton;
		ListView fileView;
		Button backButton;

		PopUpConfirmation confirmationWindow;

		List<NameTextPair> fileNames;

		string possiblyDelete;

		public SaveGameScreen(MyGame game, MenuUIManager menuUIManager)
			: base(game, menuUIManager)
		{
			UI.LoadLayoutToElement(UI.Root, game.ResourceCache, "UI/SaveLayout.xml");

			window = (Window)UI.Root.GetChild("SaveWindow");

			saveNameEdit = (LineEdit)window.GetChild("SaveName", true);
			saveNameEdit.TextChanged += NameEditTextChanged;

			saveButton = (Button) window.GetChild("SaveButton", true);
			saveButton.Pressed += SaveButton_Pressed;

			deleteButton = (Button) window.GetChild("DeleteButton", true);
			deleteButton.Pressed += DeleteButton_Pressed;

			fileView = (ListView) window.GetChild("FileView", true);
			fileView.ItemSelected += OnFileSelected;

			backButton = (Button) window.GetChild("BackButton", true);
			backButton.Pressed += BackButton_Pressed;

			confirmationWindow = new PopUpConfirmation((Window)window.GetChild("DialogWindow"));

		}



		public override void Show()
		{
			fileNames = new List<NameTextPair>();

			foreach (var file in MyGame.Files.GetFilesInDirectory(MyGame.Files.SaveGameDirAbsolutePath)) {
				fileNames.Add(new NameTextPair(Path.GetFileName(file)));
			}

			fileNames.Sort();
			foreach (var nameText in fileNames) {
				fileView.AddItem(nameText.Text);
				nameText.Text.Visible = true;
			}

			possiblyDelete = null;
			deleteButton.SetStyle("DisabledButton");

			saveNameEdit.Text = "";

			window.Visible = true;
			
		}

		public override void Hide()
		{
			window.Visible = false;
			if (fileNames != null) {
				foreach (var item in fileNames) {
					item.Text.Remove();
					item.Text.Dispose();
				}
				fileNames = null;
			}

			possiblyDelete = null;

			fileView.RemoveAllItems();
		}


		void DeleteButton_Pressed(PressedEventArgs args)
		{
			if (possiblyDelete == null) return;

			DisableAllInput();
			confirmationWindow.RequestConfirmation("Deleting file",
													$"Do you really want to delete the file \"{possiblyDelete}\"?",
													DeleteFile);


		}

		void BackButton_Pressed(PressedEventArgs args)
		{
			MenuUIManager.SwitchBack();
		}

		void SaveButton_Pressed(PressedEventArgs args)
		{
			//TODO: Pop up invalid name
			if (saveNameEdit.Text == "") return;

			string newAbsoluteFilePath = Path.Combine(MyGame.Files.SaveGameDirAbsolutePath, saveNameEdit.Text);


			if (MyGame.Files.FileExists(newAbsoluteFilePath)) {
				DisableAllInput();
				confirmationWindow.RequestConfirmation("Overriding file",
														$"Do you really want to override the file \"{possiblyDelete}\"?",
														OverrideFile);
			}
			else {
				string newFilePath = Path.Combine(MyGame.Files.SaveGameDirPath, saveNameEdit.Text);
				Stream file = MyGame.Files.OpenDynamicFile(newFilePath, FileMode.CreateNew, FileAccess.Write);
				MenuUIManager.MenuController.SavePausedLevel(file);
				MenuUIManager.SwitchBack();
			}
		}

		void NameEditTextChanged(TextChangedEventArgs args)
		{
			string newText = args.Text;
			string newPossiblyDelete = null;

			foreach (var nameText in fileNames) {
				bool visible = nameText.Name.StartsWith(newText, StringComparison.CurrentCultureIgnoreCase);
				nameText.Text.Visible = visible;

				if (visible && newText.Equals(nameText.Name, StringComparison.CurrentCultureIgnoreCase)) {
					newPossiblyDelete = newText;
				}
				
			}

			if (possiblyDelete == null && newPossiblyDelete != null) {
				deleteButton.SetStyle("DeleteButton");
			}
			else if (possiblyDelete != null && newPossiblyDelete == null) {
				deleteButton.SetStyle("DisabledButton");
			}

			possiblyDelete = newPossiblyDelete;
		}

		void OnFileSelected(ItemSelectedEventArgs args)
		{
			saveNameEdit.Text = possiblyDelete = ((Text)fileView.SelectedItem).Value;
			deleteButton.SetStyle("DeleteButton");
		}

		void DeleteFile(bool confirmed)
		{
			EnableAllInput();
			if (!confirmed) return;

			MyGame.Files.DeleteDynamicFile(Path.Combine(MyGame.Files.SaveGameDirPath, possiblyDelete));
			int index = fileNames.FindIndex((pair) => pair.Name.Equals(possiblyDelete, StringComparison.CurrentCultureIgnoreCase));

			fileView.RemoveItem(fileNames[index].Text);
			fileNames[index].Text.Dispose();
			fileNames.RemoveAt(index);

			possiblyDelete = null;
			deleteButton.SetStyle("DisabledButton");
			saveNameEdit.Text = "";
		}

		void OverrideFile(bool confirmed)
		{
			EnableAllInput();
			if (!confirmed) return;

			string newFilePath = Path.Combine(MyGame.Files.SaveGameDirPath, saveNameEdit.Text);
			Stream file = MyGame.Files.OpenDynamicFile(newFilePath, FileMode.Create, FileAccess.Write);
			MenuUIManager.MenuController.SavePausedLevel(file);
			MenuUIManager.SwitchBack();
		}

		void DisableAllInput()
		{
			saveNameEdit.Enabled = false;
			saveButton.Enabled = false;
			deleteButton.Enabled = false;
			fileView.Enabled = false;
			backButton.Enabled = false;
		}

		void EnableAllInput()
		{
			saveNameEdit.Enabled = true;
			saveButton.Enabled = true;
			deleteButton.Enabled = true;
			fileView.Enabled = true;
			backButton.Enabled = true;
		}
	}
}
