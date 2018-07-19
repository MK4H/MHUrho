using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.UserInterface;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools
{
	class UnitSpawningToolMandK : UnitSpawningTool, IMandKTool {

		Dictionary<UIElement, UnitType> unitTypes;

		GameMandKController input;
		readonly MandKGameUI ui;

		ExclusiveCheckBoxes checkBoxes;

		bool enabled;

		public UnitSpawningToolMandK(GameMandKController input, MandKGameUI ui, CameraMover camera)
			:base(input)
		{

			this.input = input;
			this.ui = ui;
			this.unitTypes = new Dictionary<UIElement, UnitType>();
			this.checkBoxes = new ExclusiveCheckBoxes();

			foreach (var unitType in PackageManager.Instance.ActiveGame.UnitTypes) {

				var checkBox = new CheckBox();
				checkBox.SetStyle("SelectionBarCheckBox");
				checkBox.Toggled += OnUnitTypeToggled;
				checkBox.Texture = PackageManager.Instance.ActiveGame.UnitIconTexture;
				checkBox.ImageRect = unitType.IconRectangle;
				checkBox.HoverOffset = new IntVector2(unitType.IconRectangle.Width(), 0);
				checkBox.HoverOffset = new IntVector2(2 * unitType.IconRectangle.Width(), 0);

				unitTypes.Add(checkBox, unitType);
			}
		}

		public override void Enable() {
			if (enabled) return;

			checkBoxes.Show();

			input.MouseDown += OnMouseDown;
			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			checkBoxes.Hide();

			input.MouseDown -= OnMouseDown;
			enabled = false;

		}

		public override void ClearPlayerSpecificState() {

		}

		public override void Dispose() {
			//TODO: Maybe dont disable, or change implementation of disable to not delete currently visible buttons
			Disable();
			foreach (var pair in unitTypes) {
				pair.Key.Dispose();
			}
			unitTypes = null;
		}

		void OnUnitTypeToggled(ToggledEventArgs e)
		{

		}

		void OnMouseDown(MouseButtonDownEventArgs e) {
			if (ui.UIHovering) return;

			if (checkBoxes.Selected != null) {

				foreach (var result in input.CursorRaycast()) {
					//Spawn only at buildings or map, not units, projectiles etc.
					if (!Level.TryGetBuilding(result.Node, out IBuilding dontCare) && !Level.Map.IsRaycastToMap(result)) {
						continue;
					}

					//Spawn at the first possible raycast hit
					if (Level.SpawnUnit(unitTypes[checkBoxes.Selected], 
										 Map.GetContainingTile(result.Position),
										 input.Player) != null)
					{
						return;
					}
				}
			}
		}
	}
}
