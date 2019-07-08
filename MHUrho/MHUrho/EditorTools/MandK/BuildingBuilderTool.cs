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
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.EditorTools.MandK
{
	public class BuildingBuilderTool : MHUrho.EditorTools.Base.BuildingBuilderTool, IMandKTool
	{

		Dictionary<CheckBox, BuildingType> buildingTypes;

		readonly GameController input;
		readonly GameUI ui;

		readonly ExclusiveCheckBoxes checkBoxes;

		bool enabled;

		public BuildingBuilderTool(GameController input, GameUI ui, CameraMover camera, IntRect iconRectangle)
			: base(input, iconRectangle)
		{
			this.input = input;
			this.ui = ui;
			this.buildingTypes = new Dictionary<CheckBox, BuildingType>();
			this.checkBoxes = new ExclusiveCheckBoxes();
			this.enabled = false;

			foreach (var buildingType in input.Level.Package.BuildingTypes) {

				var checkBox = ui.SelectionBar.CreateCheckBox();
				checkBox.SetStyle("SelectionBarCheckBox");
				checkBox.Toggled += OnBuildingTypeToggled;
				checkBox.Texture = input.Level.Package.BuildingIconTexture;
				checkBox.ImageRect = buildingType.IconRectangle;
				checkBox.HoverOffset = new IntVector2(buildingType.IconRectangle.Width(), 0);
				checkBox.CheckedOffset = new IntVector2(2 * buildingType.IconRectangle.Width(), 0);

				buildingTypes.Add(checkBox, buildingType);
				checkBoxes.AddCheckBox(checkBox);
			}
		}

		public override void Enable() {
			if (enabled) return;

			checkBoxes.Show();

			input.MouseDown += OnMouseDown;
			input.MouseMove += OnMouseMove;
			Level.Update += OnUpdate;
			ui.HoverBegin += UIHoverBegin;
			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			checkBoxes.Deselect();
			checkBoxes.Hide();

			input.UIManager.CursorTooltips.Clear();
			input.MouseDown -= OnMouseDown;
			input.MouseMove -= OnMouseMove;
			Level.Update -= OnUpdate;
			ui.HoverBegin -= UIHoverBegin;

			Map.DisableHighlight();
			enabled = false;
		}

		public override void Dispose() {
			Disable();
			foreach (var pair in buildingTypes) {
				pair.Key.Toggled -= OnBuildingTypeToggled;
				ui.SelectionBar.RemoveChild(pair.Key);
			}

			checkBoxes.Dispose();
			buildingTypes = null;
		}

		public override void ClearPlayerSpecificState() {

		}

		void OnBuildingTypeToggled(ToggledEventArgs e)
		{
			//NOTE: Can display stuff on toggle
		}


		void OnMouseDown(MouseButtonDownEventArgs e) {
			if (checkBoxes.Selected == null || ui.UIHovering) return;

			var tile = input.GetTileUnderCursor();
			if (tile == null) return;

			var buildingType = buildingTypes[checkBoxes.Selected];

			GetBuildingRectangle(tile, buildingType, out IntVector2 topLeft, out IntVector2 bottomRight);

			if (buildingType.CanBuild(topLeft, input.Player, Level)) {
				LevelManager.CurrentLevel.BuildBuilding(buildingTypes[checkBoxes.Selected], topLeft, Quaternion.Identity, input.Player);
			}
		}

		void OnMouseMove(MHUrhoMouseMovedEventArgs e) {
			if (ui.UIHovering) return;

			HighlightBuildingRectangle();
		}

		void OnUpdate(float timeStep) {
			if (ui.UIHovering) return;

			HighlightBuildingRectangle();
		}

		void GetBuildingRectangle(ITile centerTile, BuildingType buildingType, out IntVector2 topLeft, out IntVector2 bottomRight) {
			topLeft = centerTile.TopLeft - buildingType.Size / 2;
			bottomRight = buildingType.GetBottomRightTileIndex(topLeft);
			Map.SnapToMap(ref topLeft, ref bottomRight);
		}

		void HighlightBuildingRectangle() {
			if (checkBoxes.Selected == null) return;

			var tile = input.GetTileUnderCursor();
			if (tile == null) return;

			var buildingType = buildingTypes[checkBoxes.Selected];

			GetBuildingRectangle(tile, buildingType, out IntVector2 topLeft, out IntVector2 bottomRight);

			Color color = buildingType.CanBuild(topLeft, input.Player, Level) ? Color.Green : Color.Red;
			Map.HighlightRectangle(topLeft, bottomRight, color);
		}

		void UIHoverBegin()
		{
			Map.DisableHighlight();
		}
	}
}
