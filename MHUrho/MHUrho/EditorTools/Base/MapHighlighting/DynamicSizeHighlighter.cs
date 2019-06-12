using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using Urho;

namespace MHUrho.EditorTools.Base.MapHighlighting
{
	public abstract class DynamicSizeHighlighter : MapHighlighter
	{
		protected DynamicSizeHighlighter(IGameController input)
			: base(input)
		{

		}
	}
}
