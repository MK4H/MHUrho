using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.StartupManagement;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class AboutScreen : MenuScreen
	{
		class Screen : ScreenBase {

			const string BackButtonName = "BackButton";

			readonly AboutScreen proxy;

			readonly Window window;
			readonly Button backButton;

			public Screen(AboutScreen proxy)
				:base(proxy)
			{
				this.proxy = proxy;

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/AboutScreenLayout.xml");
				window = (Window)MenuUIManager.MenuRoot.GetChild("AboutWindow");

				backButton = (Button)window.GetChild(BackButtonName, true);
				backButton.Released += BackButtonReleased;
			}

			public override void Dispose()
			{
				backButton.Released -= BackButtonReleased;

				backButton.Dispose();

				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
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

			void BackButtonReleased(ReleasedEventArgs args)
			{
				MenuUIManager.SwitchBack();
			}
		}


		protected override ScreenBase ScreenInstance {
			get => screen;
			set => screen = (Screen)value;
		}

		Screen screen;

		public AboutScreen(MenuUIManager menuUIManager)
			: base(menuUIManager)
		{ }

		public override void ExecuteAction(MenuScreenAction action)
		{
			throw new NotImplementedException();
		}

		public override void Show()
		{
			if (ScreenInstance != null) {
				return;
			}

			screen = new Screen(this);
		}
	}
}
