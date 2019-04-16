using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using Urho;
using Urho.Gui;
using Urho.Urho2D;
using MHUrho.UserInterface;
using MHUrho.Packaging;
using MHUrho.Input;
using MHUrho.Control;
using MHUrho.EditorTools.MandK.TerrainManipulation;
using MHUrho.Input.MandK;
using MHUrho.UserInterface.MandK;
using MHUrho.WorldMap;

namespace MHUrho.EditorTools.MandK {
	public class TerrainManipulatorTool : MHUrho.EditorTools.Base.TerrainManipulatorTool, IMandKTool {

		
		readonly GameController input;
		readonly GameUI ui;
		readonly ExclusiveCheckBoxes checkBoxes;
		readonly CameraMover camera;
		readonly Dictionary<UIElement, TerrainManipulator> manipulators;

		TerrainManipulator manipulator;



		bool enabled;

		public TerrainManipulatorTool(GameController input, GameUI ui, CameraMover camera)
			:base(input)
		{

			//var buttonTexture = new Texture2D();
			//buttonTexture.FilterMode = TextureFilterMode.Nearest;
			//buttonTexture.SetNumLevels(1);
			//buttonTexture.SetSize(tileImage.Width, tileImage.Height, Urho.Graphics.RGBAFormat, TextureUsage.Static);
			//buttonTexture.SetData(tileType.GetImage());
			this.input = input;
			this.ui = ui;
			this.checkBoxes = new ExclusiveCheckBoxes();
			this.camera = camera;
			this.manipulators = new Dictionary<UIElement, TerrainManipulator>();

			var selectorCheckBox = ui.SelectionBar.CreateCheckBox();
			selectorCheckBox.Name = "Selector";
			selectorCheckBox.SetStyle("VertexHeightToolSelectingBox");

			VertexSelector selector = new VertexSelector(Map, input);
			checkBoxes.AddCheckBox(selectorCheckBox);
			manipulators.Add(selectorCheckBox, selector);

			var moverCheckBox = ui.SelectionBar.CreateCheckBox();
			moverCheckBox.Name = "Mover";
			moverCheckBox.SetStyle("VertexHeightToolMovingBox");

			checkBoxes.AddCheckBox(moverCheckBox);
			manipulators.Add(moverCheckBox, new VertexMover(this, selector, input, Map));


			var tileHeightCheckBox = ui.SelectionBar.CreateCheckBox();
			tileHeightCheckBox.Name = "TileHeight";
			tileHeightCheckBox.SetStyle("TileHeightToolTileHeightBox");

			checkBoxes.AddCheckBox(tileHeightCheckBox);
			manipulators.Add(tileHeightCheckBox, new TileHeightManipulator(input, ui, camera, Map));

			var terrainSmoothingCheckBox = ui.SelectionBar.CreateCheckBox();
			terrainSmoothingCheckBox.Name = "TerrainSmoothing";
			terrainSmoothingCheckBox.SetStyle("TerrainSmoothingToolCheckBox");

			checkBoxes.AddCheckBox(terrainSmoothingCheckBox);
			manipulators.Add(terrainSmoothingCheckBox, new TerrainSmoothingManipulator(input, ui, camera, Map));

			checkBoxes.SelectedChanged += OnToggled;
		}

		public override void Enable() {
			if (enabled) return;

			checkBoxes.Show();

			manipulator?.OnEnabled();

			input.MouseDown += OnMouseDown;
			input.MouseMove += OnMouseMoved;
			input.MouseUp += OnMouseUp;
			camera.CameraMoved += OnCameraMove;
			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			checkBoxes.Hide();

			manipulator?.OnDisabled();

			input.MouseDown -= OnMouseDown;
			input.MouseMove -= OnMouseMoved;
			input.MouseUp -= OnMouseUp;
			camera.CameraMoved -= OnCameraMove;
			enabled = false;
		}

		public override void ClearPlayerSpecificState() {

		}

		public override void DeselectManipulator()
		{
			//Calls this.OnToggled which does the actual deselecting
			checkBoxes.Deselect();
		}

		public override void Dispose() {
			Disable();
			

			foreach (var pair in manipulators) {
				ui.SelectionBar.RemoveChild(pair.Key);
				pair.Value.Dispose();
			}


			checkBoxes.SelectedChanged -= OnToggled;
			checkBoxes.Dispose();
			manipulator?.Dispose();
		}

		void OnMouseMoved(MHUrhoMouseMovedEventArgs args) {
			if (ui.UIHovering) return;

			manipulator?.OnMouseMoved(args);
		}

		void OnMouseDown(MouseButtonDownEventArgs args)
		{
			if (ui.UIHovering) return;

			manipulator?.OnMouseDown(args);
		}

		void OnMouseUp(MouseButtonUpEventArgs args)
		{
			if (ui.UIHovering) return;

			manipulator?.OnMouseUp(args);
		}

		void OnCameraMove(CameraMovedEventArgs args)
		{
			if (ui.UIHovering) return;

			manipulator?.OnCameraMove(args);
		}

		void OnToggled(CheckBox newCheckBox, CheckBox oldCheckBox)
		{
			if (oldCheckBox != null) {
				manipulators[oldCheckBox].OnDisabled();
				manipulator = null;
			}

			if (newCheckBox != null) {
				manipulator = manipulators[newCheckBox];
				manipulator.OnEnabled();
			}
		}

	}
}
