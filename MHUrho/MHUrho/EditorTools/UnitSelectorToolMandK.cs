using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Control;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools
{
	public class MandKOrderArgs : OrderArgs {
		public MouseButton ClickedButton { get; private set; }
		public MouseButton HeldDownButtons { get; private set; }
		/// <summary>
		/// Other qualifiers, like Ctrl, Alt, Shift etc.
		/// </summary>
		public int OtherQualifiers{ get; private set; }

		public MandKOrderArgs(MouseButton clickedButton, MouseButton heldDownButtons, int otherQualifiers)
		{
			this.ClickedButton = clickedButton;
			this.HeldDownButtons = heldDownButtons;
			this.OtherQualifiers = otherQualifiers;
		}
	}

	class UnitSelectorToolMandK : UnitSelectorTool, IMandKTool {

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

		public override IEnumerable<Button> Buttons =>
			from button in buttons
			where selected[button.Value].Count > 0
			select button.Key;

		readonly GameMandKController input;

		ILevelManager Level => input.Level;

		Map Map => input.Level.Map;

		readonly DynamicRectangleToolMandK dynamicHighlight;

		readonly Dictionary<UnitType, SelectedInfo> selected;

		readonly Dictionary<Button, UnitType> buttons;

		bool enabled;

		public UnitSelectorToolMandK(GameMandKController input) {
			this.input = input;
			this.selected = new Dictionary<UnitType, SelectedInfo>();
			this.buttons = new Dictionary<Button, UnitType>();

			this.dynamicHighlight = new DynamicRectangleToolMandK(input);
		}

		public override void Enable() {
			if (enabled) return;

			dynamicHighlight.SelectionHandler += HandleSelection;
			dynamicHighlight.SingleClickHandler += HandleSingleClick;

			dynamicHighlight.Enable();

			input.UIManager.SelectionBarShowButtons(Buttons);

			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			dynamicHighlight.SelectionHandler -= HandleSelection;
			dynamicHighlight.SingleClickHandler -= HandleSingleClick;
			dynamicHighlight.Disable();

			input.UIManager.SelectionBarClearButtons();

			enabled = false;
		}

		public override void ClearPlayerSpecificState() {
			DeselectAll();
		}

		public override void Dispose() {
			Disable();
			dynamicHighlight.Dispose();
		}

		void HandleSelection(IntVector2 topLeft, IntVector2 bottomRight) {
			Map.ForEachInRectangle(topLeft, bottomRight, SelectUnitsInTile);
		}

		void HandleSingleClick(MouseButtonUpEventArgs e) {
			//TODO: Check that the raycastResults are ordered by distance
			foreach (var result in input.CursorRaycast()) {

				

				//TODO: Target
				var entity = Level.GetEntity(result.Node);

				

				if (entity != null) {
					var visitor = new ClickDispatcher(this, e, result.Position);
					if (entity.Accept(visitor)) {
						return;
					}
				}
				

				var tile = Map.RaycastToTile(result);
				if (tile == null) {
					return;
				}
				//TODO: this
				switch (e.Button) {
					case (int)MouseButton.Left:
						Map.GetFormationController(tile).MoveToFormation(GetAllSelectedUnitSelectors().GetEnumerator());
						break;
					case (int) MouseButton.Right:
						foreach (var unit in GetAllSelectedUnitSelectors()) {
							unit.Order(tile, new MandKOrderArgs((MouseButton)e.Button, (MouseButton)e.Buttons, e.Qualifiers));
						}
						break;
				}

				return;
				
			}
		}

		//TODO: Select other things too
		void SelectUnitsInTile(ITile tile) {
			//TODO: Maybe delete selector class, just search for unit
			foreach (var unit in tile.Units) {
				UnitSelector selector = unit.GetDefaultComponent<UnitSelector>();

				//Not owned by player
				if (unit.Player != input.Player) continue;

				//Not selectable
				if (selector == null || selector.Selected) continue;

				selector.Select();
				AddUnit(selector);
			}
		}

		Button CreateButton(UnitType unitType) {
			var unitIcon = unitType.Icon;

			var buttonTexture = new Texture2D();
			buttonTexture.FilterMode = TextureFilterMode.Nearest;
			buttonTexture.SetNumLevels(1);
			buttonTexture.SetSize(unitIcon.Width, unitIcon.Height, Urho.Graphics.RGBAFormat, TextureUsage.Static);
			buttonTexture.SetData(unitIcon);



			var button = new Button();
			button.SetStyle("SelectedUnitButton");
			button.Size = new IntVector2(100, 100);
			button.HorizontalAlignment = HorizontalAlignment.Center;
			button.VerticalAlignment = VerticalAlignment.Center;
			//button.Pressed += Button_Pressed;
			button.Texture = buttonTexture;
			button.FocusMode = FocusMode.ResetFocus;
			button.MaxSize = new IntVector2(100, 100);
			button.MinSize = new IntVector2(100, 100);
			button.Visible = true;

			var text = button.CreateText("Count");
			text.Value = "1";
			text.HorizontalAlignment = HorizontalAlignment.Center;
			text.VerticalAlignment = VerticalAlignment.Top;
			text.SetColor(new Color(r: 0f, g: 0f, b: 0f));
			text.SetFont(font: PackageManager.Instance.ResourceCache.GetFont("Fonts/Font.ttf"), size: 30);

			return button;
		}

		void DisplayCount(Button button, int count) {
			Text text = (Text)button.GetChild("Count");
			text.Value = count.ToString();
		}

		void AddUnit(UnitSelector unitSelector) {
			//TODO: Check owner of the units
			var unit = unitSelector.GetComponent<Unit>();
			if (selected.TryGetValue(unit.UnitType, out SelectedInfo info)) {
				if (info.UnitSelectors.Count == 0) {
					input.UIManager.SelectionBarShowButton(info.Button);
				}
				info.UnitSelectors.Add(unitSelector);
				DisplayCount(info.Button, info.Count);
			}
			else {
				//Create info instance
				var button = CreateButton(unit.UnitType);
				selected.Add(unit.UnitType, new SelectedInfo(button, new List<UnitSelector> { unitSelector }));
				buttons.Add(button, unit.UnitType);
				input.UIManager.SelectionBarAddButton(button);
				input.UIManager.SelectionBarShowButton(button);
			}

			unitSelector.UnitDeselected += RemoveUnit;
		}

		void RemoveUnit(UnitSelector unitSelector) {
			var info = selected[unitSelector.Unit.UnitType];
			info.UnitSelectors.Remove(unitSelector);
			if (info.Count == 0) {
				input.UIManager.SelectionBarHideButton(info.Button);
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

		bool HandleUnitClick(IUnit unit, MouseButtonUpEventArgs e) {
			var selector = unit.GetDefaultComponent<UnitSelector>();
			//If the unit is selectable and owned by the clicking player
			if (selector != null && selector.Player == input.Player) {
				//Select if not selected, deselect if selected
				if (!selector.Selected) {
					selector.Select();
					AddUnit(selector);
					//Executed an action, stop handling click
					return true;
				}
				else {
					selector.Deselect();
					//Executed an action, stop handling click
					return true;
				}
			}
			else {
				//Either not selectable or enemy unit
				var executed = false;

				foreach (var selectedUnit in GetAllSelectedUnitSelectors()) {
					executed |= selectedUnit.Order(unit, new MandKOrderArgs((MouseButton)e.Button, (MouseButton)e.Buttons, e.Qualifiers));
				}
				return executed;
			}
		}

		bool HandleBuildingClick(IBuilding building, MouseButtonUpEventArgs e, Vector3 worldPosition)
		{
			var formationController = building.GetFormationController(worldPosition);
			if (formationController == null) {
				return false;
			}

			formationController.MoveToFormation(GetAllSelectedUnitSelectors().GetEnumerator());
			return true;
		}

	}
}
