using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.EditorTools;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	public abstract class GameUIManager : UIManager {
		public CursorTooltips CursorTooltips { get; protected set; }

		public event Action HoverBegin;
		public event Action HoverEnd;

		public abstract bool ToolSelectionEnabled { get; }

		public abstract bool PlayerSelectionEnabled { get; }

		protected Dictionary<UIElement, Tool> tools;
		protected Dictionary<UIElement, IPlayer> players;

		protected ILevelManager Level { get; private set; }

		protected IMap Map => Level.Map;

		protected GameUIManager(ILevelManager level)
		{
			this.Level = level;
		}

		public abstract void AddTool(Tool tool);

		public abstract void RemoveTool(Tool tool);

		public abstract void SelectTool(Tool tool);

		public abstract void DeselectTools();

		public abstract void EnableToolSelection();

		public abstract void DisableToolSelection();

		public abstract void AddPlayer(IPlayer player);

		public abstract void RemovePlayer(IPlayer player);

		public abstract void SelectPlayer(IPlayer player);

		public abstract void EnablePlayerSelection();

		public abstract void DisablePlayerSelection();

		public abstract void EnableUI();

		public abstract void DisableUI();

		public abstract void ShowUI();

		public abstract void HideUI();

		protected void OnHoverBegin()
		{
			HoverBegin?.Invoke();
		}

		protected void OnHoverEnd()
		{
			HoverEnd?.Invoke();
		}
	}
}
