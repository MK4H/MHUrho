using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Control;
using MHUrho.EditorTools;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.UserInterface
{
	public class MandKGameUI : GameUIManager, IDisposable {
		static Color selectedColor = Color.Gray;
		static Color mouseOverColor = new Color(0.9f, 0.9f, 0.9f);

		static Texture2D DefaultButtonTexture =
			PackageManager.Instance.ResourceCache.GetTexture2D("Textures/xamarin.png");



		readonly GameMandKController inputCtl;

		IPlayer player => inputCtl.Player;

		
		readonly UIElement toolSelection;
		readonly UIElement selectionBar;
		readonly UIElement playerSelection;
		readonly BorderImage minimap;

		UIElement selectionBarSelected;
		UIElement selectedToolButton;
		UIElement selectedPlayerButton;
		

		int hovering = 0;

		public MandKGameUI(MyGame game, GameMandKController inputCtl) 
			:base(game, inputCtl.Level)
		{
			this.inputCtl = inputCtl;
			this.tools = new Dictionary<UIElement, Tool>();
			this.players = new Dictionary<UIElement, IPlayer>();

			selectionBar = UI.Root.CreateWindow();
			selectionBar.SetStyle("windowStyle");
			selectionBar.LayoutMode = LayoutMode.Horizontal;
			selectionBar.LayoutSpacing = 10;
			selectionBar.HorizontalAlignment = HorizontalAlignment.Left;
			selectionBar.Position = new IntVector2(50, UI.Root.Height - 100);
			selectionBar.Height = 100;
			selectionBar.SetFixedWidth(UI.Root.Width - 100);
			selectionBar.SetColor(Color.Yellow);
			selectionBar.FocusMode = FocusMode.NotFocusable;
			selectionBar.ClipChildren = true;
			selectionBar.HoverBegin += UIHoverBegin;
			selectionBar.HoverEnd += UIHoverEnd;


			toolSelection = UI.Root.CreateWindow();
			toolSelection.LayoutMode = LayoutMode.Vertical;
			toolSelection.LayoutSpacing = 0;
			toolSelection.HorizontalAlignment = HorizontalAlignment.Left;
			toolSelection.VerticalAlignment = VerticalAlignment.Bottom;
			toolSelection.Height = UI.Root.Height;
			toolSelection.SetFixedWidth(50);
			toolSelection.SetColor(Color.Blue);
			toolSelection.FocusMode = FocusMode.NotFocusable;
			toolSelection.ClipChildren = true;
			toolSelection.HoverBegin += UIHoverBegin;
			toolSelection.HoverEnd += UIHoverEnd;

			playerSelection = UI.Root.CreateWindow();
			playerSelection.LayoutMode = LayoutMode.Vertical;
			playerSelection.LayoutSpacing = 0;
			playerSelection.HorizontalAlignment = HorizontalAlignment.Right;
			playerSelection.VerticalAlignment = VerticalAlignment.Bottom;
			playerSelection.Height = UI.Root.Height;
			playerSelection.SetFixedWidth(50);
			playerSelection.SetColor(Color.Blue);
			playerSelection.FocusMode = FocusMode.NotFocusable;
			playerSelection.ClipChildren = true;
			playerSelection.HoverBegin += UIHoverBegin;
			playerSelection.HoverEnd += UIHoverEnd;


			minimap = UI.Root.CreateBorderImage();


			minimap.Texture = Level.Minimap.Texture;
			minimap.MinSize = new IntVector2(minimap.Texture.Width, minimap.Texture.Height);		
			minimap.Size = minimap.MinSize;
			minimap.HorizontalAlignment = HorizontalAlignment.Center;
			minimap.VerticalAlignment = VerticalAlignment.Center;
		}

		public void Dispose() {
			ClearDelegates();
			selectionBar.RemoveAllChildren();
			selectionBar.Remove();
			selectionBar.Dispose();
			toolSelection.RemoveAllChildren();
			toolSelection.Remove();
			toolSelection.Dispose();
			playerSelection.RemoveAllChildren();
			playerSelection.Remove();
			playerSelection.Dispose();
			minimap.Remove();
			minimap.Dispose();

			Debug.Assert(selectionBar.IsDeleted, "Selection bar did not delete itself");
		}

		public override void EnableUI() {
			selectionBar.Enabled = true;
			toolSelection.Enabled = true;
			playerSelection.Enabled = true;
			minimap.Enabled = true;
		}

		public override void DisableUI() {
			selectionBar.Enabled = false;
			toolSelection.Enabled = false;
			playerSelection.Enabled = false;
			minimap.Enabled = false;
		}

		public override void ShowUI() {
			selectionBar.Visible = true;
			toolSelection.Visible = true;
			playerSelection.Visible = true;
			minimap.Visible = true;
			
		}

		public override void HideUI() {
			selectionBar.Visible = false;
			toolSelection.Visible = false;
			playerSelection.Visible = false;
			minimap.Visible = false;
		}

		

		public void SelectionBarShowButtons(IEnumerable<Button> buttons) {

			foreach (var button in buttons) {
				SelectionBarShowButton(button);
			}
		}

		public void SelectionBarShowButton(Button button) {
			if (selectionBar.FindChild(button) == uint.MaxValue) {
				throw new ArgumentException("button is not a child of the selectionBar");
			}

			button.HoverBegin += Button_HoverBegin;
			button.HoverBegin += UIHoverBegin;
			button.HoverEnd += Button_HoverEnd;
			button.HoverEnd += UIHoverEnd;
			button.Visible = true;
		}

		public void SelectionBarAddButton(Button button) {
			selectionBar.AddChild(button);
		}

		public void SelectionBarHideButton(Button button) {
			if (selectionBar.FindChild(button) == uint.MaxValue) {
				throw new ArgumentException("button is not a child of the selectionBar");
			}

			if (!button.Visible) {
				throw new ArgumentException("Hiding already hidden button");
			}

			button.HoverBegin -= Button_HoverBegin;
			button.HoverBegin -= UIHoverBegin;
			button.HoverEnd -= Button_HoverEnd;
			button.HoverEnd -= UIHoverEnd;
			button.Visible = false;
		}

		/// <summary>
		/// Deactivates buttons and hides them
		/// </summary>
		public void SelectionBarClearButtons() {
			selectionBarSelected = null;

			foreach (Button button in selectionBar.Children) {
				if (button.Visible) {
					button.HoverBegin -= Button_HoverBegin;
					button.HoverBegin -= UIHoverBegin;
					button.HoverEnd -= Button_HoverEnd;
					button.HoverEnd -= UIHoverEnd;
					button.Visible = false;
				}
			}
		}

		public void Deselect() {
			selectionBarSelected.SetColor(Color.White);
			selectionBarSelected = null;
		}

		public void SelectButton(Button button) {
			selectionBarSelected?.SetColor(Color.White);
			selectionBarSelected = button;
			selectionBarSelected.SetColor(Color.Gray);
		}

		public override void AddTool(Tool tool) {
			var button = toolSelection.CreateButton();
			button.SetStyle("toolButton");
			button.Size = new IntVector2(50, 50);
			button.HorizontalAlignment = HorizontalAlignment.Center;
			button.VerticalAlignment = VerticalAlignment.Center;
			button.Pressed += ToolSwitchbuttonPress;
			button.HoverBegin += UIHoverBegin;
			button.HoverEnd += UIHoverEnd;
			button.FocusMode = FocusMode.ResetFocus;
			button.MaxSize = new IntVector2(50, 50);
			button.MinSize = new IntVector2(50, 50);
			button.Texture = tool.Icon ?? DefaultButtonTexture;

			tools.Add(button, tool);

			foreach (var toolButton in tool.Buttons) {
				selectionBar.AddChild(toolButton);
			}

		}

		public override void RemoveTool(Tool tool) {
			throw new NotImplementedException();
		}

		public override void AddPlayer(IPlayer player) {
			var button = playerSelection.CreateButton();
			button.SetStyle("playerButton");
			button.Size = new IntVector2(50, 50);
			button.HorizontalAlignment = HorizontalAlignment.Center;
			button.VerticalAlignment = VerticalAlignment.Center;
			button.Pressed += PlayerSwitchButtonPress;
			button.HoverBegin += UIHoverBegin;
			button.HoverEnd += UIHoverEnd;
			button.FocusMode = FocusMode.ResetFocus;
			button.MaxSize = new IntVector2(50, 50);
			button.MinSize = new IntVector2(50, 50);
			button.Texture = /*IPlayer.Icon ??*/ DefaultButtonTexture;

			players.Add(button, player);
		}

		public override void RemovePlayer(IPlayer player) {
			throw new NotImplementedException();
		}

		void Button_HoverBegin(HoverBeginEventArgs e) {
			if (e.Element != selectionBarSelected) {
				e.Element.SetColor(new Color(0.9f, 0.9f, 0.9f));
			}
		}

		void Button_HoverEnd(HoverEndEventArgs e) {
			if (e.Element != selectionBarSelected) {
				e.Element.SetColor(Color.White);
			}
		}

		void UIHoverBegin(HoverBeginEventArgs e) {
			hovering++;
			inputCtl.UIHovering = true;

			Urho.IO.Log.Write(LogLevel.Debug, $"UIHovering :{hovering}");
		}

		void UIHoverEnd(HoverEndEventArgs e) {
			if (--hovering == 0) {
				inputCtl.UIHovering = false;
			}

			Urho.IO.Log.Write(LogLevel.Debug, $"UIHovering :{hovering}");
		}

		void ClearDelegates() {
			selectionBar.HoverBegin -= UIHoverBegin;
			selectionBar.HoverEnd -= UIHoverEnd;
			toolSelection.HoverBegin -= UIHoverBegin;
			toolSelection.HoverEnd -= UIHoverEnd;

			foreach (var button in selectionBar.Children) {
				button.HoverBegin -= Button_HoverBegin;
				button.HoverBegin -= UIHoverBegin;
				button.HoverEnd -= Button_HoverEnd;
				button.HoverEnd -= UIHoverEnd;
			}

			foreach (var button in toolSelection.Children) {
				button.HoverBegin -= Button_HoverBegin;
				button.HoverBegin -= UIHoverBegin;
				button.HoverEnd -= Button_HoverEnd;
				button.HoverEnd -= UIHoverEnd;
			}
		}

		void ToolSwitchbuttonPress(PressedEventArgs e) {
			if (selectedToolButton != null) {
				tools[selectedToolButton].Disable();
			}

			selectedToolButton = e.Element;
			tools[selectedToolButton].Enable();
		}

		void PlayerSwitchButtonPress(PressedEventArgs e) {
			selectedPlayerButton?.SetColor(Color.White);

			foreach (var tool in tools.Values) {
				tool.ClearPlayerSpecificState();
			}

			if (e.Element == selectedPlayerButton) {
				//TODO: Neutral player
			}
			else {
				e.Element.SetColor(selectedColor);
				selectedPlayerButton = e.Element;
				inputCtl.Player = players[selectedPlayerButton];
			}
		}

	}
}
