using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		public Window CustomWindow { get; private set; }

		IPlayer Player => InputCtl.Player;

		CameraMover cameraMover;

		readonly UIElement gameUI;

		
		readonly ExpansionWindow expansionWindow;
		readonly ExpandingSelector toolSelection;
		readonly ExpandingSelector playerSelection;
		readonly SelectionBar selectionBar;
		readonly UIMinimap minimap;

	
		int hovering = 0;



		public MandKGameUI(MyGame game, GameMandKController input, CameraMover cameraMover) 
			:base(game, input.Level)
		{
			this.InputCtl = input;
			this.cameraMover = cameraMover;
			this.tools = new Dictionary<UIElement, Tool>();
			this.players = new Dictionary<UIElement, IPlayer>();
			//TODO: User texture
			this.CursorTooltips = new CursorTooltips(PackageManager.Instance.GetTexture2D("Textures/xamarin.png"),this, game);

			UI.LoadLayoutToElement(UI.Root, game.ResourceCache, "UI/GameLayout.xml");

			gameUI = UI.Root.GetChild("GameUI");

			CustomWindow = (Window)gameUI.GetChild("CustomWindow");

			expansionWindow = new ExpansionWindow((Window)gameUI.GetChild("ExpansionWindow"));
			expansionWindow.HoverBegin += UIHoverBegin;
			expansionWindow.HoverEnd += UIHoverEnd;

			toolSelection = new ExpandingSelector((CheckBox) gameUI.GetChild("ToolSelector"), expansionWindow);
			toolSelection.HoverBegin += UIHoverBegin;
			toolSelection.HoverEnd += UIHoverEnd;
			toolSelection.Selected += ToolSelected;
			playerSelection = new ExpandingSelector((CheckBox)gameUI.GetChild("PlayerSelector"), expansionWindow);
			playerSelection.HoverBegin += UIHoverBegin;
			playerSelection.HoverEnd += UIHoverEnd;

			selectionBar = new SelectionBar(gameUI);
			selectionBar.HoverBegin += UIHoverBegin;
			selectionBar.HoverEnd += UIHoverEnd;

			minimap = new MandKUIMinimap((Button)gameUI.GetChild("Minimap"), this, cameraMover, Level);
			minimap.HoverBegin += UIHoverBegin;
			minimap.HoverEnd += UIHoverEnd;

		}

		public void Dispose() {
			ClearDelegates();

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



		public void SelectionBarAddElement(UIElement element) {
			selectionBar.AddElement(element);
		}

		public void SelectionBarRemoveElement(UIElement element)
		{
			selectionBar.RemoveElement(element);
		}

		public override void AddTool(Tool tool)
		{

			CheckBox checkBox = new CheckBox();
			checkBox.SetStyle("ExpansionWindowCheckBox", PackageManager.Instance.GetXmlFile("UI/GameUIStyle.xml"));

			checkBox.ImageRect = tool.IconRectangle;

			tools.Add(checkBox, tool);
			toolSelection.AddCheckBox(checkBox);

		}

		public override void RemoveTool(Tool tool) {
			throw new NotImplementedException();
		}

		public override void AddPlayer(IPlayer player) {

			CheckBox checkBox = new CheckBox();
			checkBox.SetStyle("ExpansionWindowCheckBox", PackageManager.Instance.GetXmlFile("UI/GameUIStyle.xml"));

			//TODO: This
			checkBox.ImageRect = new IntRect(0,0,50,50);

			players.Add(checkBox, player);
			playerSelection.AddCheckBox(checkBox);		
		}

		public override void RemovePlayer(IPlayer player) {
			throw new NotImplementedException();
		}

		void UIHoverBegin(HoverBeginEventArgs e)
		{
			hovering++;
		}

		void UIHoverEnd(HoverEndEventArgs e)
		{
			hovering--;
		}

		void ClearDelegates() {
			expansionWindow.HoverBegin -= UIHoverBegin;
			expansionWindow.HoverEnd -= UIHoverEnd;

			selectionBar.HoverBegin -= UIHoverBegin;
			selectionBar.HoverEnd -= UIHoverEnd;
			toolSelection.HoverBegin -= UIHoverBegin;
			toolSelection.HoverEnd -= UIHoverEnd;

			minimap.HoverBegin -= UIHoverBegin;
			minimap.HoverEnd -= UIHoverEnd;
		}

		void ToolSelected(UIElement newSelected, UIElement oldSelected)
		{
			if (oldSelected != null) {
				tools[oldSelected].Disable();
			}

			tools[newSelected].Enable();
		}

		void PlayerSelected(UIElement newSelected, UIElement oldSelected)
		{
			InputCtl.Player = players[newSelected];
		}



	}
}
