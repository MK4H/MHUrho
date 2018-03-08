using System;
using System.Collections.Generic;

using NUnit.Framework;

using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.Control;
using MHUrho.Graphics;
using Urho;

namespace NUnit.Tests {
    class AStarTests {

        private TestMap allOneSpeed;
        private TestMap randomSpeed;
        private TestMap oneWithCross;
        private TestMap randomWithCross;

        class TestUnit : IUnit {
            #region NOT USED IN TEST

            public int ID => throw new NotImplementedException();
            public UnitType Type => throw new NotImplementedException();

            public LevelManager Level => throw new NotImplementedException();

            public IPlayer Player => throw new NotImplementedException();

            public bool Select() {
                throw new NotImplementedException();
            }

            public bool Order(ITile tile) {
                throw new NotImplementedException();
            }

            public bool Order(IUnit unit) {
                throw new NotImplementedException();
            }

            public void Deselect() {
                throw new NotImplementedException();
            }

            public void Update(TimeSpan gameTime) {
                throw new NotImplementedException();
            }

            public StUnit Save() {
                throw new NotImplementedException();
            }
            #endregion
            public Vector2 Position { get; private set; }

            public ITile Tile { get; private set; }

            private readonly float defaultSpeed;

            public bool CanPass(ITile tile) {
                return true;
            }

            public float MovementSpeed(ITile tile) {
                return tile.MovementSpeedModifier * defaultSpeed;
            }

            public TestUnit(float speed, ITile tile, Vector2 position) {
                this.defaultSpeed = speed;
                this.Tile = tile;
                this.Position = position;
            }
        }

        class TestMap : IMap {

            class PassableTestTile : ITile {
                #region NOT USED IN TEST
                public Unit Unit => throw new NotImplementedException();

                public List<Unit> PassingUnits => throw new NotImplementedException();

                public TileType Type => throw new NotImplementedException();

                public IntRect MapArea => throw new NotImplementedException();

                

                public Vector2 Center => throw new NotImplementedException();

                public float Height => throw new NotImplementedException();
               
                public LevelManager Level => throw new NotImplementedException();

                public void ConnectReferences() {
                    throw new NotImplementedException();
                }

                public void FinishLoading() {
                    throw new NotImplementedException();
                }


                public bool SpawnUnit(Player player) {
                    throw new NotImplementedException();
                }

                public void AddPassingUnit(Unit unit) {
                    throw new NotImplementedException();
                }

                public bool TryAddOwningUnit(Unit unit) {
                    throw new NotImplementedException();
                }

                public void RemoveUnit(Unit unit) {
                    throw new NotImplementedException();
                }

                public StTile Save() {
                    throw new NotImplementedException();
                }

                public void ChangeType(TileType newType) {
                    throw new NotImplementedException();
                }
                #endregion

                public IntVector2 Location { get; private set; }

                public float MovementSpeedModifier { get; private set; }

                public PassableTestTile(float speedModifier, int x, int y) {
                    this.MovementSpeedModifier = speedModifier;
                    Location = new IntVector2(x, y);
                }
            }

            class NotPassableTestTile : ITile {
                #region NOT USED IN TEST
                public Unit Unit => throw new NotImplementedException();

                public List<Unit> PassingUnits => throw new NotImplementedException();



                public TileType Type => throw new NotImplementedException();

                public IntRect MapArea => throw new NotImplementedException();


                public Vector2 Center => throw new NotImplementedException();

                public float Height => throw new NotImplementedException();
               

                public LevelManager Level => throw new NotImplementedException();

                public void ConnectReferences() {
                    throw new NotImplementedException();
                }

                public void FinishLoading() {
                    throw new NotImplementedException();
                }

                public bool SpawnUnit(Player player) {
                    throw new NotImplementedException();
                }

                public void AddPassingUnit(Unit unit) {
                    throw new NotImplementedException();
                }

                public bool TryAddOwningUnit(Unit unit) {
                    throw new NotImplementedException();
                }

                public void RemoveUnit(Unit unit) {
                    throw new NotImplementedException();
                }

                public StTile Save() {
                    throw new NotImplementedException();
                }

