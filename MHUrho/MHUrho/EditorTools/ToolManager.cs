﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.EditorTools
{
	//TODO: IDisposable, now it is disposed in UI
    public abstract class ToolManager
    {
		protected List<Tool> Tools = new List<Tool>();

		public abstract void DisableTools();
	}
}