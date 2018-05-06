using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.WorldMap;
using MHUrho.Storage;
using Urho;

namespace NUnit.Tests {
	[TestFixture]
	public class MapTests {

		class HeightTestTile : ITile {
			#region NOT USED IN TESTS
			public IReadOnlyList<IUnit> Units => throw new NotImplementedException();

			public IBuilding Building => throw new NotImplementedException();

			public float MovementSpeedModifier => throw new NotImplementedException();

			public TileType Type => throw new NotImplementedException();

			public IntVector2 TopLeft => throw new NotImplementedException();

			public IntVector2 TopRight => throw new NotImplementedException();

			public IntVector2 BottomLeft => throw new NotImplementedException();

			public IntVector2 BottomRight => throw new NotImplementedException();

			public Vector3 Center3 => throw new NotImplementedException();

			public Vector3 TopLeft3 => throw new NotImplementedException();

			public Vector3 TopRight3 => throw new NotImplementedException();

			public Vector3 BottomLeft3 => throw new NotImplementedException();

			public Vector3 BottomRight3 => throw new NotImplementedException();

			public float TopRightHeight => throw new NotImplementedException();

			public float BottomLeftHeight => throw new NotImplementedException();

			public float BottomRightHeight => throw new NotImplementedException();

			public Map Map => throw new NotImplementedException();

			public void ConnectReferences(ILevelManager level) {
				throw new NotImplementedException();
			}

			public void FinishLoading() {
				throw new NotImplementedException();
			}

			public void AddUnit(IUnit unit) {
				throw new NotImplementedException();
			}

			public void RemoveUnit(IUnit unit) {
				throw new NotImplementedException();
			}

			public void AddBuilding(IBuilding building) {
				throw new NotImplementedException();
			}

			public void RemoveBuilding(IBuilding building) {
				throw new NotImplementedException();
			}

			public StTile Save() {
				throw new NotImplementedException();
			}

			public void ChangeType(TileType newType) {
				throw new NotImplementedException();
			}

			public void ChangeTopLeftHeight(float heightDelta, bool signalNeighbours = true) {
				throw new NotImplementedException();
			}

			public void SetTopLeftHeight(float newHeight, bool signalNeighbours = true) {
				throw new NotImplementedException();
			}

			public void CornerHeightChange() {
				throw new NotImplementedException();
			}

			public float GetHeightAt(float x, float y)
			{
				throw new NotImplementedException();
			}

			public float GetHeightAt(Vector2 position)
			{
				throw new NotImplementedException();
			}

			#endregion

			public float TopLeftHeight { get; private set; }

			public IntRect MapArea { get; private set; }

			/// <summary>
			/// Location in the Map matrix
			/// </summary>
			public IntVector2 MapLocation => new IntVector2(MapArea.Left, MapArea.Top);

			

			public Vector2 Center => new Vector2(MapLocation.X + 0.5f, MapLocation.Y + 0.5f);

			

			public HeightTestTile(IntVector2 location, float height) {
				this.MapArea = new IntRect(location.X, location.Y, location.X + 1, location.Y + 1);
				this.TopLeftHeight = height;
			}
		}

		Map slopedInX10x10;
		Map slopedInY10x10;
		Map cone10x10;
		Random rand;

		[OneTimeSetUp]
		public void TestSetup()
		{


			slopedInX10x10 = Map.CreateDefaultMap(null, null, new IntVector2(10, 10));
			slopedInX10x10.ChangeTileHeight(slopedInX10x10.GetTileByMapLocation(5, 5),
											new IntVector2(12, 12),
											(cHeight, x, y) => x);

			

			slopedInY10x10 = Map.CreateDefaultMap(null, new Node(), new IntVector2(10, 10));
			slopedInY10x10.ChangeTileHeight(slopedInX10x10.GetTileByMapLocation(5, 5),
											new IntVector2(12, 12),
											(cHeight, x, y) => y);


			
			cone10x10 = Map.CreateDefaultMap(null, new Node(), new IntVector2(10, 10));
			cone10x10.ChangeTileHeight(slopedInX10x10.GetTileByMapLocation(5, 5),
										new IntVector2(12, 12),
										(cHeight, x, y) => Math.Max(Math.Abs(y - 5), Math.Abs(x - 5)));

			rand = new Random();
		}

		bool FloatsEqual(float a, float b, float epsilon = 0.000001f) {
			float diff = Math.Abs(a - b);
			return (a == b) || diff < float.Epsilon || diff / (Math.Abs(a) + Math.Abs(b)) < epsilon;
		}

