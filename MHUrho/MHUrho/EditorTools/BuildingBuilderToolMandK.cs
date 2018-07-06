using System;
using System.Collections.Generic;
using System.Text;
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
	class BuildingBuilderToolMandK : BuildingBuilderTool, IMandKTool
	{
		public override IEnumerable<Button> Buttons => buildingTypeButtons.Keys;

		Dictionary<Button, BuildingType> buildingTypeButtons;

		GameMandKController input;

		Button selected;

		bool enabled;

		public BuildingBuilderToolMandK(GameMandKController input, MandKGameUI ui, CameraMover camera)
			: base(input)
		{
			this.input = input;
			this.buildingTypeButtons = new Dictionary<Button, BuildingType>();

			foreach (var buildingType in PackageManager.Instance.ActiveGame.BuildingTypes) {
				var buildingIcon = buildingType.Icon;

				var buttonTexture = new Texture2D();
				buttonTexture.FilterMode = TextureFilterMode.Nearest;
				buttonTexture.SetNumLevels(1);
				buttonTexture.SetSize(buildingIcon.Width, buildingIcon.Height, Urho.Graphics.RGBAFormat, TextureUsage.Static);
				buttonTexture.SetData(buildingIcon);

				var button = new Button();
				button.SetStyle("BuildingButton");
				button.Size = new IntVector2(100, 100);
				button.HorizontalAlignment = HorizontalAlignment.Center;
				button.VerticalAlignment = VerticalAlignment.Center;
				button.Pressed += Button_Pressed;
				button.Texture = buttonTexture;
				button.FocusMode = FocusMode.ResetFocus;
				button.MaxSize = new IntVector2(100, 100);
				button.MinSize = new IntVector2(100, 100);
				button.Visible = false;

				buildingTypeButtons.Add(button, buildingType);
			}
		}

		public override void Enable() {
			if (enabled) return;

			input.UIManager.SelectionBarShowButtons(buildingTypeButtons.Keys);
			input.MouseDown += OnMouseDown;
			input.MouseMove += OnMouseMove;
			Level.Update += OnUpdate;
			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			if (selected != null) {
				input.UIManager.Deselect();
				selected = null;
			}

			input.UIManager.CursorTooltips.Clear();
			input.UIManager.SelectionBarClearButtons();
			input.MouseDown -= OnMouseDown;
			input.MouseMove -= OnMouseMove;
			Level.Update -= OnUpdate;

			Map.DisableHighlight();
			enabled = false;
		}

		public override void Dispose() {
			Disable();
			foreach (var pair in buildingTypeButtons) {
				pair.Key.Pressed -= Button_Pressed;
				pair.Key.Dispose();
			}
			buildingTypeButtons = null;
		}

		public override void ClearPlayerSpecificState() {

		}

		void Button_Pressed(PressedEventArgs e) {
			input.UIManager.CursorTooltips.Clear();
			if (selected == e.Element) {
				input.UIManager.Deselect();
				selected = null;
			}
			else {
				input.UIManager.SelectButton((Button)e.Element);
				selected = (Button)e.Element;
				var text = input.UIManager.CursorTooltips.AddText();
				text.SetStyleAuto();
				text.Value = "Hello world";
				text.Position = new IntVector2(10, 0);

				input.UIManager.CursorTooltips.AddImage(new IntRect(0, 0, 200, 200));
			}
		}

		void OnMouseDown(MouseButtonDownEventArgs e) {
			if (selected == null) return;


			var tile = input.GetTileUnderCursor();
			if (tile == null) return;

			var buildingType = buildingTypeButtons[selected];

			GetBuildingRectangle(tile, buildingType, out IntVector2 topLeft, out IntVector2 bottomRight);

			if (buildingType.CanBuildIn(topLeft, bottomRight, Level)) {
				LevelManager.CurrentLevel.BuildBuilding(buildingTypeButtons[selected], topLeft, input.Player);
			}
		}

		void OnMouseMove(MHUrhoMouseMovedEventArgs e) {
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
			if (selected == null) return;

			var tile = input.GetTileUnderCursor();
			if (tile == null) return;

			var buildingType = buildingTypeButtons[selected];

			GetBuildingRectangle(tile, buildingType, out IntVector2 topLeft, out IntVector2 bottomRight);

			Color color = buildingType.CanBuildIn(topLeft, bottomRight, Level) ? Color.Green : Color.Red;
			Map.HighlightRectangle(topLeft, bottomRight, color);
		}
	}
}
