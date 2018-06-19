using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;

namespace MHUrho.EditorTools
{
	abstract class TileTypeTool : Tool
	{
		protected TileTypeTool(IGameController input)
			: base(input)
		{

		}
	}
}