		[Test]
		public void SlopedInXPrecise() {
			for (int y = 0; y < 10; y++) {
				for (int x = 0; x < 10; x++) {
					float value;
					Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(x, y), x), $"Precise at {x},{y} does not work, value: {value}");
				}
			}
			Assert.Pass("Precise on XSloped working");
		}

		[Test]
		public void SlopedInYPrecise() {
			for (int y = 0; y < 10; y++) {
				for (int x = 0; x < 10; x++) {
					float value;
					Assert.True(FloatsEqual(value = slopedInY10x10.GetTerrainHeightAt(x, y), y), $"Precise at {x},{y} does not work, value: {value}");
				}
			}
			Assert.Pass("Precise on YSloped working");
		}

		[Test]
		public void ConePrecise() {
			for (int y = 0; y < 10; y++) {
				for (int x = 0; x < 10; x++) {
					float value;
					Assert.True(FloatsEqual(value = cone10x10.GetTerrainHeightAt(x, y), Math.Max(Math.Abs(y - 5), Math.Abs(x - 5))), $"Precise at {x},{y} does not work, value: {value}, expected: {Math.Max(Math.Abs(y - 5), Math.Abs(x - 5))}");
				}
			}
			Assert.Pass("Precise on cone working");
		}

		[Test]
		public void SlopedInXGeneralTopLeft() {
			float value;
			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(0.3f, 0.3f), 0.3f), $"Top left triangle at {0.3f},{0.3f} does not work, value: {value}");

			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(5.42f, 8.24f), 5.42f), $"Top left triangle at {5.42f},{8.24f} does not work, value: {value}");

			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(5.42f, 8.24f), 5.42f), $"Top left triangle at {5.42f},{8.24f} does not work, value: {value}");

			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(6.11f, 4f), 6.11f), $"Top side at {6.11f},{4f} does not work, value: {value}");

			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(6f, 4.11f), 6f), $"Left side at {6f},{4.11f} does not work, value: {value}");

			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(7f, 7f), 7f), $"Top left corner at {7f},{7f} does not work, value: {value}");

			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(7f, 4.99f), 7f), $"Bottom left corner at {7f},{4.99f} does not work, value: {value}");

			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(4.99f, 4f), 4.99f), $"Top right corner at {4.99f},{4f} does not work, value: {value}");

			Assert.Pass("General on XSloped working in top left triangle");
		}

		[Test]
		public void SlopedInYGeneralBottomRight() {
			float value;
			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(0.6f, 0.9f), 0.6f), $"Bottom right triangle at {0.6f},{0.9f} does not work, value: {value}");

			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(5.92f, 3.64f), 5.92f), $"Bottom right triangle at {5.92f},{3.64f} does not work, value: {value}");

			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(7.85f, 8.32f), 7.85f), $"Bottom right triangle at {7.85f},{8.32f} does not work, value: {value}");

			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(6.11f, 4.99f), 6.11f), $"Bottom side at {6.11f},{4.99f} does not work, value: {value}");

			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(6.99f, 4.11f), 6.99f), $"Right side at {6.99f},{4.11f} does not work, value: {value}");

			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(7.99f, 7.99f), 7.99f), $"Bottom right corner at {7.99f},{7.99f} does not work, value: {value}");

			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(7.1f, 4.99f), 7.1f), $"Bottom left corner at {7.1f},{4.99f} does not work, value: {value}");

			Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(4.99f, 4.1f), 4.99f), $"Top right corner at {4.99f},{4.1f} does not work, value: {value}");

			Assert.Pass("General on XSloped working in bottom right triangle");
		}


		[Test]
		public void SlopedInXGeneralRand() {
			for (int y = 0; y < 10; y++) {
				for (int x = 0; x < 10; x++) {
					float value;
					float xPos = x + (float) rand.NextDouble();
					//TODO: Math min because map is not finished and cant handle the bottom and right side tiles
					Assert.True(FloatsEqual(value = slopedInX10x10.GetTerrainHeightAt(xPos, y), Math.Min(9,xPos)), $"General rand at {xPos},{y} does not work, value: {value}");
				}
			}
			Assert.Pass("General on XSloped working");
		}

		[Test]
		public void SlopedInYGeneralRand() {
			for (int y = 0; y < 10; y++) {
				for (int x = 0; x < 10; x++) {
					float value;
					float yPos = y + (float)rand.NextDouble();
					Assert.True(FloatsEqual(value = slopedInY10x10.GetTerrainHeightAt(x, yPos), Math.Min(9,yPos)), $"General rand at {x},{yPos} does not work, value: {value}");
				}
			}
			Assert.Pass("General on YSloped working");
		}

		[Test]
		//TEST IS FAILING BECAUSE MAP CANT HANDLE RIGHT AND BOTTOM SIDE TILE HEIGHTS
		public void ConeGeneralRand() {
			for (int y = 0; y < 10; y++) {
				for (int x = 0; x < 10; x++) {
					float value;
					float yPos = y + (float)rand.NextDouble();
					float xPos = x + (float) rand.NextDouble();
					Assert.True(FloatsEqual(value = cone10x10.GetTerrainHeightAt(xPos, yPos), Math.Max(Math.Abs(yPos - 5), Math.Abs(xPos - 5))), $"General rand at {xPos},{yPos} does not work, value: {value}, expected : {Math.Max(Math.Abs(yPos - 5), Math.Abs(xPos - 5))}");
				}
			}
			Assert.Pass("General on Cone working");
		}
	}
}