                public void ChangeType(TileType newType) {
                    throw new NotImplementedException();
                }
                #endregion

                public IntVector2 Location { get; private set; }

                //Should not call this if it is not passable
                public float MovementSpeedModifier => throw new NotImplementedException();

                public NotPassableTestTile(int x, int y) {
                    Location = new IntVector2(x, y);
                }
            }

            ITile[][] map;

            #region NOT USED IN TEST

            public MapGraphics Graphics => throw new NotImplementedException();

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

            public float GetHeightAt(Vector2 position) {
                throw new NotImplementedException();
            }

            public StMap Save() {
                throw new NotImplementedException();
            }

            public void HighlightArea(ITile center, IntVector2 size) {
                throw new NotImplementedException();
            }

            public void HideHighlight() {
                throw new NotImplementedException();
            }

            #endregion

            

            public IntVector2 TopLeft { get; private set; }


            public IntVector2 BottomRight { get; private set; }

            public int Width => Right + 1;

            public int Length => Bottom + 1;

            public int Left => TopLeft.X;

            public int Right => BottomRight.X;

            public int Top => TopLeft.Y;

            public int Bottom => BottomRight.Y;

            public bool IsInside(IntVector2 point) {
                return point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
            }

            public ITile GetTile(int x, int y) {
                return map[x][y];
            }

            public ITile GetTile(IntVector2 vector) {
                return GetTile(vector.X, vector.Y);
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

        public void StraightCornerToCornerAllSpeedsOne() {
            var aStar = new AStar(allOneSpeed);

            //Top
            var unit = new TestUnit(1, allOneSpeed.GetTile(0, 0), new Vector2(0.5f, 0.5f));

            List<IntVector2> path = aStar.FindPath(unit, new IntVector2(allOneSpeed.Right, allOneSpeed.Top));
            List<IntVector2> expected = new List<IntVector2>();
            for (int i = 0; i <= allOneSpeed.Right; i++) {
                expected.Add(new IntVector2(i, 0));
            }
            CollectionAssert.AreEqual(expected, path,"Fail going from topLeft to the topRight");

            path = aStar.FindPath(unit, new IntVector2(allOneSpeed.Left, allOneSpeed.Bottom));
            expected.Clear();
            for (int i = 0; i <= allOneSpeed.Bottom; i++) {
                expected.Add(new IntVector2(0, i));
            }
            CollectionAssert.AreEqual(expected, path, "Fail going from topLeft to the bottomLeft");

            unit = new TestUnit(1, allOneSpeed.GetTile(allOneSpeed.BottomRight), new Vector2(allOneSpeed.Right + 0.5f, allOneSpeed.Bottom + 0.5f));

            path = aStar.FindPath(unit, new IntVector2(allOneSpeed.Left, allOneSpeed.Bottom));
            expected.Clear();
            for (int i = allOneSpeed.Right; i >= allOneSpeed.Left; i--) {
                expected.Add(new IntVector2(i, allOneSpeed.Bottom));
            }
            CollectionAssert.AreEqual(expected, path, "Fail going from bottomRight to the bottomLeft");

            path = aStar.FindPath(unit, new IntVector2(allOneSpeed.Right, allOneSpeed.Top));
            expected.Clear();
            for (int i = allOneSpeed.Bottom; i >= allOneSpeed.Top; i--) {
                expected.Add(new IntVector2(allOneSpeed.Right, i));
            }
            CollectionAssert.AreEqual(expected, path, "Fail going from bottomRight to the topRight");
            Assert.Pass();
        }

        //[Test]
        //public void DiagonalCornerToCornerInSquare() {





        //}

        //[Test]
        //public void DiagonalCornerToCornerInRectangle() {





        //}


        [Test]

        public void StartIsFinish() {
            var aStar = new AStar(allOneSpeed);

            var unit = new TestUnit(1, allOneSpeed.GetTile(10, 10), new Vector2(10.5f, 10.5f));

            List<IntVector2> path = aStar.FindPath(unit, new IntVector2(10, 10));

            Assert.IsNotNull(path);
            CollectionAssert.AreEqual(new List<IntVector2>(){new IntVector2(10,10)}, path);
            Assert.Pass();
        }
    }
}