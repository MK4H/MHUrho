using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MHUrho.Control;
using MHUrho.EditorTools;
using MHUrho.Helpers;
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
			PackageManager.Instance.GetTexture2D("Textures/xamarin.png");

		public GameMandKController InputCtl { get; protected set; }

		public bool UIHovering => hovering > 0;

		public CustomElementsWindow CustomWindow { get; private set; }

		public SelectionBar SelectionBar { get; private set; }

		IPlayer Player => InputCtl.Player;

		CameraMover cameraMover;

		readonly UIElement gameUI;

		
		readonly ExpansionWindow expansionWindow;
		readonly ExpandingSelector toolSelection;
		readonly ExpandingSelector playerSelection;
		readonly UIMinimap minimap;

	
		int hovering = 0;



		public MandKGameUI(GameMandKController input, CameraMover cameraMover) 
			:base(input.Level)
		{
			this.InputCtl = input;
			this.cameraMover = cameraMover;
			this.tools = new Dictionary<UIElement, Tool>();
			this.players = new Dictionary<UIElement, IPlayer>();
			//TODO: User texture
			this.CursorTooltips = new CursorTooltips(PackageManager.Instance.GetTexture2D("Textures/xamarin.png"),this);

			gameUI = UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/GameLayout.xml"), PackageManager.Instance.GetXmlFile("UI/GameUIStyle.xml"));
			UI.Root.AddChild(gameUI);			
			

			CustomWindow = new CustomElementsWindow((Window)gameUI.GetChild("CustomWindow"));
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

			minimap = new MandKUIMinimap((Button)gameUI.GetChild("Minimap"), this, cameraMover, Level);
			minimap.HoverBegin += UIHoverBegin;
			minimap.HoverEnd += UIHoverEnd;

		}

		public void Dispose() {
			ClearDelegates();

			foreach (var tool in tools) {
				toolSelection.RemoveCheckBox((CheckBox) tool.Key);
				tool.Key.Dispose();
				tool.Value.Dispose();
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
			checkBox.SetStyle("ToolCheckBox", PackageManager.Instance.GetXmlFile("UI/GameUIStyle.xml"));

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

		public override void AddPlayer(IPlayer player)
		{

			CheckBox checkBox = playerSelection.CreateCheckBox();
			checkBox.SetStyle("PlayerCheckBox", PackageManager.Instance.GetXmlFile("UI/GameUIStyle.xml"));


			checkBox.ImageRect = player.Insignia.ShieldIcon;

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
