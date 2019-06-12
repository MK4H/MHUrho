using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.WorldMap;

namespace MHUrho.EditorTools.Base.MapHighlighting
{
	public abstract class MapHighlighter : IDisposable
	{
		protected readonly IGameController Input;

		protected ILevelManager Level => Input.Level;

		protected IMap Map => Level.Map;

		protected MapHighlighter(IGameController input)
		{
			this.Input = input;
		}

		public abstract void Dispose();

		public abstract void Enable();

		public abstract void Disable();
	}
}
