using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using Urho;

namespace MHUrho.EditorTools.Base
{
	public abstract class BuildingBuilderTool : Tool {
		protected BuildingBuilderTool(IGameController input)
			: base(input, new IntRect(0, 50, 50, 100))
		{

		}
	}
}
