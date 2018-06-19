using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;

namespace MHUrho.EditorTools
{
	abstract class BuildingBuilderTool : Tool
	{
		protected BuildingBuilderTool(IGameController input)
			: base(input)
		{

		}
	}
}
