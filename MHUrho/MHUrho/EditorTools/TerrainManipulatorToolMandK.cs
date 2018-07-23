using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Gui;
using Urho.Urho2D;
using MHUrho.UserInterface;
using MHUrho.Packaging;
using MHUrho.Input;
using MHUrho.Control;
using MHUrho.EditorTools.TerrainManipulation;
using MHUrho.WorldMap;

namespace MHUrho.EditorTools {
	class TerrainManipulatorToolMandK : TerrainManipulatorTool, IMandKTool {

		
		readonly GameMandKController input;
		readonly MandKGameUI ui;
		readonly ExclusiveCheckBoxes checkBoxes;

		readonly Dictionary<UIElement, TerrainManipulator> manipulators;

		TerrainManipulator manipulator;



		bool enabled;

		public TerrainManipulatorToolMandK(GameMandKController input, MandKGameUI ui, CameraMover camera)
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

			checkBoxes.SelectedChanged += OnToggled;
		}

		public override void Enable() {
			if (enabled) return;

			checkBoxes.Show();

			input.MouseDown += OnMouseDown;
			input.MouseMove += OnMouseMoved;
			input.MouseUp += OnMouseUp;
			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			checkBoxes.Hide();

			input.ShowCursor();
			input.MouseDown -= OnMouseDown;
			input.MouseMove -= OnMouseMoved;
			input.MouseUp -= OnMouseUp;
			enabled = false;
		}

		public override void ClearPlayerSpecificState() {

		}

		public override void DeselectManipulator()
		{
			manipulator?.OnDisabled();
			manipulator = null;
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

		void OnMouseMoved(MHUrhoMouseMovedEventArgs e) {
			if (ui.UIHovering) return;

			manipulator?.OnMouseMoved(e);
		}

		void OnMouseDown(MouseButtonDownEventArgs e)
		{
			if (ui.UIHovering) return;

			manipulator?.OnMouseDown(e);
		}

		void OnMouseUp(MouseButtonUpEventArgs e)
		{
			if (ui.UIHovering) return;

			manipulator?.OnMouseUp(e);
		}

		void OnToggled(CheckBox newCheckBox, CheckBox oldCheckBox)
		{
			if (oldCheckBox != null) {
				manipulators[oldCheckBox].OnDisabled();
			}

			if (newCheckBox != null) {
				manipulator = manipulators[newCheckBox];
				manipulator.OnEnabled();
			}
		}

	}
}
