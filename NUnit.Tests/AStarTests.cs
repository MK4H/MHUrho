using System;
using System.Collections.Generic;

using NUnit.Framework;

using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.Control;
using MHUrho.WorldMap;
using MHUrho.Helpers;
using Urho;

namespace NUnit.Tests {
	class AStarTests {

		private TestMap allOneSpeed;
		private TestMap randomSpeed;
		private TestMap oneWithCross;
		private TestMap randomWithCross;

		
		class TestMap : IMap {

			class PassableTestTile : ITile {
				#region NOT USED IN TESTS
				public Vector2 Center => throw new NotImplementedException();

				public Vector3 TopLeft3 => throw new NotImplementedException();

				public Vector3 TopRight3 => throw new NotImplementedException();

				public Vector3 BottomLeft3 => throw new NotImplementedException();

				public Vector3 BottomRight3 => throw new NotImplementedException();

				public float TopLeftHeight => throw new NotImplementedException();

				public float TopRightHeight => throw new NotImplementedException();

				public float BottomLeftHeight => throw new NotImplementedException();

				public float BottomRightHeight => throw new NotImplementedException();

				public Map Map => throw new NotImplementedException();

				public void ConnectReferences(ILevelManager level)
				{
					throw new NotImplementedException();
				}

				public void FinishLoading()
				{
					throw new NotImplementedException();
				}

				public void AddUnit(IUnit unit)
				{
					throw new NotImplementedException();
				}

				public void RemoveUnit(IUnit unit)
				{
					throw new NotImplementedException();
				}

				public void AddBuilding(IBuilding building)
				{
					throw new NotImplementedException();
				}

				public void RemoveBuilding(IBuilding building)
				{
					throw new NotImplementedException();
				}

				public StTile Save()
				{
					throw new NotImplementedException();
				}

				public void ChangeType(TileType newType)
				{
					throw new NotImplementedException();
				}

				public void ChangeTopLeftHeight(float heightDelta, bool signalNeighbours = true)
				{
					throw new NotImplementedException();
				}

				public void SetTopLeftHeight(float newHeight, bool signalNeighbours = true)
				{
					throw new NotImplementedException();
				}

				public void CornerHeightChange()
				{
					throw new NotImplementedException();
				}

				public IntRect MapArea => throw new NotImplementedException();

				public IntVector2 TopLeft => throw new NotImplementedException();

				public IntVector2 TopRight => throw new NotImplementedException();

				public IntVector2 BottomLeft => throw new NotImplementedException();

				public IntVector2 BottomRight => throw new NotImplementedException();

				public IReadOnlyList<IUnit> Units => throw new NotImplementedException();

				public IBuilding Building => throw new NotImplementedException();

				public TileType Type => throw new NotImplementedException();
				#endregion

				public Vector3 Center3 => new Vector3(MapLocation.X + 0.5f, 0, MapLocation.Y + 0.5f);

				public IntVector2 MapLocation { get; private set; }

				public float MovementSpeedModifier { get; private set; }

				public PassableTestTile(float speedModifier, int x, int y) {
					this.MovementSpeedModifier = speedModifier;
					MapLocation = new IntVector2(x, y);
				}

				public override string ToString() {
					return $"X: {MapLocation.X}, Y: {MapLocation.Y}";
				}
			}

			class NotPassableTestTile : ITile {

				#region NOT USED IN TESTS
				public Vector2 Center => throw new NotImplementedException();

				public Vector3 TopLeft3 => throw new NotImplementedException();

				public Vector3 TopRight3 => throw new NotImplementedException();

				public Vector3 BottomLeft3 => throw new NotImplementedException();

				public Vector3 BottomRight3 => throw new NotImplementedException();

				public float TopLeftHeight => throw new NotImplementedException();

				public float TopRightHeight => throw new NotImplementedException();

				public float BottomLeftHeight => throw new NotImplementedException();

				public float BottomRightHeight => throw new NotImplementedException();

				public Map Map => throw new NotImplementedException();

				public void ConnectReferences(ILevelManager level)
				{
					throw new NotImplementedException();
				}

				public void FinishLoading()
				{
					throw new NotImplementedException();
				}

				public void AddUnit(IUnit unit)
				{
					throw new NotImplementedException();
				}

