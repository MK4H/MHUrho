using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using Urho;

namespace MHUrho.EditorTools.Base
{
	public abstract class UnitSelectorTool : Tool
	{
		protected UnitSelectorTool(IGameController input)
			:base(input, new IntRect(0,200, 50, 250))
		{

		}

	}
}
