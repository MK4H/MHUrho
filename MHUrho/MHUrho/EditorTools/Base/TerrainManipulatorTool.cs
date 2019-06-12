using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using Urho;

namespace MHUrho.EditorTools.Base {
	public abstract class TerrainManipulatorTool : Tool
	{
		protected TerrainManipulatorTool(IGameController input, IntRect iconRectangle)
			: base(input, iconRectangle)
		{

		}

		public abstract void DeselectManipulator();
	}
}
