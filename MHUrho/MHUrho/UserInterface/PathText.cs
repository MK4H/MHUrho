using System;
using System.Collections.Generic;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class PathText : IDisposable
	{
		public Text Element { get; private set; }

		public string Value {
			get => Element.Value;
			set => Element.Value = value;
		}

		public bool HasDefaultValue => Value == baseValue;

		readonly string baseValue;

		public PathText(Text textElement)
		{
			this.Element = textElement;
			baseValue = textElement.Value;
		}

		public void Dispose()
		{
			Element.Dispose();
		}
	}
}
