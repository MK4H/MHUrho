using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.EditorTools;
using MHUrho.Input;
using MHUrho.Input.Touch;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.UserInterface
{
	public class TouchUI : GameUIManager,IDisposable {


		readonly GameController touchInputCtl;

		IPlayer Player => touchInputCtl.Player;

		UIElement selectionBar;

		Dictionary<UIElement, TileType> tileTypeButtons;
		UIElement selected;


		public TouchUI(GameController gameTouchController)
			:base(gameTouchController.Level)
		{
			this.touchInputCtl = gameTouchController;
		}

		public void Dispose() {
			selectionBar.Remove();
		}

		public override bool ToolSelectionEnabled { get; }
		public override bool PlayerSelectionEnabled { get; }

		public override void AddTool(Tool tool) {
			throw new NotImplementedException();
		}

		public override void RemoveTool(Tool tool) {
			throw new NotImplementedException();
		}

		public override void SelectTool(Tool tool)
		{
			throw new NotImplementedException();
		}

		public override void DeselectTools()
		{
			throw new NotImplementedException();
		}

		public override void EnableToolSelection()
		{
			throw new NotImplementedException();
		}

		public override void DisableToolSelection()
		{
			throw new NotImplementedException();
		}

		public override void AddPlayer(IPlayer player) {
			throw new NotImplementedException();
		}

		public override void RemovePlayer(IPlayer player) {
			throw new NotImplementedException();
		}

		public override void SelectPlayer(IPlayer player)
		{
			throw new NotImplementedException();
		}

		public override void EnablePlayerSelection()
		{
			throw new NotImplementedException();
		}

		public override void DisablePlayerSelection()
		{
			throw new NotImplementedException();
		}

		public override void EnableUI() {
			throw new NotImplementedException();
		}

		public override void DisableUI() {
			throw new NotImplementedException();
		}

		public override void ShowUI() {
			throw new NotImplementedException();
		}

		public override void HideUI() {
			throw new NotImplementedException();
		}



		void SelectionBar_HoverBegin(HoverBeginEventArgs obj)
		{
			touchInputCtl.UIPressed = true;
		}

		void Button_Pressed(PressedEventArgs e)
		{
			selected?.SetColor(Color.White);
			if (selected != e.Element) {
				selected = e.Element;
				e.Element.SetColor(Color.Gray);
			}
			else {
				//DESELECT
				selected = null;
			}
		}

		void UI_Pressed(PressedEventArgs e)
		{
			touchInputCtl.UIPressed = true;
		}
	}
}
