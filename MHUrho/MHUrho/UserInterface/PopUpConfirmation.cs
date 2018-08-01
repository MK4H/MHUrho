using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class PopUpConfirmation {
		public bool Waiting => popUpWindow.Visible;

		Window popUpWindow;
		Button confirmButton;
		Button cancelButton;

		Text titleElem;
		Text descriptionElem;

		Action<bool> currentHandler;
		CancellationTokenSource timeoutCancel;

		public PopUpConfirmation(MyGame game, MenuUIManager uiManager)
		{
			game.UI.LoadLayoutToElement(game.UI.Root, game.ResourceCache, "UI/PopUpConfirmationLayout.xml");
			this.popUpWindow = (Window) game.UI.Root.GetChild("PopUpConfirmationWindow");
			popUpWindow.Visible = false;

			titleElem = (Text) popUpWindow.GetChild("Title", true);
			descriptionElem = (Text) popUpWindow.GetChild("Description", true);

			confirmButton = (Button)popUpWindow.GetChild("ConfirmButton", true);
			confirmButton.Released += Button_Released;
			
			cancelButton = (Button)popUpWindow.GetChild("CancelButton", true);
			cancelButton.Released += Button_Released;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="title"></param>
		/// <param name="description"></param>
		/// <param name="handler"></param>
		/// <param name="timeout">Time after which it is considered as pressing the cancel button</param>
		public void RequestConfirmation(string title,
										string description,
										Action<bool> handler,
										TimeSpan? timeout = null)
		{
			currentHandler = handler;
			titleElem.Value = title;
			descriptionElem.Value = description;
			popUpWindow.Visible = true;
			popUpWindow.BringToFront();
			if (timeout != null) {
				timeoutCancel = new CancellationTokenSource();
				Task.Delay(timeout.Value, timeoutCancel.Token).ContinueWith(TimeOutExpired);
			}
			
		}

		void TimeOutExpired(Task task)
		{
			//Do not wait
			MyGame.InvokeOnMainSafeAsync(() => Reply(false));
		}

		void Button_Released(ReleasedEventArgs args)
		{
			timeoutCancel?.Cancel();
			Reply(args.Element == confirmButton);
		}

		void Reply(bool confirmed)
		{
			popUpWindow.Visible = false;
			currentHandler?.Invoke(confirmed);

			titleElem.Value = "";
			descriptionElem.Value = "";
			currentHandler = null;

			timeoutCancel?.Dispose();
			timeoutCancel = null;
		}
	}
}
