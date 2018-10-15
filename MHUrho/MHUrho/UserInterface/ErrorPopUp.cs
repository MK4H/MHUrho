using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class ErrorPopUp
	{
		class Screen : IDisposable {

			public Task CloseTask => taskSource.Task;

			readonly ErrorPopUp proxy;

			MyGame Game => MyGame.Instance;

			MenuUIManager MenuUIManager => proxy.menuUIManager;

			readonly Window window;
			readonly Button closeButton;

			readonly MenuScreen underlyingMenuScreen;

			/// <summary>
			/// Task source with dummy type of bool, publicly the tasks are presented as plain Task without a return type
			/// </summary>
			TaskCompletionSource<bool> taskSource;


			public Screen(ErrorPopUp proxy, string title, string description, MenuScreen underlyingMenuScreen)
			{

				this.proxy = proxy;
				this.underlyingMenuScreen = underlyingMenuScreen;

				underlyingMenuScreen?.DisableInput();

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/PopUpErrorLayout.xml");

				this.window = (Window)MenuUIManager.MenuRoot.GetChild("PopUpErrorWindow");
				window.Visible = true;
				window.BringToFront();

				using (var titleElem = (Text)window.GetChild("Title", true))
				{
					titleElem.Value = title;
				}

				using (var descriptionElem = (Text)window.GetChild("Description", true))
				{
					descriptionElem.Value = description;
				}

				closeButton = (Button)window.GetChild("CloseButton", true);
				closeButton.Released += CloseButtonReleased;

				taskSource = new TaskCompletionSource<bool>();
			}

			void CloseButtonReleased(ReleasedEventArgs obj)
			{
				//Dummy boolean value, publicly the task present as plain Task without return value
				taskSource.SetResult(false);
				underlyingMenuScreen?.ResetInput();
				proxy.Hide();
			}

			public void Dispose()
			{
				closeButton.Released -= CloseButtonReleased;

				closeButton.Dispose();

				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
			}

			
		}

		readonly MenuUIManager menuUIManager;

		Screen screen;

		public ErrorPopUp(MenuUIManager menuUIManager)
		{
			this.menuUIManager = menuUIManager;
		}

		public Task DisplayError(string title,
								string description,
								MenuScreen underlyingMenuScreen = null)
		{
			screen = new Screen(this, title, description, underlyingMenuScreen);
			return screen.CloseTask;
		}

		void Hide()
		{
			screen.Dispose();
			screen = null;
		}
	}
}
