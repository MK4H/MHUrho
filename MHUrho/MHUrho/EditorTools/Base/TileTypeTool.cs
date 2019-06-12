using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using Urho;

namespace MHUrho.EditorTools.Base
{
	public abstract class TileTypeTool : Tool
	{
		protected TileTypeTool(IGameController input, IntRect iconRectangle)
			: base(input, iconRectangle)
		{

		}
	}
}
