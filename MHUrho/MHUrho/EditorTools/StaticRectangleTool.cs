using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using Urho;

namespace MHUrho.EditorTools
{
	abstract class StaticRectangleTool : Tool
	{
		protected StaticRectangleTool(IGameController input)
			: base(input, new IntRect(0,0,0,0))
		{

		}
	}
}
