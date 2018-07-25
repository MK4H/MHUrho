using System;
using System.Collections.Generic;
using System.Text;
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

		public PopUpConfirmation(Window popUpWindow)
		{
			this.popUpWindow = popUpWindow;
			popUpWindow.Visible = false;

			titleElem = (Text) popUpWindow.GetChild("Title", true);
			descriptionElem = (Text) popUpWindow.GetChild("Description", true);

			confirmButton = (Button)popUpWindow.GetChild("ConfirmButton", true);
			confirmButton.Released += Button_Released;
			
			cancelButton = (Button)popUpWindow.GetChild("CancelButton", true);
			cancelButton.Released += Button_Released;
		}

		public void RequestConfirmation(string title,
										string description,
										Action<bool> handler)
		{
			currentHandler = handler;
			titleElem.Value = title;
			descriptionElem.Value = description;
			popUpWindow.Visible = true;
			popUpWindow.BringToFront();
		}

		void Button_Released(ReleasedEventArgs args)
		{
			popUpWindow.Visible = false;
			currentHandler?.Invoke(args.Element == confirmButton);

			titleElem.Value = "";
			descriptionElem.Value = "";
			currentHandler = null;
		}
	}
}
