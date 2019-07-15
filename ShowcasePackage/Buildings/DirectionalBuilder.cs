using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.CameraMovement;
using MHUrho.Input.MouseKeyboard;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.UserInterface.MouseKeyboard;
using MHUrho.WorldMap;
using MHUrho.Helpers.Extensions;
using MHUrho.Input;
using ShowcasePackage.Misc;
using Urho;
using Urho.Gui;

namespace ShowcasePackage.Buildings
{
	enum Sides { Front = 0, Right = 1, Back = 2, Left = 3, Center = 4 }
	enum Direction { PlusX = 0, PlusZ = 1, MinusX = 2, MinusZ = 3 }

	class DirectionalBuilder : Builder
	{
		/// <summary>
		/// Indexed by current orientation. So when facing PlusX, the top of the rectangle is right side.
		/// </summary>
		static readonly int[] TopColorMapping =
		{
			(int) Sides.Right, (int) Sides.Back, (int) Sides.Left, (int) Sides.Front
		};

		static readonly int[] BottomColorMapping =
		{
			(int) Sides.Left, (int) Sides.Front, (int) Sides.Right, (int) Sides.Back
		};

		static readonly int[] RightColorMapping =
		{
			(int) Sides.Front, (int) Sides.Right, (int) Sides.Back, (int) Sides.Left
		};

		static readonly int[] LeftColorMapping =
		{
			(int) Sides.Back, (int) Sides.Left, (int) Sides.Front, (int) Sides.Right
		};

		static readonly int[] CenterColorMapping =
		{
			(int) Sides.Center, (int) Sides.Center, (int) Sides.Center, (int) Sides.Center
		};

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

		readonly Cost cost;

		ITile lockedTile;

		public DirectionalBuilder(GameController input, 
								GameUI ui, 
								CameraMover camera,
								BuildingType buildingType,
								Cost cost)
			:base(input.Level, buildingType)
		{
			this.input = input;
			this.ui = ui;
			this.camera = camera;
			this.cost = cost;
			Direction = Direction.PlusX;
			ableColors = new[] {Color.Green, Color.Green, Color.Green, Color.Green, Color.Green };
			unableColors = new[] {Color.Red, Color.Red, Color.Red, Color.Red, Color.Red};
			lockedTile = null;
		}

		public void SetColor(Sides side, bool ableToBuild, Color color)
		{
			Color[] colors = ableToBuild ? ableColors : unableColors;
			colors[(int) side] = color;
		}

		public Color GetColor(Sides side, bool ableToBuild)
		{
			//Colors are in the order front, right, back, left, center
			Color[] colors = ableToBuild ? ableColors : unableColors;
			return colors[(int)side];
		}

		void HighlightBuildingRectangle()
		{
			if (ui.UIHovering && lockedTile == null) {
				return;
			}

			ITile centerTile = lockedTile ?? input.GetTileUnderCursor();
			if (centerTile == null) {
				return;
			}

			IntRect rect = GetBuildingRectangle(centerTile, BuildingType);

			bool ableToBuild = BuildingType.CanBuild(rect.TopLeft(), input.Player, Level) && cost.HasResources(input.Player);
			Color[] colors = ableToBuild ? ableColors : unableColors;
			Map.HighlightRectangle(rect, (cTile) => GetPositionColor(rect, cTile.TopLeft, colors));
		}

		/// <summary>
		/// Returns color for the <paramref name="currentPosition"/> based on
		/// the position inside the <paramref name="rectangle"/> and current <see cref="Direction"/>
		/// </summary>
		/// <param name="rectangle">The colored rectangle.</param>
		/// <param name="currentPosition">Current position inside the <paramref name="rectangle"/>.</param>
		/// <param name="colors">The set of colors to use.</param>
		/// <returns>Color for the position <paramref name="currentPosition"/> inside the <paramref name="rectangle"/>.</returns>
		Color GetPositionColor(IntRect rectangle, IntVector2 currentPosition, Color[] colors)
		{
			//Calculates the position as if the rectangle was in the MinusZ rotation
			// then it gets translated by GetColor function according to current rotation

			bool right = currentPosition.X == rectangle.Right;
			bool left = currentPosition.X == rectangle.Left;
			bool top = currentPosition.Y == rectangle.Top;
			bool bottom = currentPosition.Y == rectangle.Bottom;

			bool side = right || left || top || bottom;
			bool corner = (right || left) && (top || bottom);
			if (!side || corner) {
				return GetColor(colors, CenterColorMapping);
			}
			else if (right) {
				return GetColor(colors, RightColorMapping);
			}
			else if (left) {
				return GetColor(colors, LeftColorMapping);
			}
			else if (top) {
				return GetColor(colors, TopColorMapping);
			}
			else {
				return GetColor(colors, BottomColorMapping);
			}
		}


		public override void Disable()
		{
			lockedTile = null;
			Level.Map.DisableHighlight();
		}

		public override void OnMouseDown(MouseButtonDownEventArgs e)
		{
			if (ui.UIHovering){
				return;
			}

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
			if (ui.UIHovering) {
				return;
			}

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
			if (ui.UIHovering) {
				return;
			}

			HighlightBuildingRectangle();
		}

		public override void UIHoverBegin()
		{
			if (lockedTile == null) {
				Level.Map.DisableHighlight();
			}
		}

		void BuildBuilding()
		{
			var tile = input.GetTileUnderCursor();
			if (tile == null) return;

			IntRect rect = GetBuildingRectangle(tile, BuildingType);

			if (BuildingType.CanBuild(rect.TopLeft(), input.Player, Level)) {
				Vector3 facing;
				switch (Direction) {
					case Direction.PlusX:
						facing = Vector3.UnitX;
						break;
					case Direction.PlusZ:
						facing = Vector3.UnitZ;
						break;
					case Direction.MinusX:
						facing = -Vector3.UnitX;
						break;
					case Direction.MinusZ:
						facing = -Vector3.UnitZ;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				var building = Level.BuildBuilding(BuildingType, rect.TopLeft(), Quaternion.FromRotationTo(Vector3.UnitZ, facing), input.Player);
				if (building != null) {
					cost.TakeFrom(input.Player);
				}
			}
		}

		Color GetColor(Color[] colors, int[] colorMapping)
		{
			return colors[colorMapping[(int)Direction]];
		}
	}
}
