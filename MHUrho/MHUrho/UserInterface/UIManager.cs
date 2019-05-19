using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    public abstract class UIManager
    {

		protected MHUrhoApp Game => MHUrhoApp.Instance;

		protected UI UI => Game.UI;
		protected Urho.Input Input => Game.Input;


		protected UIManager()
		{

		}
	}
}
