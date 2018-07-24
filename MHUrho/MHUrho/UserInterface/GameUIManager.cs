﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.EditorTools;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    public abstract class GameUIManager : UIManager
    {
		public CursorTooltips CursorTooltips { get; protected set; }

		public event Action HoverBegin;
		public event Action HoverEnd;

		protected Dictionary<UIElement, Tool> tools;
		protected Dictionary<UIElement, IPlayer> players;

		protected ILevelManager Level { get; private set; }

		protected IMap Map => Level.Map;

		protected GameUIManager(MyGame game, ILevelManager level)
			:base(game)
		{
			this.Level = level;
		}

		public abstract void AddTool(Tool tool);

		public abstract void RemoveTool(Tool tool);

		public abstract void AddPlayer(IPlayer player);

		public abstract void RemovePlayer(IPlayer player);

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