				public void RemoveUnit(IUnit unit)
				{
					throw new NotImplementedException();
				}

				public void AddBuilding(IBuilding building)
				{
					throw new NotImplementedException();
				}

				public void RemoveBuilding(IBuilding building)
				{
					throw new NotImplementedException();
				}

				public StTile Save()
				{
					throw new NotImplementedException();
				}

				public void ChangeType(TileType newType)
				{
					throw new NotImplementedException();
				}

				public void ChangeTopLeftHeight(float heightDelta, bool signalNeighbours = true)
				{
					throw new NotImplementedException();
				}

				public void SetTopLeftHeight(float newHeight, bool signalNeighbours = true)
				{
					throw new NotImplementedException();
				}

				public void CornerHeightChange()
				{
					throw new NotImplementedException();
				}


				public IReadOnlyList<IUnit> Units => throw new NotImplementedException();

				public IBuilding Building => throw new NotImplementedException();



				public TileType Type => throw new NotImplementedException();

				public IntRect MapArea => throw new NotImplementedException();

				public IntVector2 TopLeft => throw new NotImplementedException();

				public IntVector2 TopRight => throw new NotImplementedException();

				public IntVector2 BottomLeft => throw new NotImplementedException();

				public IntVector2 BottomRight => throw new NotImplementedException();
				#endregion


				public IntVector2 MapLocation { get; private set; }

				public Vector3 Center3 => new Vector3(MapLocation.X + 0.5f, 0, MapLocation.Y + 0.5f);
				
				//Should not call this if it is not passable
				public float MovementSpeedModifier => throw new NotImplementedException();

				public NotPassableTestTile(int x, int y) {
					MapLocation = new IntVector2(x, y);
				}

				public override string ToString() {
					return $"X: {MapLocation.X}, Y: {MapLocation.Y}";
				}
			}

			ITile[][] map;

			#region NOT USED IN TEST

			public bool IsInside(Vector3 point) {
				throw new NotImplementedException();
			}

			public bool IsXInside(int x) {
				throw new NotImplementedException();
			}

			public bool IsXInside(IntVector2 vector) {
				throw new NotImplementedException();
			}

			public bool IsYInside(int y) {
				throw new NotImplementedException();
			}

			public bool IsYInside(IntVector2 vector) {
				throw new NotImplementedException();
			}

			public int WhereIsX(int x) {
				throw new NotImplementedException();
			}

			public int WhereIsX(IntVector2 vector) {
				throw new NotImplementedException();
			}

			public int WhereIsY(int y) {
				throw new NotImplementedException();
			}

			public int WhereIsY(IntVector2 vector) {
				throw new NotImplementedException();
			}





			public ITile GetTileByTopLeftCorner(int x, int y) {
				throw new NotImplementedException();
			}

			public ITile GetTileByTopLeftCorner(IntVector2 topLeftCorner) {
				throw new NotImplementedException();
			}

			public ITile GetTileByTopRightCorner(int x, int y) {
				throw new NotImplementedException();
			}

			public ITile GetTileByTopRightCorner(IntVector2 topRightCorner) {
				throw new NotImplementedException();
			}

			public ITile GetTileByBottomLeftCorner(int x, int y) {
				throw new NotImplementedException();
			}

			public ITile GetTileByBottomLeftCorner(IntVector2 bottomLeftCorner) {
				throw new NotImplementedException();
			}

			public ITile GetTileByBottomRightCorner(int x, int y) {
				throw new NotImplementedException();
			}

			public ITile GetTileByBottomRightCorner(IntVector2 bottomRightCorner) {
				throw new NotImplementedException();
			}

			public ITile GetContainingTile(Vector3 point) {
				throw new NotImplementedException();
			}



			public bool SnapToMap(ref IntVector2 topLeft, ref IntVector2 bottomRight) {
				throw new NotImplementedException();
			}

			public void SquishToMap(ref IntVector2 topLeft, ref IntVector2 bottomRight) {
				throw new NotImplementedException();
			}

			public ITile FindClosestEmptyTile(ITile closestTo) {
				throw new NotImplementedException();
			}

			public float GetHeightAt(int x, int y) {
				throw new NotImplementedException();
			}

			public float GetHeightAt(IntVector2 position) {
				throw new NotImplementedException();
			}

