﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MHUrho.StartupManagement;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class SaveGameScreen : FilePickScreen
	{
		class Screen : FilePickScreenBase {

			readonly Button saveButton;

			public Screen(SaveGameScreen proxy)
				: base(proxy)
			{
				LoadFileNames(MyGame.Files.SaveGameDirAbsolutePath);

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/SaveLayout.xml");

				Window window = (Window)MenuUIManager.MenuRoot.GetChild("SaveWindow");

				LineEdit saveNameEdit = (LineEdit)window.GetChild("SaveName", true);

				saveButton = (Button)window.GetChild("SaveButton", true);
				saveButton.Pressed += SaveButton_Pressed;
				saveButton.Enabled = false;

				Button deleteButton = (Button)window.GetChild("DeleteButton", true);
				deleteButton.Enabled = false;

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

			public override void EnableInput()
			{
				Window.SetDeepEnabled(true);
			}

			public override void DisableInput()
			{
				Window.SetDeepEnabled(false);
			}

			public override void ResetInput()
			{
				Window.ResetDeepEnabled();
			}

			protected override void NameEditTextChanged(TextChangedEventArgs args)
			{
				base.NameEditTextChanged(args);

				saveButton.Enabled = (Filename != "");
			}

			void SaveButton_Pressed(PressedEventArgs args)
			{
				//TODO: Pop up invalid name
				if (LineEdit.Text == "") return;

				string newAbsoluteFilePath = Path.Combine(MyGame.Files.SaveGameDirAbsolutePath, LineEdit.Text);


				if (MyGame.Files.FileExists(newAbsoluteFilePath)) {
					DisableInput();
					MenuUIManager.ConfirmationPopUp
								.RequestConfirmation("Overriding file",
													$"Do you really want to override the file \"{MatchSelected}\"?")
								.ContinueWith(OverrideFile, TaskScheduler.FromCurrentSynchronizationContext());
				}
				else {
					MenuUIManager.MenuController.SavePausedLevel(LineEdit.Text);
					MenuUIManager.SwitchBack();
				}
			}




			void OverrideFile(Task<bool> confirmed)
			{
				EnableInput();
				if (!confirmed.Result) return;

				MenuUIManager.MenuController.SavePausedLevel(LineEdit.Text);
				MenuUIManager.SwitchBack();
			}
		}

		protected override ScreenBase ScreenInstance {
			get => screen;
			set => screen = (Screen) value;
		}

		Screen screen;

		public SaveGameScreen(MenuUIManager menuUIManager)
			:base(menuUIManager)
		{ }


		public override void ExecuteAction(MenuScreenAction action)
		{
			throw new NotImplementedException();
		}

		public override void Show()
		{
			if (screen != null) {
				return;
			}

			screen = new Screen(this);
		}
	}
}
