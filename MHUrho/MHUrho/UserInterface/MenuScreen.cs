using System;
using System.Collections.Generic;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    abstract class MenuScreen {

		public abstract bool Visible { get; set; }

		public abstract void Show();

		public abstract void Hide();
	}
}
