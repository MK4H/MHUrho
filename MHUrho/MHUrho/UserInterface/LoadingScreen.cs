using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.StartupManagement;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class LoadingScreen : MenuScreen
	{
		class Screen : ScreenBase {

			public LoadingWatcher LoadingWatcher { get; private set; }

			readonly LoadingScreen proxy;

			

			readonly Window window;

			Text text;

			public Screen(LoadingScreen proxy)
				:base(proxy)
			{

				this.proxy = proxy;
				this.LoadingWatcher = new LoadingWatcher();

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/LoadingScreenLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild("LoadingScreen");
				text = (Text)window.GetChild("Text");

				LoadingWatcher.TextUpdate += OnTextUpdate;
				LoadingWatcher.FinishedLoading += OnLoadingFinished;
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
				LoadingWatcher.TextUpdate -= OnTextUpdate;
				LoadingWatcher.FinishedLoading -= OnLoadingFinished;

				window.RemoveAllChildren();
				window.Remove();

				window.Dispose();
				text.Dispose();
			}

			void OnLoadingFinished(ILoadingWatcher finishedLoading)
			{
				//Update UI text, must be called from main thread
				MyGame.InvokeOnMainSafe(() => { text.Value = "Loading finished"; });
				
				//Copy to local variable if any of them call Hide()
				Action handlers = proxy.OnLoadingFinished;
				//Possible user methods
				try {
					handlers?.Invoke();
				}
				catch (Exception e) {
					Urho.IO.Log.Write(LogLevel.Debug,
									$"There was an unexpected exception during the invocation of {nameof(proxy.OnLoadingFinished)}: {e.Message}");
				}
			}

			void OnTextUpdate(string newText)
			{
				//Update UI text, must be called from main thread
				MyGame.InvokeOnMainSafe(() => { text.Value = newText; ; });	
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
