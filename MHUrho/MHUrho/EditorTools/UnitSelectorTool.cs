using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;

namespace MHUrho.EditorTools
{
	public abstract class UnitSelectorTool : Tool
	{
		protected UnitSelectorTool(IGameController input)
			:base(input)
		{

		}

	}
}
