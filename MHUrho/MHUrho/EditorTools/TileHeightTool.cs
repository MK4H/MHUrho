using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;

namespace MHUrho.EditorTools
{
	abstract class TileHeightTool : Tool
	{
		protected TileHeightTool(IGameController input)
			: base(input)
		{

		}
	}
}
