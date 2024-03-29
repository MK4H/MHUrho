﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.StartupManagement;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class SaveAsScreen : MenuScreen {

		class Screen : ScreenBase {

			readonly SaveAsScreen proxy;

			readonly Window window;
			readonly LineEdit nameEdit;
			readonly Button thumbnailPathButton;
			readonly PathText thumbnailPathText;
			readonly LineEdit descriptionEdit;

			ILevelManager Level => proxy.Level;

			string name;
			string description;

			string ThumbnailPath {
				get => thumbnailPathText.Value;
				set => thumbnailPathText.Value = value;
			}
			


			public Screen(SaveAsScreen proxy)
				:base(proxy)
			{
				this.proxy = proxy;

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/SaveAsLayout.xml");
				window = (Window) MenuUIManager.MenuRoot.GetChild("SaveAsWindow");
				nameEdit = (LineEdit) window.GetChild("NameEdit", true);
				thumbnailPathButton = (Button)window.GetChild("ThumbnailPathButton", true);
				thumbnailPathText = new PathText((Text)thumbnailPathButton.GetChild("PathText"));
				descriptionEdit = (LineEdit)window.GetChild("DescriptionEdit", true);

				nameEdit.TextChanged += NameChanged;
				descriptionEdit.TextChanged += DescriptionChanged;
				thumbnailPathButton.Released += ThumbnailPathButtonReleased;

				((Button)window.GetChild("SaveAsButton", true)).Released += SaveAsButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released += BackButtonReleased;

				nameEdit.Text = Level.LevelRep.Name;
				thumbnailPathText.Value = Level.LevelRep.ThumbnailPath;
				descriptionEdit.Text = Level.LevelRep.Description;
			}

			public override void EnableInput()
			{
				window.SetDeepEnabled(true);
			}

			public override void DisableInput()
			{
				window.SetDeepEnabled(false);
			}

			public override void ResetInput()
			{
				window.ResetDeepEnabled();
			}

			public override void Dispose()
			{
				nameEdit.TextChanged -= NameChanged;
				descriptionEdit.TextChanged -= DescriptionChanged;
				thumbnailPathButton.Released -= ThumbnailPathButtonReleased;

				((Button)window.GetChild("SaveAsButton", true)).Released -= SaveAsButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released -= BackButtonReleased;

				window.RemoveAllChildren();
				window.Remove();

				window.Dispose();
				nameEdit.Dispose();
				thumbnailPathButton.Dispose();
				thumbnailPathText.Dispose();
				descriptionEdit.Dispose();
			}

			void NameChanged(TextChangedEventArgs args)
			{
				if (!LevelRep.IsNameValid(args.Text))
				{
					nameEdit.Text = name;
				}
				name = nameEdit.Text;
			}

			void DescriptionChanged(TextChangedEventArgs args)
			{
				if (!LevelRep.IsDescriptionValid(args.Text))
				{
					descriptionEdit.Text = description;
				}
				description = descriptionEdit.Text;
			}

			async void ThumbnailPathButtonReleased(ReleasedEventArgs args)
			{
				try
				{
					string oldPath = thumbnailPathText.Value;

					var result = await MenuUIManager.
										FileBrowsingPopUp.
										Request(Game.PackageManager.ActivePackage.RootedDirectoryPath,
												SelectOption.File,
												thumbnailPathText.HasDefaultValue ? null : thumbnailPathText.Value);


					string newPath = result == null ? oldPath : result.RelativePath;
					await MHUrhoApp.InvokeOnMainSafeAsync(() => thumbnailPathText.Value = newPath);
				}
				catch (OperationCanceledException)
				{
					//Text should not have changed
				}
			}

			async void SaveAsButtonReleased(ReleasedEventArgs args)
			{
				if (Level.LevelRep.GamePack.TryGetLevel(name, out LevelRep oldLevel)) {
					bool confirm = await MenuUIManager.ConfirmationPopUp.RequestConfirmation("Override level",
																							"Do you want to override existing level with the same name?",
																							 null,
																							 proxy);
					if (!confirm) {
						return;
					}
				}

				LevelRep newLevelRep = null;
				try {
					newLevelRep = Level.LevelRep.CreateClone(name, description, ThumbnailPath);
					newLevelRep.SaveToGamePack(true);
					MenuUIManager.SwitchBack();
				}
				catch (Exception) {
					newLevelRep?.Dispose();
					await MenuUIManager.ErrorPopUp.DisplayError("Package Error",
																"There was an error while saving the level to package, see log for details.",
																proxy);
				}
			}

			void BackButtonReleased(ReleasedEventArgs args)
			{
				MenuUIManager.SwitchBack();
			}
		}

		public ILevelManager Level { get; set; }

		protected override ScreenBase ScreenInstance {
			get => screen;
			set => screen = (Screen)value;
		}

		Screen screen;

		public SaveAsScreen(MenuUIManager menuUIManager)
			:base(menuUIManager)
		{

		}

		public override void ExecuteAction(MenuScreenAction action)
		{
			throw new NotImplementedException();
		}

		public override void Show()
		{
			if (screen != null)
			{
				return;
			}

			if (Level == null)
			{
				throw new InvalidOperationException("Level has to be set before saving trying to save it.");
			}

			screen = new Screen(this);
		}

	}
}
