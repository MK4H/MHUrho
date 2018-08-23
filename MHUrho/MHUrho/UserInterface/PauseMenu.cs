using System;
using System.Collections.Generic;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class PauseMenu : MenuScreen
    {
		class Screen : IDisposable {

			const string WindowName = "PauseMenu";
			const string ResumeButton = "Resume";
			const string SaveButton = "Save";
			const string LoadButton = "Load";
			const string OptionsButton = "Options";
			const string ExitButton = "Exit";

			readonly PauseMenu proxy;
			MyGame Game => proxy.game;
			MenuUIManager MenuUIManager => proxy.menuUIManager;
			UI UI => Game.UI;

			readonly Window window;

			public Screen(PauseMenu proxy)
			{
				this.proxy = proxy;
				UI.LoadLayoutToElement(UI.Root, Game.ResourceCache, "UI/PauseMenuLayout.xml");

				window = (Window)UI.Root.GetChild(WindowName);
				window.Visible = false;

				((Button) window.GetChild(ResumeButton)).Released += ButtonReleased;

				((Button)window.GetChild(SaveButton)).Released += ButtonReleased;

				((Button)window.GetChild(LoadButton)).Released += ButtonReleased;

				((Button)window.GetChild(OptionsButton)).Released += ButtonReleased;

				((Button)window.GetChild(ExitButton)).Released += ButtonReleased;
			}

			void ButtonReleased(ReleasedEventArgs args)
			{
				switch (args.Element.Name) {
					case ResumeButton:
						MenuUIManager.Clear();
						MenuUIManager.MenuController.ResumePausedLevel();
						break;
					case SaveButton:
						MenuUIManager.SwitchToSaveGame();
						break;
					case LoadButton:
						MenuUIManager.SwitchToLoadGame();
						break;
					case OptionsButton:
						MenuUIManager.SwitchToOptions();
						break;
					case ExitButton:
						MenuUIManager.MenuController.EndPausedLevel();
						MenuUIManager.Clear();
						MenuUIManager.SwitchToMainMenu();
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(args.Element.Name), args.Element.Name, "Unknown button released");
				}
			}

			public void Dispose()
			{
				((Button)window.GetChild(ResumeButton)).Released -= ButtonReleased;

				((Button)window.GetChild(SaveButton)).Released -= ButtonReleased;

				((Button)window.GetChild(LoadButton)).Released -= ButtonReleased;

				((Button)window.GetChild(OptionsButton)).Released -= ButtonReleased;

				((Button)window.GetChild(ExitButton)).Released -= ButtonReleased;

				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
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

		readonly MyGame game;
		readonly MenuUIManager menuUIManager;

		Screen screen;


		public PauseMenu(MyGame game, MenuUIManager menuUIManager)
		{
			this.game = game;
			this.menuUIManager = menuUIManager;
		}

		public override void Show()
		{
			if (screen != null) {
				return;
			}

			screen = new Screen(this);
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
