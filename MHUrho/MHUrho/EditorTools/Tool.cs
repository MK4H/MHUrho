using System;
using System.Collections.Generic;
using System.Text;

using Urho.Urho2D;

namespace MHUrho.EditorTools
{
	abstract class Tool : IDisposable {
		public Texture2D Icon { get; protected set; }

		public abstract void Dispose();

		protected Tool() {
			Icon = null;
		}
	}
}
