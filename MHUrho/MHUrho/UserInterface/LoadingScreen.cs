using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class LoadingScreen : MenuScreen
    {
		public override bool Visible {
			get => window.Visible;
			set {
				if (value) {
					Show();
				}
				else {
					Hide();
				}
			}
		}

		public ProgressBar ProgressBar { get; private set; }
		public Text Text { get; private set; }

		readonly Window window;
		



		public LoadingScreen(MyGame game, MenuUIManager menuUIManager)
			: base(game, menuUIManager)
		{
			UI.LoadLayoutToElement(UI.Root, game.ResourceCache, "UI/LoadingScreenLayout.xml");

			window = (Window) UI.Root.GetChild("LoadingScreen");
			ProgressBar = new ProgressBar(window.GetChild("ProgressBar"));
			Text = (Text)window.GetChild("Text");

		}

		public LoadingWatcher GetLoadingWatcher()
		{
			return new LoadingWatcher(OnPercentageUpdate,
									OnTextUpdate,
									OnLoadingFinished);
		}

		public override void Show()
		{
			window.Visible = true;
		}

		public override void Hide()
		{
			window.Visible = false;
		}

		void OnLoadingFinished(LoadingWatcher finishedLoading)
		{
			MenuUIManager.Clear();
			ProgressBar.SetValue(0);
			Text.Value = "";
		}

		void OnPercentageUpdate(float value)
		{
			ProgressBar.SetValue(value);
		}

		void OnTextUpdate(string text)
		{
			Text.Value = text;
		}
	}
}