			public float GetHeightAt(float x, float y) {
				throw new NotImplementedException();
			}

			

			public StMap Save() {
				throw new NotImplementedException();
			}

			public void HighlightArea(ITile center, IntVector2 size, HighlightMode mode, Color color) {
				throw new NotImplementedException();
			}

			public void HighlightArea(IntVector2 topLeft, IntVector2 bottomRight, HighlightMode mode, Color color) {
				throw new NotImplementedException();
			}

			public void HighlightArea(ITile center, IntVector2 size) {
				throw new NotImplementedException();
			}

			public void DisableHighlight() {
				throw new NotImplementedException();
			}

			#endregion

			

			public IntVector2 TopLeft { get; private set; }


			public IntVector2 BottomRight { get; private set; }

			public IntVector2 TopRight => new IntVector2(Right, Top);

			public IntVector2 BottomLeft => new IntVector2(Left, Bottom);

			public int Width => Right + 1;

			public int Length => Bottom + 1;

			public int Left => TopLeft.X;

			public int Right => BottomRight.X;

			public int Top => TopLeft.Y;

			public int Bottom => BottomRight.Y;
			public bool IsInside(int x, int y) {
				throw new NotImplementedException();
			}

			public bool IsInside(float x, float y) {
				throw new NotImplementedException();
			}

			public bool IsInside(IntVector2 point) {
				return point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
			}

			public bool IsInside(Vector2 point) {
				throw new NotImplementedException();
			}

			public ITile GetContainingTile(Vector2 point) {
				int topLeftX = (int)Math.Floor(point.X);
				int topLeftZ = (int)Math.Floor(point.Y);
				return GetTileByMapLocation(topLeftX, topLeftZ);
			}

			public ITile GetTileByMapLocation(int x, int y) {
				return map[x][y];
			}

			public ITile GetTileByMapLocation(IntVector2 mapLocation) {
				return GetTileByMapLocation(mapLocation.X, mapLocation.Y);
			}

			public float GetHeightAt(Vector2 position)
			{
				return 0;
			}

			public static TestMap GetTestMapRandomSpeeds(int width, int height, Random rnd) {
				ITile[][] map = new ITile[width][];
				for (int x = 0; x < width; x++) {
					map[x] = new ITile[height];
					for (int y = 0; y < height; y++) {
						map[x][y] = new PassableTestTile((float) rnd.NextDouble(), x, y);
					}
				}

				return new TestMap(width, height, map);
			}

			public static TestMap GetTestMapStaticSpeed(int width, int height, float speed = 1) {
				ITile[][] map = new ITile[width][];
				for (int x = 0; x < width; x++) {
					map[x] = new ITile[height];
					for (int y = 0; y < height; y++) {
						map[x][y] = new PassableTestTile(speed, x, y);
					}
				}

				return new TestMap(width, height, map);
			}

			public static TestMap GetTestMapWithImpassableCrossAndStaticSpeed(int width, int height, float speed) {
				ITile[][] map = new ITile[width][];
				for (int x = 0; x < width; x++) {
					map[x] = new ITile[height];
					for (int y = 0; y < height; y++) {
						if ((x == width / 2) || (y == height / 2)) {
							map[x][y] = new NotPassableTestTile(x, y);
						}
						else {
							map[x][y] = new PassableTestTile(speed, x, y);
						}

					}
				}

				return new TestMap(width, height, map);
			}

			public static TestMap GetTestMapWithImpassableCrossAndRandomSpeed(int width, int height, Random rnd) {
				ITile[][] map = new ITile[width][];
				for (int x = 0; x < width; x++) {
					map[x] = new ITile[height];
					for (int y = 0; y < height; y++) {
						if ((x == width / 2) || (y == height / 2)) {
							map[x][y] = new NotPassableTestTile(x, y);
						}
						else {
							map[x][y] = new PassableTestTile((float)rnd.NextDouble(), x, y);
						}

					}
				}

				return new TestMap(width, height, map);
			}

