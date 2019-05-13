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

			readonly LoadGameScreen proxy;

			public Screen(LoadGameScreen proxy) 
				:base(proxy)
			{
				this.proxy = proxy;

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

			void LoadButton_Pressed(PressedEventArgs args)
			{
			
				if (MatchSelected == null) return;

				string newRelativePath = Path.Combine(MyGame.Files.SaveGameDirPath, MatchSelected);

				//Has to be last statement in the method, this instance will be released during execution.
				proxy.Load(newRelativePath);
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

		/// <summary>
		/// Starts loading of the saved level from <paramref name="newRelativePath"/>.
		/// Cannot be implemented in screen, because we switch to loading screen, which releases our <see cref="screen"/>.
		/// </summary>
		/// <param name="newRelativePath">Relative path to the save game</param>
		async void Load(string newRelativePath)
		{
			//100% in total
			const double repLoadingPartSize = 10;
			const double managerLoadingPartSize = 90;

			ProgressWatcher progress = new ProgressWatcher();
			try
			{
				MenuUIManager.SwitchToLoadingScreen(progress);
				var levelRep =
					await LevelRep.GetFromSavedGame(newRelativePath,
													new ProgressWatcher(progress, repLoadingPartSize));


				ILevelLoader loader = MenuUIManager.MenuController
													.GetLevelLoaderForPlaying(levelRep,
																			PlayerSpecification.LoadFromSavedGame,
																			LevelLogicCustomSettings.LoadFromSavedGame,
																			new ProgressWatcher(progress, managerLoadingPartSize));
				loader.Finished += (finishedProgress) => { MenuUIManager.Clear(); };
				loader.Failed += (failedProgress, message) => {
									//Switch back from the loading screen
									MenuUIManager.SwitchBack();
									MenuUIManager.ErrorPopUp.DisplayError("Error", message, this);
								};
				await loader.StartLoading();
			}
			catch (LevelLoadingException e)
			{
				MenuUIManager.SwitchBack();
				await MenuUIManager.ErrorPopUp.DisplayError("Error", e.Message, this);
			}
		}
	}
}
