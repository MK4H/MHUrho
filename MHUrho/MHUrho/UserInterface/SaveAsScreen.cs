using System;
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

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/SaveLayout.xml");
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
			}

			public override void Dispose()
			{
				nameEdit.TextChanged += NameChanged;
				descriptionEdit.TextChanged += DescriptionChanged;
				thumbnailPathButton.Released += ThumbnailPathButtonReleased;

				((Button)window.GetChild("SaveAsButton", true)).Released -= SaveAsButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released -= BackButtonReleased;

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
										Request(PackageManager.Instance.ActivePackage.RootedXmlDirectoryPath,
												SelectOption.File,
												thumbnailPathText.HasDefaultValue ? null : thumbnailPathText.Value);


					string newPath = result == null ? oldPath : result.RelativePath;
					MyGame.InvokeOnMainSafeAsync(() => thumbnailPathText.Value = newPath);
				}
				catch (OperationCanceledException e)
				{
					//Text should not have changed
				}
			}

			void SaveAsButtonReleased(ReleasedEventArgs args)
			{
				Level.LevelRep.SaveToGamePackAs(name, description, ThumbnailPath);	
			}

			void BackButtonReleased(ReleasedEventArgs args)
			{
				MenuUIManager.SwitchBack();
			}
		}

		//TODO: Check the setting
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

			screen = new Screen(this);
		}

		public override void Hide()
		{
			if (screen == null) {
				return;
			}

			Level = null;
			base.Hide();
		}

	}
}
