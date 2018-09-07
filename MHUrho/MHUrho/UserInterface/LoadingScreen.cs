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
			MyGame Game => proxy.Game;
			MenuUIManager MenuUIManager => proxy.menuUIManager;

			ILoadingWatcher LoadingWatcher => proxy.LoadingWatcher;

			readonly Window window;

			ProgressBar progressBar;
			Text text;

			public Screen(LoadingScreen proxy)
			{

				this.proxy = proxy;

				Game.UI.LoadLayoutToElement(Game.UI.Root, Game.ResourceCache, "UI/LoadingScreenLayout.xml");

				window = (Window)Game.UI.Root.GetChild("LoadingScreen");
				progressBar = new ProgressBar(window.GetChild("ProgressBar"));
				text = (Text)window.GetChild("Text");

				LoadingWatcher.OnTextUpdate += OnTextUpdate;
				LoadingWatcher.OnPercentageUpdate += OnPercentageUpdate;
				LoadingWatcher.OnFinishedLoading += OnLoadingFinished;
			}

			public void OnLoadingFinished(ILoadingWatcher finishedLoading)
			{
				MenuUIManager.Clear();
				progressBar.SetValue(0);
				text.Value = "";
			}

			public void OnPercentageUpdate(float value)
			{
				progressBar.SetValue(value);
			}

			public void OnTextUpdate(string newText)
			{
				text.Value = newText;
			}

			public void Dispose()
			{
				LoadingWatcher.OnTextUpdate -= OnTextUpdate;
				LoadingWatcher.OnPercentageUpdate -= OnPercentageUpdate;
				LoadingWatcher.OnFinishedLoading -= OnLoadingFinished;

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

		//TODO: Restrict setting only when not visible etc.
		public ILoadingWatcher LoadingWatcher { get; set; }

		MyGame Game => MyGame.Instance;
		readonly MenuUIManager menuUIManager;

		Screen screen;

		public LoadingScreen(MenuUIManager menuUIManager)
		{
			this.menuUIManager = menuUIManager;
		}

		public override void Show()
		{
			if (screen != null) {
				return;
			}

			//TODO: Check that loading watcher is set

			screen = new Screen(this);
		}

		public override void Hide()
		{
			if (screen == null) {
				return;
			}
			LoadingWatcher = null;

			screen.Dispose();
			screen = null;
		}
	}
}
