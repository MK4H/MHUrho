using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.Control;
using MHUrho.EditorTools;
using MHUrho.Helpers;
using MHUrho.Input;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.UserInterface.MandK
{
	public class GameUI : GameUIManager, IDisposable {
		static Color selectedColor = Color.Gray;
		static Color mouseOverColor = new Color(0.9f, 0.9f, 0.9f);

		static Texture2D DefaultButtonTexture =
			MHUrhoApp.Instance.PackageManager.GetTexture2D("Textures/xamarin.png");

		public GameController InputCtl { get; protected set; }

		public CursorTooltips CursorTooltips { get; protected set; }

		public override UIElement GameUIRoot => gameUI;

		public bool UIHovering => hovering > 0;

		public CustomElementsWindow CustomWindow { get; private set; }

		public SelectionBar SelectionBar { get; private set; }

		public override bool ToolSelectionEnabled => toolSelection.Enabled;
		public override bool PlayerSelectionEnabled => playerSelection.Enabled;

		IPlayer Player => InputCtl.Player;

		CameraMover cameraMover;

		readonly UIElement gameUI;

		
		readonly ExpansionWindow expansionWindow;
		readonly ExpandingSelector toolSelection;
		readonly ExpandingSelector playerSelection;
		readonly UIMinimap minimap;

		readonly HashSet<UIElement> registeredForHover;

		int hovering = 0;



		public GameUI(GameController input, CameraMover cameraMover) 
			:base(input.Level)
		{
			this.InputCtl = input;
			this.cameraMover = cameraMover;
			this.tools = new Dictionary<UIElement, Tool>();
			this.players = new Dictionary<UIElement, IPlayer>();
			//TODO: User texture
			this.CursorTooltips = new CursorTooltips(Level.PackageManager.GetTexture2D("Textures/xamarin.png"),this);
			this.registeredForHover = new HashSet<UIElement>();

			gameUI = UI.LoadLayout(Level.PackageManager.GetXmlFile("UI/GameLayout.xml", true),
									Level.PackageManager.GetXmlFile("UI/GameUIStyle.xml", true));
			UI.Root.AddChild(gameUI);
			gameUI.Visible = false;

			CustomWindow = new CustomElementsWindow((Window)gameUI.GetChild("CustomWindow"), UI, Game.ResourceCache);
			CustomWindow.HoverBegin += UIHoverBegin;
			CustomWindow.HoverEnd += UIHoverEnd;

			expansionWindow = new ExpansionWindow((Window)gameUI.GetChild("ExpansionWindow"));
			expansionWindow.Hide();
			expansionWindow.HoverBegin += UIHoverBegin;
			expansionWindow.HoverEnd += UIHoverEnd;

			toolSelection = new ExpandingSelector((CheckBox) gameUI.GetChild("ToolSelector"), expansionWindow);
			toolSelection.HoverBegin += UIHoverBegin;
			toolSelection.HoverEnd += UIHoverEnd;
			toolSelection.Selected += ToolSelected;
			playerSelection = new ExpandingSelector((CheckBox)gameUI.GetChild("PlayerSelector"), expansionWindow);
			playerSelection.HoverBegin += UIHoverBegin;
			playerSelection.HoverEnd += UIHoverEnd;
			playerSelection.Selected += PlayerSelected;

			SelectionBar = new SelectionBar(gameUI);
			SelectionBar.HoverBegin += UIHoverBegin;
			SelectionBar.HoverEnd += UIHoverEnd;

			minimap = new UIMinimap((Button)gameUI.GetChild("Minimap"), this, cameraMover, Level);
			minimap.HoverBegin += UIHoverBegin;
			minimap.HoverEnd += UIHoverEnd;

			
		}

		public void Dispose() {
			ClearDelegates();

			foreach (var tool in tools) {
				toolSelection.RemoveCheckBox((CheckBox) tool.Key);
				tool.Key.Dispose();
			}

			tools.Clear();

			foreach (var player in players) {
				playerSelection.RemoveCheckBox((CheckBox)player.Key);
				player.Key.Dispose();
			}

			players.Clear();

			CustomWindow.Dispose();
			expansionWindow.Dispose();
			toolSelection.Dispose();
			playerSelection.Dispose();
			SelectionBar.Dispose();
			minimap.Dispose();

			gameUI.RemoveAllChildren();
			gameUI.Remove();
			gameUI.Dispose();

			Debug.Assert(gameUI.IsDeleted, "gameUI did not delete itself");
		}



		public override void EnableUI()
		{
			gameUI.Enabled = true;
		}

		public override void DisableUI() {
			gameUI.Enabled = false;
		}

		public override void ShowUI()
		{
			gameUI.Visible = true;
		}

		public override void HideUI()
		{
			gameUI.Visible = false;
		}

		public override void AddTool(Tool tool)
		{

			CheckBox checkBox = toolSelection.CreateCheckBox();
			checkBox.SetStyle("ToolCheckBox", Game.PackageManager.GetXmlFile("UI/GameUIStyle.xml", true));
			checkBox.Texture = InputCtl.Level.Package.ToolIconTexture;
			checkBox.ImageRect = tool.IconRectangle;

			tools.Add(checkBox, tool);

		}

		public override void RemoveTool(Tool tool) {
			UIElement toolElement = tools.Where((pair) => pair.Value == tool).Select((pair) => pair.Key).FirstOrDefault();

			if (toolElement == null) {
				throw new ArgumentException("Could not remove tool that was not previously added", nameof(tool));
			}

			toolSelection.RemoveCheckBox((CheckBox) toolElement);

			tools.Remove(toolElement);
		}

		public override void SelectTool(Tool tool)
		{
			foreach (var pair in tools) {
				if (pair.Value == tool) {
					toolSelection.Select((CheckBox)pair.Key);
				}
			}
		}

		public override void DeselectTools()
		{
			toolSelection.Deselect();
		}

		public override void EnableToolSelection()
		{
			toolSelection.Enable();
		}

		public override void DisableToolSelection()
		{
			toolSelection.Disable();
		}

		public override void AddPlayer(IPlayer player)
		{

			CheckBox checkBox = playerSelection.CreateCheckBox();
			checkBox.SetStyle("PlayerCheckBox", Game.PackageManager.GetXmlFile("UI/GameUIStyle.xml", true));


			checkBox.ImageRect = player.Insignia.ShieldRectangle;

			players.Add(checkBox, player);		
		}

		public override void RemovePlayer(IPlayer player) {
			UIElement playerElement = players.Where((pair) => pair.Value == player).Select((pair) => pair.Key).FirstOrDefault();

			if (playerElement == null) {
				throw new ArgumentException("Could not remove player that was not previously added", nameof(player));
			}

			playerSelection.RemoveCheckBox((CheckBox)playerElement);

			players.Remove(playerElement);
		}

		public override void SelectPlayer(IPlayer player)
		{
			foreach (var pair in players) {
				if (pair.Value == player) {
					playerSelection.Select((CheckBox)pair.Key);
				}
			}
		}

		public override void EnablePlayerSelection()
		{
			playerSelection.Enable();
		}

		public override void DisablePlayerSelection()
		{
			playerSelection.Disable();
		}

		public override void RegisterForHover(UIElement element)
		{
			if (registeredForHover.Contains(element)) {
				return;
			}

			element.HoverBegin += UIHoverBegin;
			element.HoverEnd += UIHoverEnd;
			registeredForHover.Add(element);
		}

		public override void UnregisterForHover(UIElement element)
		{
			if (!registeredForHover.Remove(element)) {
				return;
			}

			if (element.Hovering) {
				UIHoverEnd(new HoverEndEventArgs());
			}
			element.HoverBegin -= UIHoverBegin;
			element.HoverEnd -= UIHoverEnd;
		}

		void UIHoverBegin(HoverBeginEventArgs e)
		{
			if (hovering == 0) {
				OnHoverBegin();
			}
			hovering++;
		}

		void UIHoverEnd(HoverEndEventArgs e)
		{
			hovering--;
			if (hovering == 0) {
				OnHoverEnd();
			}
		}

		void ClearDelegates() {
			CustomWindow.HoverBegin -= UIHoverBegin;
			CustomWindow.HoverEnd -= UIHoverEnd;

			expansionWindow.HoverBegin -= UIHoverBegin;
			expansionWindow.HoverEnd -= UIHoverEnd;

			toolSelection.HoverBegin -= UIHoverBegin;
			toolSelection.HoverEnd -= UIHoverEnd;
			toolSelection.Selected -= ToolSelected;

			playerSelection.HoverBegin -= UIHoverBegin;
			playerSelection.HoverEnd -= UIHoverEnd;

			SelectionBar.HoverBegin -= UIHoverBegin;
			SelectionBar.HoverEnd -= UIHoverEnd;

			minimap.HoverBegin -= UIHoverBegin;
			minimap.HoverEnd -= UIHoverEnd;

			foreach (var element in registeredForHover) {
				//TODO: Check if is deleted
				element.HoverBegin -= UIHoverBegin;
				element.HoverEnd -= UIHoverEnd;
			}
		}

		void ToolSelected(UIElement newSelected, UIElement oldSelected)
		{
			if (oldSelected != null) {
				tools[oldSelected].Disable();
			}

			if (newSelected != null) {
				tools[newSelected].Enable();
			}
			
		}

		void PlayerSelected(UIElement newSelected, UIElement oldSelected)
		{
			InputCtl.ChangeControllingPlayer(players[newSelected]);
		}



	}
}
