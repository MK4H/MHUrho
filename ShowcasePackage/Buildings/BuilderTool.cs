using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.CameraMovement;
using MHUrho.EditorTools;
using MHUrho.EditorTools.MouseKeyboard;
using MHUrho.Helpers.Extensions;
using MHUrho.Input;
using MHUrho.Input.MouseKeyboard;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.DefaultComponents;
using MHUrho.UserInterface.MouseKeyboard;
using ShowcasePackage.Buildings;
using Urho;
using Urho.Gui;

namespace ShowcasePackage.Buildings
{
	class BuilderTool : Tool, IMouseKeyboardTool {
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
		readonly UIElement uiElem;

		bool enabled;

		Builder currentBuilder;

		public BuilderTool(GameController input, GameUI ui, CameraMover camera, IntRect iconRectangle)
			: base(input, iconRectangle)
		{
			this.input = input;
			this.ui = ui;
			this.builders = new Dictionary<CheckBox, Builder>();
			this.checkBoxes = new ExclusiveCheckBoxes();
			this.enabled = false;

			checkBoxes.SelectedChanged += OnSelectedChanged;

			IEnumerable<string> activeBuilders = Level.EditorMode ? EditBuilders : PlayBuilders;
			foreach (var builder in activeBuilders) {
				BaseBuildingTypePlugin typePlugin = (BaseBuildingTypePlugin)Level.Package.GetBuildingType(builder).Plugin;
				InitCheckbox(typePlugin.GetBuilder(input, ui, camera), input.Level.Package);
			}

			InitCheckbox(new Destroyer(input, ui, camera), input.Level.Package);
			InitUI(ui, out uiElem);
		}

		public override void Dispose()
		{
			Disable();
			foreach (var pair in builders)
			{
				ui.SelectionBar.RemoveChild(pair.Key);
				pair.Value.Dispose();
			}

			checkBoxes.SelectedChanged -= OnSelectedChanged;

			checkBoxes.Dispose();
			uiElem.Dispose();
			builders = null;
		}

		public override void Enable()
		{
			if (enabled) return;

			checkBoxes.Show();
			uiElem.Visible = true;


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
			uiElem.Visible = false;

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

		static void InitUI(GameUI ui, out UIElement uiElem)
		{
			if ((uiElem = ui.CustomWindow.GetChild("BuildingToolUI")) == null)
			{
				ui.CustomWindow.LoadLayout("Assets/UI/BuildingToolUI.xml");
				uiElem = ui.CustomWindow.GetChild("BuildingToolUI");
			}

			uiElem.Visible = false;
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
				uiElem.Visible = false;
				currentBuilder = builders[newSelected];
				currentBuilder.Enable();
			}
			else {
				uiElem.Visible = true;
			}
		}

		void OnMouseDown(MouseButtonDownEventArgs e)
		{
			if (currentBuilder == null) {
				if (ui.UIHovering) return;

				var raycast = input.CursorRaycast();
				foreach (var result in raycast) {
					for (Node current = result.Node; current != Level.LevelNode && current != null; current = current.Parent)
					{
						if (!Level.TryGetBuilding(current, out IBuilding building))
						{
							continue;
						}

						Clicker clicker = null;
						if ((clicker = building.GetDefaultComponent<Clicker>()) != null)
						{
							clicker.Click(e.Button, e.Buttons, e.Qualifiers);
							return;
						}
					}					
				}
			}
			else {
				currentBuilder.OnMouseDown(e);
			}
		}

		void OnMouseMove(MHUrhoMouseMovedEventArgs e)
		{
			currentBuilder?.OnMouseMove(e);
		}

		void OnMouseUp(MouseButtonUpEventArgs e)
		{
			currentBuilder?.OnMouseUp(e);
		}

		void OnMouseWheelMoved(MouseWheelEventArgs e)
		{
			currentBuilder?.OnMouseWheelMoved(e);
		}

		void OnUpdate(float timeStep)
		{
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
