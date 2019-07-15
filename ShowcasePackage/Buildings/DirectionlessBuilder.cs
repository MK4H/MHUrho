using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.CameraMovement;
using MHUrho.Helpers.Extensions;
using MHUrho.Input;
using MHUrho.Input.MouseKeyboard;
using MHUrho.Logic;
using MHUrho.UserInterface.MouseKeyboard;
using ShowcasePackage.Misc;
using Urho;
using Urho.Gui;

namespace ShowcasePackage.Buildings
{
	class DirectionlessBuilder : Builder {
		public Color AbleColor { get; set; } = Color.Green;
		public Color UnableColor { get; set; } = Color.Red;

		protected readonly GameController Input;
		protected readonly GameUI Ui;
		protected readonly CameraMover Camera;

		readonly Cost cost;

		public DirectionlessBuilder(GameController input, GameUI ui, CameraMover camera, BuildingType type, Cost cost)
			: base(input.Level, type)
		{
			this.Input = input;
			this.Ui = ui;
			this.Camera = camera;
			this.cost = cost;
		}


		public override void Disable()
		{
			Level.Map.DisableHighlight();
		}

		public override void UIHoverBegin()
		{
			Level.Map.DisableHighlight();
		}

		public override void OnMouseDown(MouseButtonDownEventArgs e)
		{
			if (Ui.UIHovering) {
				return;
			}

			if (e.Button != (int) MouseButton.Left) {
				return;
			}

			ITile tile = Input.GetTileUnderCursor();
			if (tile == null) {
				return;
			}

			IntRect rect = GetBuildingRectangle(tile, BuildingType);
			if (BuildingType.CanBuild(rect.TopLeft(), Input.Player, Level) && cost.HasResources(Input.Player)) {
				if (Level.BuildBuilding(BuildingType, rect.TopLeft(), Quaternion.Identity, Input.Player) != null) {
					cost.TakeFrom(Input.Player);
				}
			}
		}

		public override void OnMouseMove(MHUrhoMouseMovedEventArgs e)
		{
			HighlightBuildingRectangle();
		}

		public override void OnUpdate(float timeStep)
		{
			HighlightBuildingRectangle();
		}

		protected void HighlightBuildingRectangle()
		{
			if (Ui.UIHovering)
			{
				return;
			}

			ITile tile = Input.GetTileUnderCursor();

			if (tile == null) {
				return;
			}

			IntRect rect = GetBuildingRectangle(tile, BuildingType);
			Color color = BuildingType.CanBuild(rect.TopLeft(), Input.Player, Level) && cost.HasResources(Input.Player) ? AbleColor : UnableColor;
			Map.HighlightRectangle(rect, color);
		}
	}
}
