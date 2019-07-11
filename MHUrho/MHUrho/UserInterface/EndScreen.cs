using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.StartupManagement;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class EndScreen : MenuScreen
	{
		class Screen : ScreenBase {

			readonly EndScreen proxy;

			readonly Window window;
			readonly Text heading;
			readonly Button mainMenuButton;

			public Screen(EndScreen proxy)
				: base(proxy)
			{
				this.proxy = proxy;
				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/EndScreenLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild("EndWindow");

				heading = (Text) window.GetChild("Heading");
				heading.Value = proxy.Victory ? "Victory" : "Defeat";

				mainMenuButton = (Button) window.GetChild("MainMenuButton");
				mainMenuButton.Pressed += MainMenuButtonPressed;
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
				mainMenuButton.Pressed -= MainMenuButtonPressed;

				heading.Dispose();
				mainMenuButton.Dispose();
				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
			}

			void MainMenuButtonPressed(PressedEventArgs obj)
			{
				MenuUIManager.Clear();
				MenuUIManager.SwitchToMainMenu();
			}
		}

		public bool Victory { get; set; }

		protected override ScreenBase ScreenInstance {
			get => screen;
			set => screen = (Screen)value;
		}


		Screen screen;

		public EndScreen(MenuUIManager menuUIManager)
			: base(menuUIManager)
		{ }

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
	}
}
