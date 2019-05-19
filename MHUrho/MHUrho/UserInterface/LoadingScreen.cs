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

			IProgressNotifier ProgressNotifier => proxy.ProgressNotifier;

			readonly LoadingScreen proxy;

			readonly Window window;

			Text text;

			public Screen(LoadingScreen proxy)
				:base(proxy)
			{

				this.proxy = proxy;

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/LoadingScreenLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild("LoadingScreen");
				text = (Text)window.GetChild("Text");

				ProgressNotifier.TextUpdate += OnTextUpdate;
				ProgressNotifier.Finished += OnLoadingFinished;
				ProgressNotifier.Failed += OnLoadingFailed;
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
				ProgressNotifier.TextUpdate -= OnTextUpdate;
				ProgressNotifier.Finished -= OnLoadingFinished;
				ProgressNotifier.Failed -= OnLoadingFailed;

				window.RemoveAllChildren();
				window.Remove();

				window.Dispose();
				text.Dispose();
			}

			void OnLoadingFinished(IProgressNotifier finished)
			{
				//Update UI text, must be called from main thread
				MHUrhoApp.InvokeOnMainSafe(() => { text.Value = "Loading finished"; });
			}

			void OnLoadingFailed(IProgressNotifier failed, string message)
			{
				//Update UI text, must be called from main thread
				MHUrhoApp.InvokeOnMainSafe(() => { text.Value = "Loading failed"; });
			}

			void OnTextUpdate(string newText)
			{
				//Update UI text, must be called from main thread
				MHUrhoApp.InvokeOnMainSafe(() => { text.Value = newText; ; });	
			}
		}

		public IProgressNotifier ProgressNotifier { get; set; } 
	


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
			if (action is LoadingScreenAction myAction)
			{
				switch (myAction.Action)
				{
					case LoadingScreenAction.Actions.None:
						//Nothing
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(action), myAction.Action, "Unknown action type");
				}
			}
			else
			{
				throw new ArgumentException("Action does not belong to the current screen", nameof(action));
			}
		}

		public override void Show()
		{
			if (screen != null) {
				return;
			}


			if (ProgressNotifier == null) {
				throw new
					InvalidOperationException($"{nameof(ProgressNotifier)} has to be set before Showing this screen");
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
			ProgressNotifier = null;
		}
	}
}