			public static TestMap GetTestMapWithImpassableTiles(int width, int height, Random rnd, List<IntVector2> impassableTiles) {
				ITile[][] map = new ITile[width][];
				for (int x = 0; x < width; x++) {
					map[x] = new ITile[height];
					for (int y = 0; y < height; y++) {
						if (impassableTiles.Contains(new IntVector2(x, y))) {
							map[x][y] = new NotPassableTestTile(x, y);
						}
						else {
							map[x][y] = new PassableTestTile((float)rnd.NextDouble(), x, y);
						}

					}
				}

				return new TestMap(width, height, map);
			}

			public static TestMap GetTestMapWithImpassableTiles(int width, int height, int speed, List<IntVector2> impassableTiles) {
				ITile[][] map = new ITile[width][];
				for (int x = 0; x < width; x++) {
					map[x] = new ITile[height];
					for (int y = 0; y < height; y++) {
						if (impassableTiles.Contains(new IntVector2(x, y))) {
							map[x][y] = new NotPassableTestTile(x,y);
						}
						else {
							map[x][y] = new PassableTestTile(speed, x, y);
						}

					}
				}

				return new TestMap(width, height, map);
			}

			private TestMap(int width, int height, ITile[][] map) {
				this.map = map;
				TopLeft = new IntVector2(0, 0);
				BottomRight = new IntVector2(width - 1, height - 1);
			}
		}

		[OneTimeSetUp]
		public void TestSetup() {
			allOneSpeed = TestMap.GetTestMapStaticSpeed(50, 50);
			randomSpeed = TestMap.GetTestMapRandomSpeeds(50, 50, new Random());
			oneWithCross = TestMap.GetTestMapWithImpassableCrossAndStaticSpeed(50, 50, 1);
			randomWithCross = TestMap.GetTestMapWithImpassableCrossAndRandomSpeed(50, 50, new Random());

		}

		[Test]

		public void StraightCornerToCornerAllSpeedsOneGetTileList() {
			var aStar = new AStar(allOneSpeed);

			//Top
			
			List<ITile> path = aStar.GetTileList(allOneSpeed.TopLeft.ToVector2(), allOneSpeed.GetTileByMapLocation(allOneSpeed.TopRight), (tile1, tile2) => true, (tile) => 1);
			List<ITile> expected = new List<ITile>();
			for (int i = 0; i <= allOneSpeed.Right; i++) {
				expected.Add(allOneSpeed.GetTileByMapLocation(i,0));
			}
			CollectionAssert.AreEqual(expected, path,"Fail going from topLeft to the topRight");

			path = aStar.GetTileList(allOneSpeed.TopLeft.ToVector2(), allOneSpeed.GetTileByMapLocation(allOneSpeed.BottomLeft), (tile1, tile2) => true, (tile) => 1);
			expected.Clear();
			for (int i = 0; i <= allOneSpeed.Right; i++) {
				expected.Add(allOneSpeed.GetTileByMapLocation(0, i));
			}
			CollectionAssert.AreEqual(expected, path, "Fail going from topLeft to the bottomLeft");


			path = aStar.GetTileList(allOneSpeed.BottomRight.ToVector2(), allOneSpeed.GetTileByMapLocation(allOneSpeed.BottomLeft), (tile1, tile2) => true, (tile) => 1);
			expected.Clear();
			for (int i = allOneSpeed.Right; i >= allOneSpeed.Left; i--) {
				expected.Add(allOneSpeed.GetTileByMapLocation(i, allOneSpeed.Bottom));
			}
			CollectionAssert.AreEqual(expected, path, "Fail going from bottomRight to the bottomLeft");

			path = aStar.GetTileList(allOneSpeed.BottomRight.ToVector2(), allOneSpeed.GetTileByMapLocation(allOneSpeed.TopRight), (tile1, tile2) => true, (tile) => 1);
			expected.Clear();
			for (int i = allOneSpeed.Bottom; i >= allOneSpeed.Top; i--) {
				expected.Add(allOneSpeed.GetTileByMapLocation(allOneSpeed.Right, i));
			}
			CollectionAssert.AreEqual(expected, path, "Fail going from bottomRight to the topRight");
			Assert.Pass();
		}


		[Test]

