using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.UserInterface
{
    public class CursorTooltips : IDisposable
    {
		MandKGameUI uiCtl;
		MyGame game;

		protected UI UI => game.UI;
		protected Urho.Input Input => game.Input;

		readonly Texture2D images;

		List<UIElement> elements = new List<UIElement>();

		public CursorTooltips(Texture2D images, MandKGameUI uiCtl, MyGame game)
		{
			this.images = images;
			this.uiCtl = uiCtl;
			this.game = game;
		}

		public Text AddText()
		{
			var newElement = UI.Cursor.CreateText();
			elements.Add(newElement);
			return newElement;
		}

		public void AddImage(IntRect imageRect)
		{
			var newElement = UI.Cursor.CreateBorderImage();
			newElement.Texture = images;
			newElement.ImageRect = imageRect;
			newElement.MinSize = new IntVector2(100, 100);
			newElement.Position = new IntVector2(10, 50);
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
