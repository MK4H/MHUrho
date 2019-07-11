using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.CameraMovement;
using MHUrho.Helpers.Extensions;
using MHUrho.Input;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.UserInterface.MandK;
using ShowcasePackage.Misc;
using Urho;
using Urho.Gui;

namespace ShowcasePackage.Buildings
{
	class LineBuilder : Builder {
		public Color AbleColor { get; set; } = Color.Green;
		public Color UnableColor { get; set; } = Color.Red;


		readonly GameController input;
		readonly GameUI ui;
		readonly CameraMover camera;
		readonly Cost cost;

		List<ITile> line;

		public LineBuilder(GameController input,
							GameUI ui,
							CameraMover camera, 
							BuildingType type,
							Cost cost)
			: base(input.Level, type)
		{
			this.input = input;
			this.ui = ui;
			this.camera = camera;
			this.cost = cost;

			line = null;
		}

		public override void Disable()
		{
			line = null;
			Level.Map.DisableHighlight();
		}

		public override void OnMouseDown(MouseButtonDownEventArgs e)
		{
			if (ui.UIHovering) {
				return;
			}

			if (e.Button == (int)MouseButton.Left) {
				ITile startTile = input.GetTileUnderCursor();
				if (startTile != null) {
					line = new List<ITile>{startTile};
				}
			}
		}

		public override void OnMouseUp(MouseButtonUpEventArgs e)
		{
			if (e.Button != (int) MouseButton.Left || line == null) {
				return;
			}

			if (ui.UIHovering) {
				ClearSelection();
				return;
			}

			ITile endTile = input.GetTileUnderCursor();
			if (endTile == null) {
				ClearSelection();
				return;
			}

			if (endTile != line[line.Count - 1]) {
				line = GetLine(line[0], endTile);
			}

			if (cost.HasResources(input.Player, line.Count) &&
				line.All((tile) => BuildingType.CanBuild(GetBuildingRectangle(tile, BuildingType).TopLeft(), input.Player ,Level))) {
				foreach (var tile in line) {
					IBuilding newBuilding = Level.BuildBuilding(BuildingType,
																GetBuildingRectangle(tile, BuildingType).TopLeft(),
																Quaternion.Identity,
																input.Player);
					if (newBuilding == null) {
						break;
					}
					else {
						cost.TakeFrom(input.Player);
					}
				}
			}

			ClearSelection();
		}

		public override void OnMouseMove(MHUrhoMouseMovedEventArgs e)
		{
			if (line == null) {
				ITile tile = input.GetTileUnderCursor();
				if (tile == null) {
					return;
				}
				Color color = BuildingType.CanBuild(GetBuildingRectangle(tile, BuildingType).TopLeft(),
													input.Player,
													Level) && 
							cost.HasResources(input.Player)
								? AbleColor
								: UnableColor;
				Level.Map.HighlightTileList(new []{tile}, color);
				return;
			}

			UpdateHighlight();
		}

		public override void OnUpdate(float timeStep)
		{
			if (line == null) {
				return;
			}

			UpdateHighlight();
		}

		List<ITile> GetLine(ITile start, ITile finish)
		{
			List<ITile> newLine = new List<ITile> {start};
			IntVector2 position = start.TopLeft;
			int xStep = Math.Sign(finish.TopLeft.X - position.X);
			int yStep = Math.Sign(finish.TopLeft.Y - position.Y);
			while (position != finish.TopLeft) {
				if (position.X != finish.TopLeft.X) {
					position.X += xStep;
				}

				if (position.Y != finish.TopLeft.Y) {
					position.Y += yStep;
				}

				newLine.Add(Level.Map.GetTileByTopLeftCorner(position));
			}

			return newLine;
		}

		void UpdateHighlight()
		{
			ITile endTile = input.GetTileUnderCursor();
			if (endTile == null)
			{
				Level.Map.DisableHighlight();
			}

			if (endTile != line[line.Count - 1])
			{
				line = GetLine(line[0], endTile);
			}

			Level.Map.HighlightTileList(line,
										line.All((tile) =>
													BuildingType.CanBuild(GetBuildingRectangle(tile, BuildingType).TopLeft(),
																			input.Player,
																			Level)) &&
										cost.HasResources(input.Player, line.Count)
											? AbleColor
											: UnableColor);
		}

		void ClearSelection()
		{
			Level.Map.DisableHighlight();
			line = null;
		}
	}
}
