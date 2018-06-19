using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;

namespace MHUrho.EditorTools
{
	abstract class UnitSpawningTool :Tool
	{
		protected UnitSpawningTool(IGameController input)
			: base(input)
		{

		}
	}
}
