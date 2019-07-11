using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class ConfirmationPopUp {

		class Screen : IDisposable {

			public Task<bool> ConfirmationTask => taskSource.Task;

			readonly ConfirmationPopUp proxy;

			MHUrhoApp Game => MHUrhoApp.Instance;

			MenuUIManager MenuUIManager => proxy.menuUIManager;

			readonly Window popUpWindow;
			readonly Button confirmButton;
			readonly Button cancelButton;

			TaskCompletionSource<bool> taskSource;
			CancellationTokenSource timeoutCancel;
			MenuScreen underlyingMenuScreen;

			public Screen(ConfirmationPopUp proxy, string title, string description, TimeSpan? timeout = null, MenuScreen underlyingMenuScreen = null)
			{
				this.proxy = proxy;
				this.underlyingMenuScreen = underlyingMenuScreen;

				underlyingMenuScreen?.DisableInput();

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/PopUpConfirmationLayout.xml");
				this.popUpWindow = (Window)MenuUIManager.MenuRoot.GetChild("PopUpConfirmationWindow");
				popUpWindow.Visible = true;
				popUpWindow.BringToFront();

				using (var titleElem = (Text) popUpWindow.GetChild("Title", true)) {
					titleElem.Value = title;
				}

				using (var descriptionElem = (Text) popUpWindow.GetChild("Description", true)) {
					descriptionElem.Value = description;
				}

				confirmButton = (Button)popUpWindow.GetChild("ConfirmButton", true);
				confirmButton.Released += Button_Released;

				cancelButton = (Button)popUpWindow.GetChild("CancelButton", true);
				cancelButton.Released += Button_Released;

				taskSource = new TaskCompletionSource<bool>();

				if (timeout != null)
				{
					timeoutCancel = new CancellationTokenSource();
					Task.Delay(timeout.Value, timeoutCancel.Token).ContinueWith(TimeOutExpired, TaskContinuationOptions.NotOnCanceled);
				}
			}

			async void TimeOutExpired(Task task)
			{
				//Do not wait
				await MHUrhoApp.InvokeOnMainSafeAsync(() => Reply(false));
			}

			void Button_Released(ReleasedEventArgs args)
			{
				timeoutCancel?.Cancel();
				Reply(args.Element == confirmButton);
			}

			void Reply(bool confirmed)
			{
				underlyingMenuScreen?.ResetInput();
				taskSource.SetResult(confirmed);
				proxy.Hide();
			}

			public void Dispose()
			{
				confirmButton.Released -= Button_Released;
				cancelButton.Released -= Button_Released;

				timeoutCancel?.Dispose();
				confirmButton.Dispose();
				cancelButton.Dispose();

				popUpWindow.RemoveAllChildren();
				popUpWindow.Remove();
				popUpWindow.Dispose();

			}
		}


		readonly MenuUIManager menuUIManager;

		Screen screen;

		public ConfirmationPopUp(MenuUIManager menuUIManager)
		{
			this.menuUIManager = menuUIManager;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="title"></param>
		/// <param name="description"></param>
		/// <param name="timeout">Time after which it is considered as pressing the cancel button</param>
		/// <param name="underlyingMenuScreen">Underlying menu screen that will have input disabled for the duration of the confirmation popup</param>
		public Task<bool> RequestConfirmation(string title,
											string description,
											TimeSpan? timeout = null,
											MenuScreen underlyingMenuScreen = null)
		{
			screen = new Screen(this, title, description, timeout, underlyingMenuScreen);
			return screen.ConfirmationTask;
		}

		void Hide()
		{
			screen.Dispose();
			screen = null;
		}
		
	}
}
