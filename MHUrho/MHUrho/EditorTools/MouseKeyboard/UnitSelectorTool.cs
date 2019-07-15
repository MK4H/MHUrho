using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.Control;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Input;
using MHUrho.Input.MouseKeyboard;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.DefaultComponents;
using MHUrho.EditorTools.MouseKeyboard.MapHighlighting;
using MHUrho.UserInterface;
using MHUrho.UserInterface.MouseKeyboard;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools.MouseKeyboard
{
	public class UnitSelectorTool : Base.UnitSelectorTool, IMouseKeyboardTool {

		class SelectedInfo : IDisposable {
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

			public void Dispose()
			{
				Button.Dispose();
			}
		}

		class ClickDispatcher : IEntityVisitor<bool> {

			readonly UnitSelectorTool unitSelectorTool;
			readonly MouseButtonUpEventArgs args;
			readonly Vector3 worldPosition;

			public ClickDispatcher(UnitSelectorTool unitSelectorTool,
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


		readonly GameController input;
		readonly GameUI ui;

		readonly DynamicSizeHighlighter dynamicHighlight;

		readonly Dictionary<UnitType, SelectedInfo> selected;

		readonly Dictionary<UIElement, UnitType> unitTypes;

		readonly UIElement uiElem;
		readonly Button deselectButton;

		bool enabled;

		public UnitSelectorTool(GameController input, GameUI ui, CameraMover camera, IntRect iconRectangle)
			:base(input, iconRectangle)
		{
			this.input = input;
			this.ui = ui;
			this.selected = new Dictionary<UnitType, SelectedInfo>();
			this.unitTypes = new Dictionary<UIElement, UnitType>();

			this.dynamicHighlight = new DynamicSizeHighlighter(input, ui, camera);
			this.enabled = false;

			InitUI(ui, out uiElem, out deselectButton);
		}

		public override void Enable() {
			if (enabled) return;

			dynamicHighlight.Selected += HandleAreaSelection;
			dynamicHighlight.SingleClick += HandleSingleClick;

			deselectButton.Pressed += DeselectButtonPressed;

			dynamicHighlight.Enable();

			foreach (var type in selected.Values) {
				if (type.Count != 0) {
					type.Button.Visible = true;
				}

			}

			uiElem.Visible = true;

			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			dynamicHighlight.Selected -= HandleAreaSelection;
			dynamicHighlight.SingleClick -= HandleSingleClick;

			deselectButton.Pressed -= DeselectButtonPressed;

			dynamicHighlight.Disable();

			foreach (var type in selected.Values) {
				type.Button.Visible = false;
			}

			uiElem.Visible = false;

			enabled = false;
		}

		public override void ClearPlayerSpecificState() {
			DeselectAll();
		}

		public override void Dispose() {
			Disable();
			dynamicHighlight.Dispose();

			foreach (var element in unitTypes.Keys) {
				ui.SelectionBar.RemoveChild(element);
				element.Dispose();
			}

			deselectButton.Dispose();
			uiElem.Dispose();
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

		/// <summary>
		/// Tries to handle a click on a unit.
		/// </summary>
		/// <param name="unit">The clicked unit.</param>
		/// <param name="e">Event data.</param>
		/// <returns>True if the event was handled and should not be propagated to other things behind the clicked unit.</returns>
		protected virtual bool HandleUnitClick(IUnit unit, MouseButtonUpEventArgs e)
		{
			if ((MouseButton)e.Button == MouseButton.Right) {
				Level.Camera.Follow(unit);
				return true;
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

		/// <summary>
		/// Tries to handle a click on a building.
		/// </summary>
		/// <param name="building">The clicked building.</param>
		/// <param name="e">Event data.</param>
		/// <param name="worldPosition">Position of the intersection of raycast used for click and the building geometry.</param>
		/// <returns>True if the event was handled and should not be propagated to other things behind the clicked building.</returns>
		protected virtual bool HandleBuildingClick(IBuilding building, MouseButtonUpEventArgs e, Vector3 worldPosition)
		{
			//Right clicked enemy building
			if ((MouseButton)e.Button == MouseButton.Right)
			{
				if (building.Player == input.Player) {
					return false;
				}
				var executed = false;
				foreach (var selectedUnit in GetAllSelectedUnitSelectors())
				{
					executed |= selectedUnit.Order(new AttackOrder(building));
				}
				return executed;
			}

			var formationController = building.GetFormationController(worldPosition);
			if (formationController == null) {
				return false;
			}

			formationController.MoveToFormation(new UnitGroup(GetAllSelectedUnitSelectors()));
			return true;
		}

		/// <summary>
		/// Tries to handle a click on a tile.
		/// </summary>
		/// <param name="tile">The clicked tile.</param>
		/// <param name="e">Event data.</param>
		/// <returns>True if the event was handled and should not be propagated to other things behind the clicked tile.</returns>
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

		static void InitUI(GameUI ui, out UIElement uiElem, out Button deselectButton)
		{
			if ((uiElem = ui.CustomWindow.GetChild("SelectionToolUI")) == null) {
				ui.CustomWindow.LoadLayout("UI/SelectionToolUI.xml");
				uiElem = ui.CustomWindow.GetChild("SelectionToolUI");
			}

			deselectButton = (Button)uiElem.GetChild("DeselectButton", true);

			uiElem.Visible = false;
		}

		void HandleSingleClick(MouseButtonUpEventArgs e) {

			foreach (var result in input.CursorRaycast()) {

				for (Node current = result.Node; current != Level.LevelNode && current != null; current = current.Parent) {
					if (Level.TryGetEntity(current, out IEntity entity))
					{
						//Dispatch the click with the actual position of the click
						var visitor = new ClickDispatcher(this, e, result.Position);
						if (entity.Accept(visitor))
						{
							return;
						}
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
			var button = ui.SelectionBar.CreateButton();
			button.SetStyle("SelectedUnitButton");
			button.Texture = input.Level.Package.UnitIconTexture;
			button.ImageRect = unitType.IconRectangle;
			button.HoverOffset = new IntVector2(unitType.IconRectangle.Width(), 0);
			button.PressedOffset = new IntVector2(unitType.IconRectangle.Width() * 2, 0);

			Text text = button.CreateText("Count");
			text.SetStyle("SelectedUnitText");
			text.Value = "1";

			return button;
		}

		void DisplayCount(Button button, int count) {
			Text text = (Text)button.GetChild("Count");
			text.Value = count.ToString();
		}

		void AddUnit(UnitSelector unitSelector) {
			
			var unit = unitSelector.Unit;

			if (unit.Player != input.Player) {
				return;
			}

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

				button.Visible = true;
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

		void DeselectButtonPressed(PressedEventArgs obj)
		{
			DeselectAll();
		}
	}
}
