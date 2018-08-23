using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class LoadingScreen : MenuScreen
    {
		class Screen : IDisposable {

			readonly LoadingScreen proxy;
			MyGame Game => proxy.game;
			MenuUIManager MenuUIManager => proxy.menuUIManager;

			readonly Window window;

			ProgressBar progressBar;
			Text text;

			public Screen(LoadingScreen proxy)
			{
				Game.UI.LoadLayoutToElement(Game.UI.Root, Game.ResourceCache, "UI/LoadingScreenLayout.xml");

				window = (Window)Game.UI.Root.GetChild("LoadingScreen");
				progressBar = new ProgressBar(window.GetChild("ProgressBar"));
				text = (Text)window.GetChild("Text");
			}

			public LoadingWatcher GetLoadingWatcher()
			{
				return new LoadingWatcher(OnPercentageUpdate,
										OnTextUpdate,
										OnLoadingFinished);
			}

			void OnLoadingFinished(LoadingWatcher finishedLoading)
			{
				MenuUIManager.Clear();
				progressBar.SetValue(0);
				text.Value = "";
			}

			void OnPercentageUpdate(float value)
			{
				progressBar.SetValue(value);
			}

			void OnTextUpdate(string newText)
			{
				text.Value = newText;
			}

			public void Dispose()
			{
				window.RemoveAllChildren();
				window.Remove();

				window.Dispose();
				text.Dispose();
				progressBar.Dispose();
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

		public LoadingScreen(MyGame game, MenuUIManager menuUIManager)
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
