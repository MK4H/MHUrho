using System;
using System.Collections.Generic;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    public class CursorTooltips : IDisposable
    {
		MandKGameUI uiCtl;
		MyGame game;

		protected UI UI => game.UI;
		protected Urho.Input Input => game.Input;

		List<UIElement> elements = new List<UIElement>();

		public CursorTooltips(MandKGameUI uiCtl, MyGame game)
		{
			this.uiCtl = uiCtl;
			this.game = game;
		}

		public Text AddText()
		{
			var newElement = UI.Cursor.CreateText();
			elements.Add(newElement);
			return newElement;
		}

		public void Clear()
		{
			foreach (var element in elements) {
				element.Remove();
			}

			elements.Clear();
		}

		public void Dispose()
		{
			
		}
	}
}
