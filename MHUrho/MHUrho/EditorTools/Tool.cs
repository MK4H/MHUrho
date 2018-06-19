using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools
{
	public abstract class Tool : IDisposable {
		public Texture2D Icon { get; protected set; }


		public abstract IEnumerable<Button> Buttons { get; }

		protected readonly IGameController Input;

		protected ILevelManager Level => Input.Level;

		protected IMap Map => Level.Map;

		public abstract void Dispose();

		public abstract void Enable();

		public abstract void Disable();

		public abstract void ClearPlayerSpecificState();

		protected Tool(IGameController input)
		{
			this.Input = input;
			Icon = null;
		}
	}
}
