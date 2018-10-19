﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.StartupManagement;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class LoadingScreen : MenuScreen
	{
		class Screen : ScreenBase {

			public LoadingWatcher LoadingWatcher { get; private set; }

			readonly LoadingScreen proxy;

			

			readonly Window window;

			ProgressBar progressBar;
			Text text;

			public Screen(LoadingScreen proxy)
				:base(proxy)
			{

				this.proxy = proxy;
				this.LoadingWatcher = new LoadingWatcher();

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/LoadingScreenLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild("LoadingScreen");
				progressBar = new ProgressBar(window.GetChild("ProgressBar"));
				text = (Text)window.GetChild("Text");

				LoadingWatcher.OnTextUpdate += OnTextUpdate;
				LoadingWatcher.OnPercentageUpdate += OnPercentageUpdate;
				LoadingWatcher.OnFinishedLoading += OnLoadingFinished;
			}

			public void OnLoadingFinished(ILoadingWatcher finishedLoading)
			{
				progressBar.SetValue(100);
				text.Value = "Loading finished";
				Action handlers = proxy.OnLoadingFinished;
				handlers?.Invoke();
			}

			public void OnPercentageUpdate(float value)
			{
				progressBar.SetValue(value);
			}

			public void OnTextUpdate(string newText)
			{
				text.Value = newText;
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

		public LoadingWatcher LoadingWatcher => screen.LoadingWatcher;

		public event Action OnLoadingFinished;

		protected override ScreenBase ScreenInstance {
			get => screen;
			set => screen = (Screen)value;
		}


		Screen screen;

		public LoadingScreen(MenuUIManager menuUIManager)
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

			//TODO: Check that loading watcher is set

			screen = new Screen(this);
		}

		public override void Hide()
		{
			if (screen == null) {
				return;
			}
			

			screen.Dispose();
			screen = null;

			OnLoadingFinished = null;
		}
	}
}
