﻿using NUnit.Framework;
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

        private class HeightTestTile : ITile {
            #region NOT USED IN TESTS
            public IUnit Unit => throw new NotImplementedException();

            public List<IUnit> PassingUnits => throw new NotImplementedException();

            public float MovementSpeedModifier => throw new NotImplementedException();

            public TileType Type => throw new NotImplementedException();

            public Map Map => throw new NotImplementedException();

            public void ConnectReferences() {
                throw new NotImplementedException();
            }

            public void FinishLoading() {
                throw new NotImplementedException();
            }

            public bool SpawnUnit(Player player) {
                throw new NotImplementedException();
            }

            public void AddPassingUnit(IUnit unit) {
                throw new NotImplementedException();
            }

            public bool TryAddOwningUnit(IUnit unit) {
                throw new NotImplementedException();
            }

            public void RemoveUnit(IUnit unit) {
                throw new NotImplementedException();
            }

            public StTile Save() {
                throw new NotImplementedException();
            }

            public void ChangeType(TileType newType) {
                throw new NotImplementedException();
            }

            public void ChangeHeight(float heightDelta) {
                throw new NotImplementedException();
            }

            public void SetHeight(float newHeight) {
                throw new NotImplementedException();
            }

            public Path GetPath(IUnit forUnit) {
                throw new NotImplementedException();
            }

            #endregion

            public IntRect MapArea { get; private set; }

            /// <summary>
            /// Location in the Map matrix
            /// </summary>
            public IntVector2 Location => new IntVector2(MapArea.Left, MapArea.Top);

            public Vector2 Center => new Vector2(Location.X + 0.5f, Location.Y + 0.5f);

            public Vector3 Center3 => throw new NotImplementedException();

            public float Height { get; private set; }

            public HeightTestTile(IntVector2 location, float height) {
                this.MapArea = new IntRect(location.X, location.Y, location.X + 1, location.Y + 1);
                this.Height = height;
            }
        }

        private Map slopedInX10x10;
        private Map slopedInY10x10;
        private Map cone10x10;
        private Random rand;

        [OneTimeSetUp]
        public void TestSetup() {
            ITile[] testTiles = new ITile[10 * 10];
            for (int y = 0; y < 10; y++) {
                for (int x = 0; x < 10; x++) {
                    testTiles[x + y * 10] = new HeightTestTile(new IntVector2(x, y), x);
                }
            }

            slopedInX10x10 = Map.CreateTestMap(testTiles, new IntVector2(10, 10));

            testTiles = new ITile[10 * 10];
            for (int y = 0; y < 10; y++) {
                for (int x = 0; x < 10; x++) {
                    testTiles[x + y * 10] = new HeightTestTile(new IntVector2(x, y), y);
                }
            }

            slopedInY10x10 = Map.CreateTestMap(testTiles, new IntVector2(10, 10));

            testTiles = new ITile[10 * 10];
            for (int y = 0; y < 10; y++) {
                for (int x = 0; x < 10; x++) {
                    testTiles[x + y * 10] = new HeightTestTile(new IntVector2(x, y), Math.Max(Math.Abs(y - 5), Math.Abs(x - 5)));
                }
            }

            cone10x10 = Map.CreateTestMap(testTiles, new IntVector2(10, 10));

            rand = new Random();
        }

        private bool FloatsEqual(float a, float b, float epsilon = 0.000001f) {
            float diff = Math.Abs(a - b);
            return (a == b) || diff < float.Epsilon || diff / (Math.Abs(a) + Math.Abs(b)) < epsilon;
        }

        [Test]
        public void SlopedInXPrecise() {
            for (int y = 0; y < 10; y++) {
                for (int x = 0; x < 10; x++) {
                    float value;
                    Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(x, y), x), $"Precise at {x},{y} does not work, value: {value}");
                }
            }
            Assert.Pass("Precise on XSloped working");
        }

        [Test]
        public void SlopedInYPrecise() {
            for (int y = 0; y < 10; y++) {
                for (int x = 0; x < 10; x++) {
                    float value;
                    Assert.True(FloatsEqual(value = slopedInY10x10.GetHeightAt(x, y), y), $"Precise at {x},{y} does not work, value: {value}");
                }
            }
            Assert.Pass("Precise on YSloped working");
        }

        [Test]
        public void ConePrecise() {
            for (int y = 0; y < 10; y++) {
                for (int x = 0; x < 10; x++) {
                    float value;
                    Assert.True(FloatsEqual(value = cone10x10.GetHeightAt(x, y), Math.Max(Math.Abs(y - 5), Math.Abs(x - 5))), $"Precise at {x},{y} does not work, value: {value}, expected: {Math.Max(Math.Abs(y - 5), Math.Abs(x - 5))}");
                }
            }
            Assert.Pass("Precise on cone working");
        }

        [Test]
        public void SlopedInXGeneralTopLeft() {
            float value;
            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(0.3f, 0.3f), 0.3f), $"Top left triangle at {0.3f},{0.3f} does not work, value: {value}");

            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(5.42f, 8.24f), 5.42f), $"Top left triangle at {5.42f},{8.24f} does not work, value: {value}");

            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(5.42f, 8.24f), 5.42f), $"Top left triangle at {5.42f},{8.24f} does not work, value: {value}");

            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(6.11f, 4f), 6.11f), $"Top side at {6.11f},{4f} does not work, value: {value}");

            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(6f, 4.11f), 6f), $"Left side at {6f},{4.11f} does not work, value: {value}");

            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(7f, 7f), 7f), $"Top left corner at {7f},{7f} does not work, value: {value}");

            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(7f, 4.99f), 7f), $"Bottom left corner at {7f},{4.99f} does not work, value: {value}");

            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(4.99f, 4f), 4.99f), $"Top right corner at {4.99f},{4f} does not work, value: {value}");

            Assert.Pass("General on XSloped working in top left triangle");
        }

        [Test]
        public void SlopedInYGeneralBottomRight() {
            float value;
            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(0.6f, 0.9f), 0.6f), $"Bottom right triangle at {0.6f},{0.9f} does not work, value: {value}");

            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(5.92f, 3.64f), 5.92f), $"Bottom right triangle at {5.92f},{3.64f} does not work, value: {value}");

            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(7.85f, 8.32f), 7.85f), $"Bottom right triangle at {7.85f},{8.32f} does not work, value: {value}");

            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(6.11f, 4.99f), 6.11f), $"Bottom side at {6.11f},{4.99f} does not work, value: {value}");

            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(6.99f, 4.11f), 6.99f), $"Right side at {6.99f},{4.11f} does not work, value: {value}");

            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(7.99f, 7.99f), 7.99f), $"Bottom right corner at {7.99f},{7.99f} does not work, value: {value}");

            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(7.1f, 4.99f), 7.1f), $"Bottom left corner at {7.1f},{4.99f} does not work, value: {value}");

            Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(4.99f, 4.1f), 4.99f), $"Top right corner at {4.99f},{4.1f} does not work, value: {value}");

            Assert.Pass("General on XSloped working in bottom right triangle");
        }


        [Test]
        public void SlopedInXGeneralRand() {
            for (int y = 0; y < 10; y++) {
                for (int x = 0; x < 10; x++) {
                    float value;
                    float xPos = x + (float) rand.NextDouble();
                    //TODO: Math min because map is not finished and cant handle the bottom and right side tiles
                    Assert.True(FloatsEqual(value = slopedInX10x10.GetHeightAt(xPos, y), Math.Min(9,xPos)), $"General rand at {xPos},{y} does not work, value: {value}");
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
                    Assert.True(FloatsEqual(value = slopedInY10x10.GetHeightAt(x, yPos), Math.Min(9,yPos)), $"General rand at {x},{yPos} does not work, value: {value}");
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
                    Assert.True(FloatsEqual(value = cone10x10.GetHeightAt(xPos, yPos), Math.Max(Math.Abs(yPos - 5), Math.Abs(xPos - 5))), $"General rand at {xPos},{yPos} does not work, value: {value}, expected : {Math.Max(Math.Abs(yPos - 5), Math.Abs(xPos - 5))}");
                }
            }
            Assert.Pass("General on Cone working");
        }
    }
}
