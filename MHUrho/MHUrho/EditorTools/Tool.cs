using System;
using System.Collections.Generic;
using System.Text;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools
{
	public abstract class Tool : IDisposable {
		public Texture2D Icon { get; protected set; }

		public abstract IEnumerable<Button> Buttons { get; }

		public abstract void Dispose();

		public abstract void Enable();

		public abstract void Disable();

		public abstract void ClearPlayerSpecificState();

		protected Tool() {
			Icon = null;
		}
	}
}
