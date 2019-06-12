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
using Urho;

namespace ShowcasePackage.Buildings
{
	class LineBuilder : Builder {
		public Color AbleColor { get; set; } = Color.Green;
		public Color UnableColor { get; set; } = Color.Red;


		readonly GameController input;
		readonly GameUI ui;
		readonly CameraMover camera;

		List<ITile> line;

		public LineBuilder(GameController input,
							GameUI ui,
							CameraMover camera, 
							BuildingType type)
			: base(input.Level, type)
		{
			this.input = input;
			this.ui = ui;
			this.camera = camera;

			line = null;
		}

		public override void Disable()
		{
			line = null;
			Level.Map.DisableHighlight();
		}

		public override void OnMouseDown(MouseButtonDownEventArgs e)
		{
			if (e.Button == (int)MouseButton.Left) {
				ITile startTile = input.GetTileUnderCursor();
				if (startTile != null) {
					line = new List<ITile>{startTile};
				}
			}
		}

		public override void OnMouseUp(MouseButtonUpEventArgs e)
		{
			if (e.Button == (int)MouseButton.Left) {
				ITile endTile = input.GetTileUnderCursor();
				if (endTile == null) {
					line = null;
					return;
				}

				if (endTile != line[line.Count - 1]) {
					line = GetLine(line[0], endTile);
				}

				if (line.All((tile) => BuildingType.CanBuild(GetBuildingRectangle(tile, BuildingType), input.Player ,Level))) {
					foreach (var tile in line) {
						Level.BuildBuilding(BuildingType,
											GetBuildingRectangle(tile, BuildingType).TopLeft(),
											Quaternion.Identity,
											input.Player);
					}
				}
				line = null;
			}
		}

		public override void OnMouseMove(MHUrhoMouseMovedEventArgs e)
		{
			UpdateHighlight();
		}

		public override void OnUpdate(float timeStep)
		{
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
													BuildingType.CanBuild(GetBuildingRectangle(tile, BuildingType),
																			input.Player,
																			Level))
											? AbleColor
											: UnableColor);
		}
	}
}
