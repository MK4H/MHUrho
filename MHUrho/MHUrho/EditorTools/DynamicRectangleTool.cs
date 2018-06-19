using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;

namespace MHUrho.EditorTools
{
	abstract class DynamicRectangleTool : Tool
	{
		protected DynamicRectangleTool(IGameController input)
			: base(input)
		{

		}
	}
}
