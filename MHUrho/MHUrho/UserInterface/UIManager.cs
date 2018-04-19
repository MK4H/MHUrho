﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.EditorTools;
using MHUrho.Logic;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    public abstract class UIManager
    {
		protected readonly MyGame game;

		protected UI UI => game.UI;
		protected Urho.Input Input => game.Input;

		protected Dictionary<UIElement, Tool> tools;
		protected Dictionary<UIElement, IPlayer> players;

		protected UIManager(MyGame game) {
			this.game = game;
		}

		public abstract void AddTool(Tool tool);

		public abstract void RemoveTool(Tool tool);

		public abstract void AddPlayer(IPlayer player);

		public abstract void RemovePlayer(IPlayer player);


	}
}
