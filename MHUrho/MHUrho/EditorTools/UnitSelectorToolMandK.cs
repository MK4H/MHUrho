using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Control;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.UnitComponents;
using MHUrho.UserInterface;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools
{
	public class UnitSelectorToolMandK : UnitSelectorTool, IMandKTool {

		class SelectedInfo {
			public int Count => UnitSelectors.Count;

			public readonly List<UnitSelector> UnitSelectors;
			public readonly Button Button;

			public void DeselectAll() {
				while (UnitSelectors.Count != 0) {
					UnitSelectors[Count - 1].Deselect();
				}


			}

			public SelectedInfo(Button button, List<UnitSelector> unitSelectors) {
				Button = button;
				UnitSelectors = unitSelectors;
			}
		}

		class ClickDispatcher : IEntityVisitor<bool> {

			readonly UnitSelectorToolMandK unitSelectorTool;
			readonly MouseButtonUpEventArgs args;
			readonly Vector3 worldPosition;

			public ClickDispatcher(UnitSelectorToolMandK unitSelectorTool,
									MouseButtonUpEventArgs args,
									Vector3 worldPosition)
			{
				this.unitSelectorTool = unitSelectorTool;
				this.args = args;
				this.worldPosition = worldPosition;
			}

			public bool Visit(IUnit unit)
			{
				return unitSelectorTool.HandleUnitClick(unit, args);
			}

			public bool Visit(IBuilding building)
			{
				return unitSelectorTool.HandleBuildingClick(building, args, worldPosition);
			}

			public bool Visit(IProjectile projectile)
			{
				return false;
			}
		}


		readonly GameMandKController input;
		readonly MandKGameUI ui;

		readonly DynamicRectangleToolMandK dynamicHighlight;

		readonly Dictionary<UnitType, SelectedInfo> selected;

		readonly Dictionary<UIElement, UnitType> unitTypes;

		bool enabled;

		public UnitSelectorToolMandK(GameMandKController input, MandKGameUI ui, CameraMover camera)
			:base(input)
		{
			this.input = input;
			this.ui = ui;
			this.selected = new Dictionary<UnitType, SelectedInfo>();
			this.unitTypes = new Dictionary<UIElement, UnitType>();

			this.dynamicHighlight = new DynamicRectangleToolMandK(input, ui, camera);
		}

		public override void Enable() {
			if (enabled) return;

			dynamicHighlight.SelectionHandler += HandleAreaSelection;
			dynamicHighlight.SingleClickHandler += HandleSingleClick;

			dynamicHighlight.Enable();

			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			dynamicHighlight.SelectionHandler -= HandleAreaSelection;
			dynamicHighlight.SingleClickHandler -= HandleSingleClick;
			dynamicHighlight.Disable();

			enabled = false;
		}

		public override void ClearPlayerSpecificState() {
			DeselectAll();
		}

		public override void Dispose() {
			Disable();
			dynamicHighlight.Dispose();
		}

		public void SelectUnit(UnitSelector unitSelector)
		{
			if (unitSelector.Selected) {
				throw new InvalidOperationException($"{nameof(unitSelector)} was already selected");
			}

			unitSelector.Select();
			AddUnit(unitSelector);
		}

		public bool DeselectUnit(UnitSelector unitSelector)
		{
			if (unitSelector.Selected) {
				unitSelector.Deselect();
				//RemoveUnit is called by the event in selector
				//RemoveUnit(unitSelector);
				return true;
			}

			return false;
		}

		protected virtual void HandleAreaSelection(IntVector2 topLeft, IntVector2 bottomRight, MouseButtonUpEventArgs e)
		{

			var unitSelectorsInRectangle = Map.GetTilesInRectangle(topLeft, bottomRight)
											.SelectMany((tile) => tile.Units)
											.Where((unit) => unit.Player == input.Player)
											.Select((unit) => unit.GetDefaultComponent<UnitSelector>())
											.Where((selector) => selector != null && !selector.Selected);

			foreach (var unitSelector in unitSelectorsInRectangle) {
				SelectUnit(unitSelector);
			}
		}

		protected virtual bool HandleUnitClick(IUnit unit, MouseButtonUpEventArgs e)
		{
			if ((MouseButton)e.Button == MouseButton.Right) {
				Level.Camera.Follow(unit);
			}


			var selector = unit.GetDefaultComponent<UnitSelector>();
			//If the unit is selectable and owned by the clicking player
			if (selector != null && selector.Player == input.Player) {
				//Select if not selected, deselect if selected
				if (!selector.Selected) {
					SelectUnit(selector);
					//Executed an action, stop handling click
					return true;
				}
				else {
					DeselectUnit(selector);
					//Executed an action, stop handling click
					return true;
				}
			}
			else {
				//Either not selectable or enemy unit
				var executed = false;

				foreach (var selectedUnit in GetAllSelectedUnitSelectors()) {
					executed |= selectedUnit.Order(new AttackOrder(unit));
				}
				return executed;
			}
		}

		protected virtual bool HandleBuildingClick(IBuilding building, MouseButtonUpEventArgs e, Vector3 worldPosition)
		{
			var formationController = building.GetFormationController(worldPosition);
			if (formationController == null) {
				return false;
			}

			formationController.MoveToFormation(new UnitGroup(GetAllSelectedUnitSelectors()));
			return true;
		}

		protected virtual bool HandleTileClick(ITile tile, MouseButtonUpEventArgs e)
		{
			switch (e.Button) {
				case (int)MouseButton.Left:
					return Map.GetFormationController(tile).MoveToFormation(new UnitGroup(GetAllSelectedUnitSelectors()));
				case (int)MouseButton.Right:
					bool executed = false;
					foreach (var unit in GetAllSelectedUnitSelectors()) {
						executed = unit.Order(new ShootOrder(Map.GetRangeTarget(tile.Center3)));
					}
					return executed;
			}
			return false;
		}

		void HandleSingleClick(MouseButtonUpEventArgs e) {

			foreach (var result in input.CursorRaycast()) {

				if (Level.TryGetEntity(result.Node, out IEntity entity)) {
					var visitor = new ClickDispatcher(this, e, result.Position);
					if (entity.Accept(visitor)) {
						return;
					}
				}
				

				var tile = Map.RaycastToTile(result);
				if (tile == null) {
					return;
				}

				HandleTileClick(tile, e);
				
				return;
			}
		}


		Button CreateButton(UnitType unitType) {
			var unitIcon = unitType.IconRectangle;

			var button = new Button();
			button.SetStyle("SelectedUnitButton");
			button.Visible = true;

			Text text = (Text)button.GetChild("Count");
			text.Value = "1";

			return button;
		}

		void DisplayCount(Button button, int count) {
			Text text = (Text)button.GetChild("Count");
			text.Value = count.ToString();
		}

		void AddUnit(UnitSelector unitSelector) {
			//TODO: Check owner of the units
			var unit = unitSelector.Unit;
			if (selected.TryGetValue(unit.UnitType, out SelectedInfo info)) {
				if (info.UnitSelectors.Count == 0) {
					info.Button.Visible = true;
				}
				info.UnitSelectors.Add(unitSelector);
				DisplayCount(info.Button, info.Count);
			}
			else {
				//Create info instance
				var button = CreateButton(unit.UnitType);
				selected.Add(unit.UnitType, new SelectedInfo(button, new List<UnitSelector> { unitSelector }));
				unitTypes.Add(button, unit.UnitType);
				input.UIManager.SelectionBarAddElement(button);
			}

			unitSelector.UnitDeselected += RemoveUnit;
		}

		void RemoveUnit(UnitSelector unitSelector) {
			var info = selected[unitSelector.Unit.UnitType];
			info.UnitSelectors.Remove(unitSelector);
			if (info.Count == 0) {
				info.Button.Visible = false;
			}
			else {
				DisplayCount(info.Button, info.Count);
			}

			unitSelector.UnitDeselected -= RemoveUnit;
		}

		IEnumerable<UnitSelector> GetAllSelectedUnitSelectors() {
			foreach (var unitType in selected.Values) {
				foreach (var unitSelector in unitType.UnitSelectors) {
					yield return unitSelector;
				}
			}
		}

		void DeselectAll() {
			foreach (var unitType in selected.Values) {
				unitType.DeselectAll();
			}
		}

		
	}
}
