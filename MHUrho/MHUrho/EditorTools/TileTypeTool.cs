using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using Urho;

namespace MHUrho.EditorTools
{
	public abstract class TileTypeTool : Tool
	{
		protected TileTypeTool(IGameController input)
			: base(input, new IntRect(0, 150, 50, 200))
		{

		}
	}
}
