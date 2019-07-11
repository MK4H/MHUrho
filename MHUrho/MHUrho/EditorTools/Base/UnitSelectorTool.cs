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
		protected UnitSelectorTool(IGameController input, IntRect iconRectangle)
			:base(input, iconRectangle)
		{

		}

	}
}
