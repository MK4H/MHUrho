using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    public class UIManager
    {

		protected readonly MyGame Game;

		protected UI UI => Game.UI;
		protected Urho.Input Input => Game.Input;


		public UIManager(MyGame game)
		{
			this.Game = game;
		}
	}
}
