using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.CameraMovement;
using MHUrho.DefaultComponents;
using MHUrho.EditorTools;
using MHUrho.EditorTools.MouseKeyboard;
using MHUrho.Helpers.Extensions;
using MHUrho.Input;
using MHUrho.Input.MouseKeyboard;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.UserInterface.MouseKeyboard;
using ShowcasePackage.Buildings;
using Urho;
using Urho.Gui;

namespace ShowcasePackage.Units
{
	class SpawnerTool : Tool, IMouseKeyboardTool {
		static readonly string[] ActiveSpawners = {ChickenType.TypeName, WolfType.TypeName};

		Dictionary<CheckBox, Spawner> spawners;

		readonly GameController input;
		readonly GameUI ui;

		readonly ExclusiveCheckBoxes checkBoxes;
		readonly UIElement uiElem;
		readonly Text unitName;

		bool enabled;

		Spawner currentSpawner;

		public SpawnerTool(GameController input, GameUI ui, CameraMover camera, IntRect iconRectangle)
			: base(input, iconRectangle)
		{
			this.input = input;
			this.ui = ui;
			this.spawners = new Dictionary<CheckBox, Spawner>();
			this.checkBoxes = new ExclusiveCheckBoxes();
			this.enabled = false;

			checkBoxes.SelectedChanged += OnSelectedChanged;

			foreach (var spawner in ActiveSpawners)
			{
				SpawnableUnitTypePlugin typePlugin = (SpawnableUnitTypePlugin)Level.Package.GetUnitType(spawner).Plugin;
				InitCheckbox(typePlugin.GetSpawner(input, ui, camera), input.Level.Package);
			}

			InitCheckbox(new Deleter(input, ui, camera), input.Level.Package);
			InitUI(ui, out uiElem, out unitName);
		}


		public override void Dispose()
		{
			Disable();
			foreach (var pair in spawners)
			{
				ui.SelectionBar.RemoveChild(pair.Key);
			}

			checkBoxes.SelectedChanged -= OnSelectedChanged;
			checkBoxes.Dispose();
			uiElem.Dispose();
			spawners = null;
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

		static void InitUI(GameUI ui, out UIElement uiElem, out Text unitName)
		{
			if ((uiElem = ui.CustomWindow.GetChild("SpawningToolUI")) == null)
			{
				ui.CustomWindow.LoadLayout("Assets/UI/SpawningToolUI.xml");
				uiElem = ui.CustomWindow.GetChild("SpawningToolUI");
			}

			unitName = (Text)uiElem.GetChild("Name");

			uiElem.Visible = false;
		}

		void InitCheckbox(Spawner spawner, GamePack package)
		{
			var checkBox = ui.SelectionBar.CreateCheckBox();
			checkBox.SetStyle("SelectionBarCheckBox");
			checkBox.Texture = package.BuildingIconTexture;
			checkBox.ImageRect = spawner.UnitType.IconRectangle;
			checkBox.HoverOffset = new IntVector2(spawner.UnitType.IconRectangle.Width(), 0);
			checkBox.CheckedOffset = new IntVector2(2 * spawner.UnitType.IconRectangle.Width(), 0);
			checkBoxes.AddCheckBox(checkBox);
			spawners.Add(checkBox, spawner);
		}

		void OnSelectedChanged(CheckBox newSelected, CheckBox oldSelected)
		{
			currentSpawner?.Disable();
			currentSpawner = null;

			if (newSelected != null)
			{
				currentSpawner = spawners[newSelected];
				currentSpawner.Enable();
				unitName.Value = currentSpawner.UnitType.Name;
			}
			else {
				unitName.Value = "";
			}
		}

		void OnMouseDown(MouseButtonDownEventArgs e)
		{
			currentSpawner?.OnMouseDown(e);
		}

		void OnMouseMove(MHUrhoMouseMovedEventArgs e)
		{
			currentSpawner?.OnMouseMove(e);
		}

		void OnMouseUp(MouseButtonUpEventArgs e)
		{
			currentSpawner?.OnMouseUp(e);
		}

		void OnMouseWheelMoved(MouseWheelEventArgs e)
		{
			currentSpawner?.OnMouseWheelMoved(e);
		}

		void OnUpdate(float timeStep)
		{
			currentSpawner?.OnUpdate(timeStep);
		}

		void UIHoverBegin()
		{
			currentSpawner?.UIHoverBegin();
		}

		void UIHoverEnd()
		{
			currentSpawner?.UIHoverEnd();
		}

	}
}
