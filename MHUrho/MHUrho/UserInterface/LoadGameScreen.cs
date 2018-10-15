using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.StartupManagement;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class LoadGameScreen : FilePickScreen
	{
		class Screen : FilePickScreenBase {

			readonly Button loadButton;

			public Screen(LoadGameScreen proxy) 
				:base(proxy)
			{
				LoadFileNames(MyGame.Files.SaveGameDirAbsolutePath);

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/LoadLayout.xml");

				Window window = (Window)MenuUIManager.MenuRoot.GetChild("LoadWindow");

				LineEdit loadLineEdit = (LineEdit)window.GetChild("LoadLineEdit", true);

				loadButton = (Button)window.GetChild("LoadButton", true);
				loadButton.Pressed += LoadButton_Pressed;
				loadButton.Enabled = false;

				Button deleteButton = (Button)window.GetChild("DeleteButton", true);
				deleteButton.Enabled = false;

				ListView fileView = (ListView)window.GetChild("FileView", true);

				Button backButton = (Button)window.GetChild("BackButton", true);

				InitUIElements(window, loadLineEdit, deleteButton, backButton, fileView);
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

			public override void Dispose()
			{
				loadButton.Pressed -= LoadButton_Pressed;
				loadButton.Dispose();
				base.Dispose();
			}

			protected override void TotalMatchSelected(string newMatchSelected)
			{
				base.TotalMatchSelected(newMatchSelected);

				loadButton.Enabled = true;
			}

			protected override void TotalMatchDeselected()
			{
				base.TotalMatchDeselected();

				loadButton.Enabled = false;
			}

			async void LoadButton_Pressed(PressedEventArgs args)
			{
				if (MatchSelected == null) return;

				string newRelativePath = Path.Combine(MyGame.Files.SaveGameDirPath, MatchSelected);

				LoadingScreen screen = MenuUIManager.SwitchToLoadingScreen(MenuUIManager.Clear);

				var levelRep = await LevelRep.GetFromSavedGame(newRelativePath, screen.LoadingWatcher.GetWatcherForSubsection(40));

				MenuUIManager.MenuController.StartLoadingLevelForPlaying(levelRep, PlayerSpecification.LoadFromSavedGame, LevelLogicCustomSettings.LoadFromSavedGame, screen.LoadingWatcher.GetWatcherForSubsection(60));
			}
		}

		protected override ScreenBase ScreenInstance {
			get => screen;
			set => screen = (Screen)value;
		}

		Screen screen;

		public LoadGameScreen(MenuUIManager menuUIManager)
			:base(menuUIManager)
		{


		}


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
