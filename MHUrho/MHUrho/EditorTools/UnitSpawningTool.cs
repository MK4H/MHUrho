using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using Urho;

namespace MHUrho.EditorTools
{
	public abstract class UnitSpawningTool :Tool
	{
		protected UnitSpawningTool(IGameController input)
			: base(input, new IntRect(0, 0, 50, 50))
		{

		}
	}
}
