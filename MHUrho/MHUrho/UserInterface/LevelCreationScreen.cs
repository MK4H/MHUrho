using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class LevelCreationScreen : MenuScreen
    {
		class Screen : IDisposable {
			LevelCreationScreen proxy;

			MyGame Game => proxy.game;
			MenuUIManager MenuUIManager => proxy.menuUIManager;

			Window window;


			public Screen(LevelCreationScreen proxy)
			{
				this.proxy = proxy;

				Game.UI.LoadLayoutToElement(Game.UI.Root, Game.ResourceCache, "UI/LevelPickingLayout.xml");

				window = (Window)Game.UI.Root.GetChild("LevelCreationWindow");

				((Button)window.GetChild("EditButton", true)).Released += EditButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released += BackButtonReleased;
			}

			void EditButtonReleased(ReleasedEventArgs args)
			{
				//Creating new level
				if (proxy.Level == null) {
					//TODO: Read size from some UI element
					MenuUIManager.MenuController.StartLoadingDefaultLevel(new Urho.IntVector2(200, 200));
				}
				else {
					MenuUIManager.MenuController.StartLoadingLevel(proxy.Level, true);
				}
			}

			public void Dispose()
			{
				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
			}
		}

		public LevelRep Level { get; set; }

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

		public LevelCreationScreen(MyGame game, MenuUIManager menuUIManager)
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
			screen.Dispose();
			screen = null;
		}
	}
}
