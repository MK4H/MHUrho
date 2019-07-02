using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Input;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.UserInterface;
using MHUrho.UserInterface.MandK;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools.MandK
{
	public class UnitSpawningTool : Base.UnitSpawningTool, IMandKTool {

		Dictionary<CheckBox, UnitType> unitTypes;

		readonly GameController input;
		readonly GameUI ui;

		readonly ExclusiveCheckBoxes checkBoxes;

		bool enabled;

		public UnitSpawningTool(GameController input, GameUI ui, CameraMover camera, IntRect iconRectangle)
			:base(input, iconRectangle)
		{

			this.input = input;
			this.ui = ui;
			this.unitTypes = new Dictionary<CheckBox, UnitType>();
			this.checkBoxes = new ExclusiveCheckBoxes();

			foreach (var unitType in input.Level.Package.UnitTypes) {

				var checkBox = ui.SelectionBar.CreateCheckBox();
				checkBox.SetStyle("SelectionBarCheckBox");
				checkBox.Toggled += OnUnitTypeToggled;
				checkBox.Texture = input.Level.Package.UnitIconTexture;
				checkBox.ImageRect = unitType.IconRectangle;
				checkBox.HoverOffset = new IntVector2(unitType.IconRectangle.Width(), 0);
				checkBox.CheckedOffset = new IntVector2(2 * unitType.IconRectangle.Width(), 0);

				unitTypes.Add(checkBox, unitType);
				checkBoxes.AddCheckBox(checkBox);
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
			//ALT: Maybe don't disable, or change implementation of disable to not delete currently visible buttons
			Disable();
			foreach (var pair in unitTypes) {
				pair.Key.Toggled -= OnUnitTypeToggled;
				ui.SelectionBar.RemoveChild(pair.Key);
			}
			unitTypes = null;

			checkBoxes.Dispose();
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
										 Quaternion.Identity,
										 input.Player) != null)
					{
						return;
					}
				}
			}
		}
	}
}
