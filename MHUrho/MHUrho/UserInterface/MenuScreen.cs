using System;
using System.Collections.Generic;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    abstract class MenuScreen {

		public abstract bool Visible { get; set; }

		protected MenuUIManager MenuUIManager;
		protected MyGame Game;

		protected UI UI => Game.UI;

		protected MenuScreen(MyGame game, MenuUIManager menuUIManager)
		{
			this.MenuUIManager = menuUIManager;
			this.Game = game;
		}

		public abstract void Show();

		public abstract void Hide();
	}
}
