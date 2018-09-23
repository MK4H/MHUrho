using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.EditorTools;
using MHUrho.Logic;

namespace DefaultPackage
{
	class ToolManagerMandK : ToolManager
	{
		readonly ILevelManager level;

		public ToolManagerMandK(ILevelManager level)
		{
			this.level = level;
			if (level.Input.InputType != MHUrho.Input.InputType.MouseAndKeyboard) {
				throw new ArgumentException("Wrong input type for this toolManager", nameof(level));
			}
		}

		public override void LoadTools()
		{
			throw new NotImplementedException();
		}

		public override void DisableTools()
		{
			throw new NotImplementedException();
		}
	}
}