		public void StraightCornerToCornerAllSpeedsOneGetPath() {
			var aStar = new AStar(allOneSpeed);

			//Top

			Path path = aStar.FindPath(new Vector2(0.5f, 0.5f), allOneSpeed.GetTileByMapLocation(allOneSpeed.TopRight), (tile1, tile2) => true, (tile) => 1);
			var pathEnumerator = path.GetEnumerator();
			Vector3 position = new Vector3(0.5f, 0, 0.5f);
			for (int i = 0; i < allOneSpeed.Right * 2 + 1; i++) {
				pathEnumerator.MoveNext();
				Assert.That(pathEnumerator.Current.Position.IsNear(position, 10E-9f));
				position.X += 0.5f;
			}

			Assert.That(!pathEnumerator.MoveNext());

			path = aStar.FindPath(new Vector2(0.5f, 0.5f), allOneSpeed.GetTileByMapLocation(allOneSpeed.BottomLeft), (tile1, tile2) => true, (tile) => 1);
			pathEnumerator = path.GetEnumerator();
			position = new Vector3(0.5f, 0, 0.5f);
			for (int i = 0; i < allOneSpeed.Bottom * 2 + 1; i++) {
				pathEnumerator.MoveNext();
				Assert.That(pathEnumerator.Current.Position.IsNear(position, 10E-9f));
				position.Z += 0.5f;
			}

			Assert.That(!pathEnumerator.MoveNext());


			path = aStar.FindPath(allOneSpeed.BottomRight.ToVector2() + new Vector2(0.5f, 0.5f), allOneSpeed.GetTileByMapLocation(allOneSpeed.BottomLeft), (tile1, tile2) => true, (tile) => 1);
			pathEnumerator = path.GetEnumerator();
			position = new Vector3( allOneSpeed.Right + 0.5f, 0, allOneSpeed.Bottom + 0.5f);
			for (int i = 0; i < allOneSpeed.Right * 2 + 1; i++) {
				pathEnumerator.MoveNext();
				Assert.That(pathEnumerator.Current.Position.IsNear(position, 10E-9f));
				position.X -= 0.5f;
			}

			Assert.That(!pathEnumerator.MoveNext());

			path = aStar.FindPath(allOneSpeed.BottomRight.ToVector2() + new Vector2(0.5f, 0.5f), allOneSpeed.GetTileByMapLocation(allOneSpeed.TopRight), (tile1, tile2) => true, (tile) => 1);
			pathEnumerator = path.GetEnumerator();
			position = new Vector3(allOneSpeed.Right + 0.5f, 0, allOneSpeed.Bottom + 0.5f);
			for (int i = 0; i < allOneSpeed.Bottom * 2 + 1; i++) {
				pathEnumerator.MoveNext();
				Assert.That(pathEnumerator.Current.Position.IsNear(position, 10E-9f));
				position.Z -= 0.5f;
			}

			Assert.That(!pathEnumerator.MoveNext());
			Assert.Pass();
		}
		//[Test]
		//public void DiagonalCornerToCornerInSquare() {





		//}

		//[Test]
		//public void DiagonalCornerToCornerInRectangle() {





		//}


		[Test]

		public void StartIsFinishPath() {
			var aStar = new AStar(allOneSpeed);

			Path path = aStar.FindPath(new Vector2(10.5f, 10.5f), allOneSpeed.GetTileByMapLocation(new IntVector2(10, 10)), (tile1, tile2) => true, (tile) => 1);

			Assert.IsNotNull(path);
			var enumerator = path.GetEnumerator();
			Assert.That(enumerator.MoveNext());
			//Current position
			Assert.That(enumerator.Current.Position.IsNear(new Vector3(10.5f, 0, 10.5f), 10E-9f));
			//Target position
			Assert.That(enumerator.MoveNext());
			Assert.That(enumerator.Current.Position.IsNear(new Vector3(10.5f, 0, 10.5f), 10E-9f));
			Assert.That(!enumerator.MoveNext());
			Assert.Pass();


		}

		[Test]

		public void StartIsFinishTileList() {
			var aStar = new AStar(allOneSpeed);

			var path = aStar.GetTileList(new Vector2(10, 10), allOneSpeed.GetTileByMapLocation(new IntVector2(10, 10)), (tile1, tile2) => true, (tile) => 1);

			Assert.IsNotNull(path);
			CollectionAssert.AreEqual(new List<ITile> {allOneSpeed.GetTileByMapLocation(new IntVector2(10, 10))}, path);
			Assert.Pass();


		}
	}
}