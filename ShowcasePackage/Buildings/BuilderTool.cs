using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.CameraMovement;
using MHUrho.EditorTools;
using MHUrho.EditorTools.MandK;
using MHUrho.Helpers.Extensions;
using MHUrho.Input;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.DefaultComponents;
using MHUrho.UserInterface.MandK;
using ShowcasePackage.Buildings;
using Urho;
using Urho.Gui;

namespace ShowcasePackage.Buildings
{
	class BuilderTool : Tool, IMandKTool {
		static readonly string[] PlayBuilders =
		{
			GateType.TypeName,
			WallType.TypeName,
			TowerType.TypeName,
			TreeCutterType.TypeName,
			KeepType.TypeName
		};

		static readonly string[] EditBuilders = 
		{
			GateType.TypeName,
			WallType.TypeName,
			TowerType.TypeName,
			TreeCutterType.TypeName,
			KeepType.TypeName,
			TreeType.TypeName
		};

		Dictionary<CheckBox, Builder> builders;

		readonly GameController input;
		readonly GameUI ui;

		readonly ExclusiveCheckBoxes checkBoxes;

		bool enabled;

		Builder currentBuilder;

		public BuilderTool(GameController input, GameUI ui, CameraMover camera, IntRect iconRectangle)
			: base(input, iconRectangle)
		{
			this.input = input;
			this.ui = ui;
			this.builders = new Dictionary<CheckBox, Builder>();
			this.checkBoxes = new ExclusiveCheckBoxes();

			checkBoxes.SelectedChanged += OnSelectedChanged;

			IEnumerable<string> activeBuilders = Level.EditorMode ? EditBuilders : PlayBuilders;
			foreach (var builder in activeBuilders) {
				BaseBuildingTypePlugin typePlugin = (BaseBuildingTypePlugin)Level.Package.GetBuildingType(builder).Plugin;
				InitCheckbox(typePlugin.GetBuilder(input, ui, camera), input.Level.Package);
			}
		}

		public override void Dispose()
		{
			Disable();
			foreach (var pair in builders)
			{
				ui.SelectionBar.RemoveChild(pair.Key);
			}

			checkBoxes.Dispose();
			builders = null;
		}

		public override void Enable()
		{
			if (enabled) return;

			checkBoxes.Show();

			input.MouseWheelMoved += OnMouseWheelMoved;
			input.MouseUp += OnMouseUp;
			input.MouseDown += OnMouseDown;
			input.MouseMove += OnMouseMove;
			Level.Update += OnUpdate;
			ui.HoverBegin += UIHoverBegin;
			ui.HoverEnd += UIHoverEnd;
			enabled = true;
		}

		public override void Disable()
		{
			if (!enabled) return;

			checkBoxes.Deselect();
			checkBoxes.Hide();

			input.MouseWheelMoved -= OnMouseWheelMoved;
			input.MouseUp -= OnMouseUp;
			input.MouseDown -= OnMouseDown;
			input.MouseMove -= OnMouseMove;
			Level.Update -= OnUpdate;
			ui.HoverBegin -= UIHoverBegin;
			ui.HoverEnd -= UIHoverEnd;

			Map.DisableHighlight();
			enabled = false;
		}

		public override void ClearPlayerSpecificState()
		{

		}

		void InitCheckbox(Builder builder, GamePack package)
		{
			var checkBox = ui.SelectionBar.CreateCheckBox();
			checkBox.SetStyle("SelectionBarCheckBox");
			checkBox.Texture = package.BuildingIconTexture;
			checkBox.ImageRect = builder.BuildingType.IconRectangle;
			checkBox.HoverOffset = new IntVector2(builder.BuildingType.IconRectangle.Width(), 0);
			checkBox.CheckedOffset = new IntVector2(2 * builder.BuildingType.IconRectangle.Width(), 0);
			checkBoxes.AddCheckBox(checkBox);
			builders.Add(checkBox, builder);
		}

		void OnSelectedChanged(CheckBox newSelected, CheckBox oldSelected)
		{
			currentBuilder?.Disable();
			currentBuilder = null;

			if (newSelected != null) {
				currentBuilder = builders[newSelected];
				currentBuilder.Enable();
			}
		}

		void OnMouseDown(MouseButtonDownEventArgs e)
		{
			if (ui.UIHovering) return;

			if (currentBuilder == null) {
				var raycast = input.CursorRaycast();
				foreach (var result in raycast) {
					if (!Level.TryGetBuilding(result.Node, out IBuilding building)) {
						continue;
					}

					Clicker clicker = null;
					if ((clicker = building.GetDefaultComponent<Clicker>()) != null) {
						clicker.Click(e.Button, e.Buttons, e.Qualifiers);
						return;
					}
				}
			}
			else {
				currentBuilder.OnMouseDown(e);
			}
		}

		void OnMouseMove(MHUrhoMouseMovedEventArgs e)
		{
			if (ui.UIHovering) return;

			currentBuilder?.OnMouseMove(e);
		}

		void OnMouseUp(MouseButtonUpEventArgs e)
		{
			if (ui.UIHovering) return;

			currentBuilder?.OnMouseUp(e);
		}

		void OnMouseWheelMoved(MouseWheelEventArgs e)
		{
			if (ui.UIHovering) return;

			currentBuilder?.OnMouseWheelMoved(e);
		}

		void OnUpdate(float timeStep)
		{
			if (ui.UIHovering) return;

			currentBuilder?.OnUpdate(timeStep);
		}

		void UIHoverBegin()
		{
			currentBuilder?.UIHoverBegin();
		}

		void UIHoverEnd()
		{
			currentBuilder?.UIHoverEnd();
		}
	}
}
