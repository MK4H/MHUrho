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

		public TouchUI(GameController gameTouchController)
			:base(gameTouchController.Level)
		{

		}

		public void Dispose() {

		}

		public override bool ToolSelectionEnabled { get; } = false;
		public override bool PlayerSelectionEnabled { get; } = false;

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
	}
}
