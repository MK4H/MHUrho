using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.UserInterface;

namespace MHUrho.EditorTools
{

    public abstract class ToolManager : IDisposable
    {
		protected readonly List<Tool> Tools = new List<Tool>();

		readonly GameUIManager gameUI;

		protected ToolManager(GameUIManager gameUI)
		{
			this.gameUI = gameUI;
		}

		public abstract void LoadTools();

		public void DisableTools()
		{
			gameUI.DeselectTools();
			OnToolsDisabled();
		}

		public virtual void ClearPlayerSpecificState()
		{
			foreach (var tool in Tools) {
				tool.ClearPlayerSpecificState();
			}
		}

		protected virtual void OnToolsDisabled()
		{

		}

		protected void LoadTool(Tool tool)
		{
			Tools.Add(tool);
			gameUI.AddTool(tool);
		}

		/// <summary>
		/// Removes the first occurence of tool in <see cref="Tools"/> and if the removal succeeds, removes the same tool from the UI
		///
		/// Also disposes the tool
		/// </summary>
		/// <param name="tool">The tool to remove</param>
		/// <returns>True if the tool was present in <see cref="Tools"/> and was removed, false if it was not present in <see cref="Tools"/></returns>
		protected bool RemoveTool(Tool tool)
		{
			if (!Tools.Remove(tool)) {
				return false;
			}

			gameUI.RemoveTool(tool);

			//ALT: Maybe leave disposing for the caller
			tool.Dispose();
			return true;
		}

		public virtual void Dispose()
		{
			foreach (var tool in Tools) {
				tool.Dispose();
			}
		}
	}
}
