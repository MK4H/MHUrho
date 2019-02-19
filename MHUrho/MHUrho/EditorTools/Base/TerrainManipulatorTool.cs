using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using Urho;

namespace MHUrho.EditorTools.Base {
	public abstract class TerrainManipulatorTool : Tool
	{
		protected TerrainManipulatorTool(IGameController input)
			: base(input, new IntRect(0, 100, 50, 150))
		{

		}

		public abstract void DeselectManipulator();
	}
}
