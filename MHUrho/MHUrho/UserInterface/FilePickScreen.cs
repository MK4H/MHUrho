﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    abstract class FilePickScreen : MenuScreen {
		public override bool Visible {
			get => Window.Visible;
			set {
				if (value) {
					Show();
				}
				else {
					Hide();
				}
			}
		}

		protected struct NameTextPair : IComparable<NameTextPair> {
			public readonly string Name;
			public readonly Text Text;

			public NameTextPair(string name)
			{
				Name = name;
				Text = new Text {
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

		protected Window Window;
		protected LineEdit LineEdit;
		protected Button DeleteButton;
		protected ListView FileView;
		protected Button BackButton;

		protected List<NameTextPair> FileNames;

		protected string MatchSelected;

		protected PopUpConfirmation ConfirmationWindow;

		protected FilePickScreen(MyGame game,MenuUIManager menuUIManager)
			: base(game, menuUIManager)
		{
		}

		protected void InitUIElements(Window window,
									LineEdit lineEdit,
									Button deleteButton,
									Button backButton,
									ListView fileView,
									PopUpConfirmation popUpConfirmation)
		{
			this.Window = window;
			this.LineEdit = lineEdit;
			this.DeleteButton = deleteButton;
			this.FileView = fileView;
			this.BackButton = backButton;
			this.ConfirmationWindow = popUpConfirmation;

			LineEdit.TextChanged += NameEditTextChanged;
			DeleteButton.Pressed += DeleteButton_Pressed;
			FileView.ItemSelected += OnFileSelected;
			BackButton.Pressed += BackButton_Pressed;
		}

		public override void Show()
		{
			FileNames = new List<NameTextPair>();

			foreach (var file in MyGame.Files.GetFilesInDirectory(MyGame.Files.SaveGameDirAbsolutePath)) {
				FileNames.Add(new NameTextPair(Path.GetFileName(file)));
			}

			FileNames.Sort();
			foreach (var nameText in FileNames) {
				FileView.AddItem(nameText.Text);
				nameText.Text.Visible = true;
			}

			MatchSelected = null;
			TotalMatchDeselected();

			LineEdit.Text = "";

			Window.Visible = true;

		}

		public override void Hide()
		{
			Window.Visible = false;
			if (FileNames != null) {
				foreach (var item in FileNames) {
					item.Text.Remove();
					item.Text.Dispose();
				}
				FileNames = null;
			}

			MatchSelected = null;

			FileView.RemoveAllItems();
		}

		protected virtual void EnableInput()
		{
			LineEdit.Enabled = true;
			DeleteButton.Enabled = true;
			FileView.Enabled = true;
			BackButton.Enabled = true;
		}

		protected virtual void DisableInput()
		{
			LineEdit.Enabled = false;
			DeleteButton.Enabled = false;
			FileView.Enabled = false;
			BackButton.Enabled = false;
		}

		protected virtual void TotalMatchSelected(string newMatchSelected)
		{
			DeleteButton.SetStyle("DeleteButton");
			MatchSelected = newMatchSelected;
		}

		protected virtual void TotalMatchDeselected()
		{
			DeleteButton.SetStyle("DisabledButton");
			MatchSelected = null;
		}

		void DeleteButton_Pressed(PressedEventArgs args)
		{
			if (MatchSelected == null) return;

			DisableInput();
			ConfirmationWindow.RequestConfirmation("Deleting file",
													$"Do you really want to delete the file \"{MatchSelected}\"?",
													DeleteFile);


		}

		void DeleteFile(bool confirmed)
		{
			EnableInput();
			if (!confirmed) return;

			MyGame.Files.DeleteDynamicFile(Path.Combine(MyGame.Files.SaveGameDirPath, MatchSelected));
			int index = FileNames.FindIndex((pair) => pair.Name.Equals(MatchSelected, StringComparison.CurrentCultureIgnoreCase));

			FileView.RemoveItem(FileNames[index].Text);
			FileNames[index].Text.Dispose();
			FileNames.RemoveAt(index);

			MatchSelected = null;
			TotalMatchDeselected();
			LineEdit.Text = "";
		}

		void BackButton_Pressed(PressedEventArgs args)
		{
			MenuUIManager.SwitchBack();
		}

		void NameEditTextChanged(TextChangedEventArgs args)
		{
			string newText = args.Text;
			string newMatchSelected = null;

			foreach (var nameText in FileNames) {
				bool visible = nameText.Name.StartsWith(newText, StringComparison.CurrentCultureIgnoreCase);
				nameText.Text.Visible = visible;

				if (visible && newText.Equals(nameText.Name, StringComparison.CurrentCultureIgnoreCase)) {
					newMatchSelected = newText;
				}

			}

			if (MatchSelected == null && newMatchSelected != null) {
				TotalMatchSelected(newMatchSelected);
			}
			else if (MatchSelected != null && newMatchSelected == null) {
				TotalMatchDeselected();
			}
		}

		void OnFileSelected(ItemSelectedEventArgs args)
		{
			LineEdit.Text = ((Text)FileView.SelectedItem).Value;
			TotalMatchSelected(LineEdit.Text);
		}


		
	}
}