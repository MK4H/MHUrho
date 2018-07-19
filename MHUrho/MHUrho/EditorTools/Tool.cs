using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools
{
	public abstract class Tool : IDisposable {
		public IntRect IconRectangle { get; protected set; }

		protected readonly IGameController Input;

		protected ILevelManager Level => Input.Level;

		protected IMap Map => Level.Map;

		public abstract void Dispose();

		public abstract void Enable();

		public abstract void Disable();

		public abstract void ClearPlayerSpecificState();

		protected Tool(IGameController input, IntRect iconRectangle)
		{
			this.Input = input;
		}
	}
}
