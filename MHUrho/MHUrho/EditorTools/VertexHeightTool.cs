using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;

namespace MHUrho.EditorTools {
	abstract class VertexHeightTool : Tool
	{
		protected VertexHeightTool(IGameController input)
			: base(input)
		{

		}
	}
}
