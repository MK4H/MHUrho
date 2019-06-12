using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.CameraMovement;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.UserInterface.MandK;
using MHUrho.WorldMap;
using MHUrho.Helpers.Extensions;
using MHUrho.Input;
using Urho;
using Urho.Gui;

namespace ShowcasePackage.Buildings
{
	enum Sides { Front = 0, Right = 1, Back = 2, Left = 3, Center = 4 }
	enum Direction { PlusX = 0, PlusZ = 1, MinusX = 2, MinusZ = 3 }

	class DirectionalBuilder : Builder
	{

		static readonly int[][] Mapping = {new []{0, 1, 2, 3, 4}, new []{1, 2, 3, 0, 4}, new []{2, 3, 0, 1, 4}, new []{3, 0, 1, 2, 4}};

		public Direction Direction { get; set; }

		public Color AbleFront {
			get => GetColor(Sides.Front, true);
			set => SetColor(Sides.Front, true, value);
		}

		public Color AbleBack {
			get => GetColor(Sides.Back, true);
			set => SetColor(Sides.Back, true, value);
		}

		public Color AbleLeft {
			get => GetColor(Sides.Left, true);
			set => SetColor(Sides.Left, true, value);
		}

		public Color AbleRight {
			get => GetColor(Sides.Right, true);
			set => SetColor(Sides.Right, true, value);
		}

		public Color AbleCenter {
			get => GetColor(Sides.Center, true);
			set => SetColor(Sides.Center, true, value);
		}

		public Color UnableFront {
			get => GetColor(Sides.Front, false);
			set => SetColor(Sides.Front, false, value);
		}

		public Color UnableBack {
			get => GetColor(Sides.Back, false);
			set => SetColor(Sides.Back, false, value);
		}

		public Color UnableLeft {
			get => GetColor(Sides.Left, false);
			set => SetColor(Sides.Left, false, value);
		}

		public Color UnableRight {
			get => GetColor(Sides.Right, false);
			set => SetColor(Sides.Right, false, value);
		}

		public Color UnableCenter {
			get => GetColor(Sides.Center, false);
			set => SetColor(Sides.Center, false, value);
		}

		readonly GameController input;
		readonly GameUI ui;
		readonly CameraMover camera;

		readonly Color[] ableColors;
		readonly Color[] unableColors;

		ITile lockedTile;

		public DirectionalBuilder(GameController input, 
								GameUI ui, 
								CameraMover camera,
								BuildingType buildingType)
			:base(input.Level, buildingType)
		{
			this.input = input;
			this.ui = ui;
			this.camera = camera;
			Direction = Direction.PlusX;
			ableColors = new[] {Color.Green, Color.Green, Color.Green, Color.Green, Color.Green };
			unableColors = new[] {Color.Red, Color.Red, Color.Red, Color.Red, Color.Red};
			lockedTile = null;
		}

		public void SetColor(Sides side, bool ableToBuild, Color color)
		{
			ableColors[Mapping[(int)Direction][(int) side]] = color;
		}

		public Color GetColor(Sides side, bool ableToBuild)
		{
			Color[] colors = ableToBuild ? ableColors : unableColors;
			return colors[Mapping[(int)Direction][(int)side]];
		}

		void HighlightBuildingRectangle()
		{
			ITile centerTile = lockedTile ?? input.GetTileUnderCursor();
			if (centerTile == null) {
				return;
			}

			IntRect rect = GetBuildingRectangle(centerTile, BuildingType);

			bool ableToBuild = BuildingType.CanBuild(rect.TopLeft(), rect.BottomRight(), input.Player, Level);
			Map.HighlightRectangle(rect, (cTile) => GetPositionColor(rect, cTile.TopLeft, ableToBuild));
		}

		Color GetPositionColor(IntRect rectangle, IntVector2 currentPosition, bool ableToBuild)
		{
			if (currentPosition.X == rectangle.Right)
			{
				return GetColor(Sides.Front, ableToBuild);
			}
			else if (currentPosition.X == rectangle.Left)
			{
				return GetColor(Sides.Right, ableToBuild);
			}
			else if (currentPosition.Y == rectangle.Top)
			{
				return GetColor(Sides.Back, ableToBuild);
			}
			else if (currentPosition.Y == rectangle.Bottom)
			{
				return GetColor(Sides.Left, ableToBuild);
			}
			else
			{
				return GetColor(Sides.Center, ableToBuild);
			}
		}

		public override void Enable()
		{
			
		}

		public override void Disable()
		{
			lockedTile = null;
			Level.Map.DisableHighlight();
		}

		public override void OnMouseDown(MouseButtonDownEventArgs e)
		{
			if (e.Button == (int) MouseButton.Left) {
				BuildBuilding();
			}
			else if (e.Button == (int) MouseButton.Right) {
				lockedTile = input.GetTileUnderCursor();
			}
			
		}

		public override void OnMouseUp(MouseButtonUpEventArgs e)
		{
			if (e.Button == (int) MouseButton.Right) {
				lockedTile = null;
			}
		}

		public override void OnMouseMove(MHUrhoMouseMovedEventArgs e)
		{
			if (lockedTile != null) {
				ITile tile = input.GetTileUnderCursor();
				if (tile != lockedTile) {
					int xDiff = tile.TopLeft.X - lockedTile.TopLeft.X;
					int yDiff = tile.TopLeft.Y - lockedTile.TopLeft.Y;
					if (Math.Abs(xDiff) >= Math.Abs(yDiff)) {
						Direction = xDiff > 0 ? Direction.PlusX : Direction.MinusX;
					}
					else {
						Direction = yDiff > 0 ? Direction.PlusZ : Direction.MinusZ;
					}
				}
			}

			HighlightBuildingRectangle();
		}

		public override void OnUpdate(float timeStep)
		{
			HighlightBuildingRectangle();
		}

		public override void UIHoverBegin()
		{
			
		}

		void BuildBuilding()
		{
			var tile = input.GetTileUnderCursor();
			if (tile == null) return;

			IntRect rect = GetBuildingRectangle(tile, BuildingType);

			if (BuildingType.CanBuild(rect.TopLeft(), rect.BottomRight(), input.Player, Level))
			{
				float angle = 90 * (int)Direction;
				Level.BuildBuilding(BuildingType, rect.TopLeft(), Quaternion.FromAxisAngle(Vector3.UnitY, angle), input.Player);
			}
		}
	}
}
