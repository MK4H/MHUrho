﻿using System;
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
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.EditorTools
{
	class BuildingBuilderToolMandK : BuildingBuilderTool, IMandKTool
	{

		Dictionary<UIElement, BuildingType> buildingTypes;

		readonly GameMandKController input;
		readonly MandKGameUI ui;

		readonly ExclusiveCheckBoxes checkBoxes;

		bool enabled;

		public BuildingBuilderToolMandK(GameMandKController input, MandKGameUI ui, CameraMover camera)
			: base(input)
		{
			this.input = input;
			this.ui = ui;
			this.buildingTypes = new Dictionary<UIElement, BuildingType>();
			this.checkBoxes = new ExclusiveCheckBoxes();

			foreach (var buildingType in PackageManager.Instance.ActiveGame.BuildingTypes) {

				var checkBox = new CheckBox();
				//TODO: Style
				checkBox.SetStyle("SelectionBarCheckBox");
				checkBox.Toggled += OnBuildingTypeToggled;
				checkBox.Texture = PackageManager.Instance.ActiveGame.BuildingIconTexture;
				checkBox.ImageRect = buildingType.IconRectangle;
				checkBox.HoverOffset = new IntVector2(buildingType.IconRectangle.Width(), 0);
				checkBox.HoverOffset = new IntVector2(2 * buildingType.IconRectangle.Width(), 0);

				buildingTypes.Add(checkBox, buildingType);
				checkBoxes.AddCheckBox(checkBox);

				ui.SelectionBarAddElement(checkBox);
			}
		}

		public override void Enable() {
			if (enabled) return;

			checkBoxes.Show();

			input.MouseDown += OnMouseDown;
			input.MouseMove += OnMouseMove;
			Level.Update += OnUpdate;
			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			checkBoxes.Deselect();
			checkBoxes.Hide();

			input.UIManager.CursorTooltips.Clear();
			//input.UIManager.SelectionBarClearButtons();
			input.MouseDown -= OnMouseDown;
			input.MouseMove -= OnMouseMove;
			Level.Update -= OnUpdate;

			Map.DisableHighlight();
			enabled = false;
		}

		public override void Dispose() {
			Disable();
			foreach (var pair in buildingTypes) {
				pair.Key.Dispose();
			}
			buildingTypes = null;
		}

		public override void ClearPlayerSpecificState() {

		}

		void OnBuildingTypeToggled(ToggledEventArgs e)
		{
			if (e.State) {
				//TODO: THINGS
				//var text = input.UIManager.CursorTooltips.AddText();
				//text.SetStyleAuto();
				//text.Value = "Hello world";
				//text.Position = new IntVector2(10, 0);

				//input.UIManager.CursorTooltips.AddImage(new IntRect(0, 0, 200, 200));
			}

		}


		void OnMouseDown(MouseButtonDownEventArgs e) {
			if (checkBoxes.Selected == null || ui.UIHovering) return;

			var tile = input.GetTileUnderCursor();
			if (tile == null) return;

			var buildingType = buildingTypes[checkBoxes.Selected];

			GetBuildingRectangle(tile, buildingType, out IntVector2 topLeft, out IntVector2 bottomRight);

			if (buildingType.CanBuildIn(topLeft, bottomRight, Level)) {
				LevelManager.CurrentLevel.BuildBuilding(buildingTypes[checkBoxes.Selected], topLeft, input.Player);
			}
		}

		void OnMouseMove(MHUrhoMouseMovedEventArgs e) {
			if (ui.UIHovering) return;

			HighlightBuildingRectangle();
		}

		void OnUpdate(float timeStep) {
			HighlightBuildingRectangle();
		}

		void GetBuildingRectangle(ITile centerTile, BuildingType buildingType, out IntVector2 topLeft, out IntVector2 bottomRight) {
			topLeft = centerTile.TopLeft - buildingType.Size / 2;
			bottomRight = topLeft + buildingType.Size - new IntVector2(1,1);
			Map.SnapToMap(ref topLeft, ref bottomRight);
		}

		void HighlightBuildingRectangle() {
			if (checkBoxes.Selected == null) return;

			var tile = input.GetTileUnderCursor();
			if (tile == null) return;

			var buildingType = buildingTypes[checkBoxes.Selected];

			GetBuildingRectangle(tile, buildingType, out IntVector2 topLeft, out IntVector2 bottomRight);

			Color color = buildingType.CanBuildIn(topLeft, bottomRight, Level) ? Color.Green : Color.Red;
			Map.HighlightRectangle(topLeft, bottomRight, color);
		}
	}
}
