using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho;
using Urho.Resources;
using MHUrho.Storage;

namespace MHUrho.WorldMap
{
	public partial class Map {

		enum BorderType { None, Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight }

		class MapGraphics : IDisposable {

			const float HighlightHeightAboveTerain = 0.02f;

			static Vector3 HighlightAboveTerrainOffset = new Vector3(0, HighlightHeightAboveTerain, 0);

			class CornerTiles : IEnumerable<ITile>,IEnumerator<ITile> {
				public ITile TopLeft;
				public ITile TopRight;
				public ITile BottomLeft;
				public ITile BottomRight;

				int state = -1;

				public IEnumerator<ITile> GetEnumerator() {
					state = -1;
					return this;
				}

				IEnumerator IEnumerable.GetEnumerator() {
					return GetEnumerator();
				}

				public bool MoveNext() {
					do {
						++state;
					} while (state < 4 && Current == null);

					return state < 4;
				}

				public void Reset() {
					state = -1;
				}

				public ITile Current {
					get {
						switch (state) {
							case 0: return TopLeft;
							case 1: return TopRight;
							case 2: return BottomLeft;
							case 3: return BottomRight;
							default:
								throw new InvalidOperationException("Current with invalid state");
						}
					}
				}

				object IEnumerator.Current => Current;

				public void Dispose() {

				}
			}

			class MapChunk : IDisposable {

				[StructLayout(LayoutKind.Sequential)]
				struct TileVertex {
					public Vector3 Position;
					public Vector3 Normal;
					public Vector2 TexCoords;

					public TileVertex(Vector3 position, Vector3 normal, Vector2 texCoords)
					{
						this.Position = position;
						this.Normal = normal;
						this.TexCoords = texCoords;
					}
				}

				[StructLayout(LayoutKind.Sequential)]
				struct TileInVB {
					public const int VerticiesPerTile = 4;

					public TileVertex TopLeft;
					public TileVertex TopRight;
					public TileVertex BottomLeft;
					public TileVertex BottomRight;

					public void ChangeTextureCoords(Rect rect)
					{
						TopLeft.TexCoords = rect.Min;
						TopRight.TexCoords = new Vector2(rect.Max.X, rect.Min.Y);
						BottomLeft.TexCoords = new Vector2(rect.Min.X, rect.Max.Y);
						BottomRight.TexCoords = rect.Max;
					}

					public TileInVB(ITile tile, Vector3 chunkPosition)
					{
						TopLeft = new TileVertex(tile.TopLeft3 - chunkPosition,
												 new Vector3(0, 1, 0),
												 new Vector2(tile.Type.TextureCoords.Min.X, tile.Type.TextureCoords.Min.Y));
						TopRight = new TileVertex(tile.TopRight3 - chunkPosition,
												  new Vector3(0, 1, 0),
												  new Vector2(tile.Type.TextureCoords.Max.X, tile.Type.TextureCoords.Min.Y));
						BottomLeft = new TileVertex(tile.BottomLeft3 - chunkPosition,
													new Vector3(0, 1, 0),
													new Vector2(tile.Type.TextureCoords.Min.X, tile.Type.TextureCoords.Max.Y));
						BottomRight = new TileVertex(tile.BottomRight3 - chunkPosition,
													 new Vector3(0, 1, 0),
													 new Vector2(tile.Type.TextureCoords.Max.X, tile.Type.TextureCoords.Max.Y));
						CalculateLocalNormals();
					}


					/// <summary>
					/// Creates normals just from this tile, disregarding the angle of surrounding tiles
					/// </summary>
					public void CalculateLocalNormals()
					{
						if (IsTopLeftBotRightDiagHigher()) {
							TopRight.Normal = Vector3.Cross(TopLeft.Position - TopRight.Position,
															BottomRight.Position - TopRight.Position);
							BottomLeft.Normal = Vector3.Cross(BottomRight.Position - BottomLeft.Position,
															  TopLeft.Position - BottomLeft.Position);

							TopLeft.Normal = AverageNormalNotNormalized(ref TopLeft.Position,
																		ref BottomLeft.Position,
																		ref BottomRight.Position,
																		ref TopRight.Position);
							BottomRight.Normal = AverageNormalNotNormalized(ref BottomRight.Position,
																			ref TopRight.Position,
																			ref TopLeft.Position,
																			ref BottomLeft.Position);

						}
						else {
							TopLeft.Normal = Vector3.Cross(BottomLeft.Position - TopLeft.Position,
														   TopRight.Position - TopLeft.Position);
							BottomRight.Normal = Vector3.Cross(TopRight.Position - BottomRight.Position,
															   BottomLeft.Position - BottomRight.Position);
							TopRight.Normal = AverageNormalNotNormalized(ref TopRight.Position,
																		 ref TopLeft.Position,
																		 ref BottomLeft.Position,
																		 ref BottomRight.Position);
							BottomLeft.Normal = AverageNormalNotNormalized(ref BottomLeft.Position,
																		   ref BottomRight.Position,
																		   ref TopRight.Position,
																		   ref TopLeft.Position);

						}


						TopLeft.Normal.Normalize();
						TopRight.Normal.Normalize();
						BottomLeft.Normal.Normalize();
						BottomRight.Normal.Normalize();
					}

					public bool IsTopLeftBotRightDiagHigher()
					{
						return TopLeft.Position.Y + BottomRight.Position.Y >= TopRight.Position.Y + BottomLeft.Position.Y;
					}

					public bool IsTopRightBotLeftDiagHigher()
					{
						return !IsTopLeftBotRightDiagHigher();
					}

					public override string ToString()
					{
						return $"Top left: {TopLeft.Position}";
					}

					/// <summary>
					/// Gets the average vector of the normals of the two adjacent triangles
					/// 
					/// This normal vector is not normalized yet
					/// </summary>
					/// <param name="center">the point where the normal vector originates</param>
					/// <param name="first">first vector in counterclockwise direction</param>
					/// <param name="second">second vector in counterclockwise direction</param>
					/// <param name="third">third vector in counterclockwise direction</param>
					/// <returns>Not normalized normal vector</returns>
					Vector3 AverageNormalNotNormalized(ref Vector3 center,
															   ref Vector3 first,
															   ref Vector3 second,
															   ref Vector3 third)
					{

						var topLeftNormal1 = Vector3.Cross(first - center,
														   second - center);
						var topLeftNormal2 = Vector3.Cross(second - center,
														   third - center);
						topLeftNormal1.Normalize();
						topLeftNormal2.Normalize();

						return topLeftNormal1 + topLeftNormal2;
					}
				}

				[StructLayout(LayoutKind.Sequential)]
				struct TileInIB {

					public const int IndiciesPerTile = 6;
					/*
					 * 0-----1
					 * |     |
					 * |     |
					 * 2-----3
					 *
					 * We switch from
					 * 0-----1
					 * |   / |
					 * |  /  |
					 * | /   |
					 * 2-----3
					 *
					 * to
					 * 0-----1
					 * | \   |
					 * |  \  |
					 * |   \ |
					 * 2-----3

					 */

					short cornerA1;
					short middleA;
					short cornerA2;
					short cornerB1;
					short middleB;
					short cornerB2;

					public unsafe void TestAndRotate(TileInVB* tileInVB)
					{
						var cornerA1LocalIndex = cornerA1 % 4;
						if ((tileInVB->IsTopLeftBotRightDiagHigher() && (cornerA1LocalIndex == 1 || cornerA1LocalIndex == 2)) ||
							(tileInVB->IsTopRightBotLeftDiagHigher() && (cornerA1LocalIndex == 0 || cornerA1LocalIndex == 3))) {
							//If the diagonal is not the one that is higher, rotate the diagonal
							Rotate();
						}
					}

					public TileInIB(short topLeft, short topRight, short bottomLeft, short bottomRight, ref TileInVB tileInVB)
					{
						if (tileInVB.IsTopLeftBotRightDiagHigher()) {
							cornerA1 = topLeft;
							middleA = bottomLeft;
							cornerA2 = bottomRight;
							cornerB1 = bottomRight;
							middleB = topRight;
							cornerB2 = topLeft;
						}
						else {
							cornerA1 = bottomLeft;
							middleA = bottomRight;
							cornerA2 = topRight;
							cornerB1 = topRight;
							middleB = topLeft;
							cornerB2 = bottomLeft;
						}
					}

					/// <summary>
					/// Rotates the split in the quad
					/// </summary>
					void Rotate()
					{
						//Because values will be some indecies to the vertex buffer, do rotation by rotating values
						short tmp = cornerA1;
						cornerA1 = middleA;
						cornerB2 = middleA;
						middleA = cornerA2;
						cornerA2 = middleB;
						cornerB1 = middleB;
						middleB = tmp;
					}
				}

				unsafe class VertexBufferWrapper : IDisposable {
					readonly VertexBuffer vertexBuffer;
					readonly MapChunk chunk;

					public bool Locked => vertexBuffer.Locked;

					public TileInVB* Data{ get; private set; }

					public VertexBufferWrapper(MapChunk chunk, VertexBuffer vertexBuffer)
					{
						this.chunk = chunk;
						this.vertexBuffer = vertexBuffer;
					}

					public void Lock()
					{
						if (Locked) return;

						IntPtr vbData = vertexBuffer.Lock(0, (uint)chunk.TileCount * TileInVB.VerticiesPerTile);

						if (vbData == IntPtr.Zero) {
							throw new Exception("Could not lock buffer to memory");
						}

						Data = (TileInVB*)vbData.ToPointer();
					}

					public void Unlock()
					{
						if (!Locked) return;

						vertexBuffer.Unlock();

						Data = null;
					}

					public void Dispose()
					{
						vertexBuffer.Dispose();
					}
				}

				unsafe class IndexBufferWrapper : IDisposable {
					readonly IndexBuffer indexBuffer;
					readonly MapChunk chunk;

					public bool Locked => indexBuffer.Locked;

					public TileInIB* Data { get; private set; }

					public IndexBufferWrapper(MapChunk chunk, IndexBuffer indexBuffer)
					{
						this.chunk = chunk;
						this.indexBuffer = indexBuffer;
					}

					public void Lock()
					{
						if (Locked) return;

						IntPtr ibData = indexBuffer.Lock(0, (uint)chunk.TileCount * TileInIB.IndiciesPerTile);

						if (ibData == IntPtr.Zero) {
							throw new Exception("Could not lock buffer to memory");
						}

						Data = (TileInIB*)ibData.ToPointer();
					}

					public void Unlock()
					{
						if (!Locked) return;

						indexBuffer.Unlock();

						Data = null;
					}

					public void Dispose()
					{
						indexBuffer?.Dispose();
					}
				}

				public bool Locked { get; private set; }

				Model model;
				VertexBufferWrapper vertexBuffer;
				IndexBufferWrapper indexBuffer;

				Node chunkNode;

				IntVector2 topLeftCorner;
				IntVector2 size => graphics.chunkSize;

				int TileCount => size.X * size.Y;

				readonly MapGraphics graphics;

				Map Map => graphics.map;

				public MapChunk(Map map, MapGraphics graphics, IntVector2 topLeftCorner)
				{
					this.graphics = graphics;
					this.topLeftCorner = topLeftCorner;
					MyGame.InvokeOnMainSafe(() => {
												this.chunkNode = map.node.CreateChild("chunkNode");
												chunkNode.Position = new Vector3(topLeftCorner.X + size.X / 2.0f, 0, topLeftCorner.Y + size.Y / 2.0f);
					});
						
					CreateModel(map);
				}

				public void Dispose()
				{
					model?.Dispose();
					vertexBuffer.Dispose();
					indexBuffer.Dispose();
					chunkNode.Dispose();
				}

				public unsafe void ChangeTileType(int x, int y, TileType newType)
				{
					CheckLocked();

					vertexBuffer.Lock();

					TileInVB* tileInVertexBuffer = vertexBuffer.Data + GetBufferOffset(x,y);
					tileInVertexBuffer->ChangeTextureCoords(newType.TextureCoords);					
				}

				/// <summary>
				/// Changes tile height to the height of the logical tiles
				/// </summary>
				/// <param name="topLeft"></param>
				/// <param name="bottomRight"></param>
				public unsafe void CorrectTileHeight(int x, int y)
				{
					CheckLocked();

					vertexBuffer.Lock();
					indexBuffer.Lock();

					int offset = GetBufferOffset(x, y);
					TileInVB* tileInVertexBuffer = vertexBuffer.Data + offset;
					TileInIB* tileInIndexBuffer = indexBuffer.Data + offset;

					tileInVertexBuffer->TopLeft.Position.Y = Map.GetTerrainHeightAt(x, y);
					tileInVertexBuffer->TopRight.Position.Y = Map.GetTerrainHeightAt(x + 1, y);
					tileInVertexBuffer->BottomLeft.Position.Y = Map.GetTerrainHeightAt(x, y + 1);
					tileInVertexBuffer->BottomRight.Position.Y = Map.GetTerrainHeightAt(x + 1, y + 1);

					tileInVertexBuffer->CalculateLocalNormals();
					tileInIndexBuffer->TestAndRotate(tileInVertexBuffer);
				}

				public void Lock()
				{
					//Lazy locking, every operation tests if the correct buffers are locked and lockes them if needed
					Locked = true;
				}

				public void Unlock()
				{
					if (!Locked) {
						return;
					}

					if (vertexBuffer.Locked) {
						//Bounding box could have changed, recalculate
						FixBoundingBox();
					}

					//Unlocks only if the buffer was locked
					vertexBuffer.Unlock();
					indexBuffer.Unlock();

					Locked = false;
				}

				int GetBufferOffset(int x, int y)
				{
					return x - topLeftCorner.X + (y - topLeftCorner.Y) * size.X;
				}

				int GetBufferOffset(ITile tile)
				{
					//TODO: maybe - Map.Left and - Map.Top
					return GetBufferOffset(tile.TopLeft.X, tile.TopLeft.Y);
				}

				void CheckLocked()
				{
					if (!Locked) throw new InvalidCastException("Locked operation on unlocked chunk");
				}

				unsafe void FixBoundingBox()
				{
					TileInVB* tile = vertexBuffer.Data;
					float minHeight = tile->TopLeft.Position.Y;
					float maxHeight = minHeight;
					float currentHeight;

					int countWithouthBottomRow = TileCount - graphics.chunkSize.X;
					for (int i = 0; i < countWithouthBottomRow ; i++, tile++) {
						currentHeight = tile->TopLeft.Position.Y;
						minHeight = Math.Min(minHeight, currentHeight);
						maxHeight = Math.Max(maxHeight, currentHeight);
					}

					//Bottom row
					tile = vertexBuffer.Data;
					for (int i = 0; i < graphics.chunkSize.X; i++, tile++) {
						currentHeight = tile->TopLeft.Position.Y;
						minHeight = Math.Min(minHeight, currentHeight);
						maxHeight = Math.Max(maxHeight, currentHeight);
						currentHeight = tile->BottomLeft.Position.Y;
						minHeight = Math.Min(minHeight, currentHeight);
						maxHeight = Math.Max(maxHeight, currentHeight);
					}

					//Right column
					tile = vertexBuffer.Data + graphics.chunkSize.X - 1;
					for (int i = 0; i < graphics.chunkSize.Y - 1; i++, tile += graphics.chunkSize.X) {
						currentHeight = tile->TopRight.Position.Y;
						minHeight = Math.Min(minHeight, currentHeight);
						maxHeight = Math.Max(maxHeight, currentHeight);
					}

					//Last tile
					currentHeight = tile->TopRight.Position.Y;
					minHeight = Math.Min(minHeight, currentHeight);
					maxHeight = Math.Max(maxHeight, currentHeight);
					currentHeight = tile->BottomRight.Position.Y;
					minHeight = Math.Min(minHeight, currentHeight);
					maxHeight = Math.Max(maxHeight, currentHeight);

					var oldBoundingBox = model.BoundingBox;
					model.BoundingBox = new BoundingBox(new Vector3(oldBoundingBox.Min.X,
																	minHeight,
																	oldBoundingBox.Min.Z),
														new Vector3(oldBoundingBox.Max.X,
																	maxHeight,
																	oldBoundingBox.Max.Z));

				}

				void CreateModel(Map map)
				{

					//4 verticies for every tile, so that we can map every tile to different texture
					// and the same tile types to the same textures
					uint numVerticies = (uint)(size.X * size.Y * 4);

					//two triangles per tile, 3 indicies per triangle
					uint numIndicies = (uint)(size.X * size.Y * 6);


					VertexBuffer vb = InitializeVertexBuffer(numVerticies);
					IndexBuffer ib = InitializeIndexBuffer(numIndicies);


					IntPtr vbPointer = LockVertexBufferSafe(vb, numVerticies);
					IntPtr ibPointer = LockIndexBufferSafe(ib, numIndicies);

					if (vbPointer == IntPtr.Zero || ibPointer == IntPtr.Zero) {
						//TODO: Error, could not lock buffers into memory, cannot create map
						throw new Exception("Could not lock buffer into memory for map model creation");
					}

					unsafe {
						TileInVB* verBuff = (TileInVB*)vbPointer.ToPointer();
						TileInIB* inBuff = (TileInIB*)ibPointer.ToPointer();

						int vertexIndex = 0;
						for (int y = topLeftCorner.Y; y < topLeftCorner.Y + size.Y; y++) {
							for (int x = topLeftCorner.X; x < topLeftCorner.X + size.X; x++) {
								ITile tile = map.GetTileByTopLeftCorner(x, y);
								var tileInVB = new TileInVB(tile, chunkNode.Position);

								//Create verticies
								*(verBuff++) = tileInVB;

								//Connect verticies to triangles        
								*(inBuff++) = new TileInIB((short)(vertexIndex + 0),
															(short)(vertexIndex + 1),
															(short)(vertexIndex + 2),
															(short)(vertexIndex + 3),
															ref tileInVB);

								vertexIndex += TileInVB.VerticiesPerTile;
							}
						}

					}

					FinalizeModelCreation(vb, ib, numIndicies);
					this.vertexBuffer = new VertexBufferWrapper(this,vb);
					this.indexBuffer = new IndexBufferWrapper(this,ib);

					SetModel(map.LevelManager);
				}

				static VertexBuffer InitializeVertexBuffer(uint numVerticies)
				{
					return MyGame.InvokeOnMainSafe(InitializeVertexBufferImpl);

					VertexBuffer InitializeVertexBufferImpl()
					{
						//TODO: Context
						VertexBuffer vb = new VertexBuffer(Application.CurrentContext, false);

						vb.Shadowed = true;
						vb.SetSize(numVerticies, ElementMask.Position | ElementMask.Normal | ElementMask.TexCoord1, false);
						return vb;
					}
				}

				static IndexBuffer InitializeIndexBuffer(uint numIndicies)
				{
					return MyGame.InvokeOnMainSafe(InitializeIndexBufferImpl);

					IndexBuffer InitializeIndexBufferImpl()
					{
						IndexBuffer ib = new IndexBuffer(Application.CurrentContext, false);

						ib.Shadowed = true;
						ib.SetSize(numIndicies, false, false);

						return ib;
					}
				}

				static IntPtr LockVertexBufferSafe(VertexBuffer vb, uint numVerticies)
				{
					return MyGame.InvokeOnMainSafe(() => vb.Lock(0, numVerticies));
				}

				static IntPtr LockIndexBufferSafe(IndexBuffer ib, uint numIndicies)
				{
					return MyGame.InvokeOnMainSafe(() => ib.Lock(0, numIndicies));
				}

				void FinalizeModelCreation(VertexBuffer vb, IndexBuffer ib, uint numIndicies)
				{
					MyGame.InvokeOnMainSafe(FinalizeModelCreationImpl);

					void FinalizeModelCreationImpl()
					{
						model = new Model();

						vb.Unlock();
						ib.Unlock();

						Geometry geom = new Geometry();
						geom.SetVertexBuffer(0, vb);
						geom.IndexBuffer = ib;
						geom.SetDrawRange(PrimitiveType.TriangleList, 0, numIndicies, true);

						model.NumGeometries = 1;
						model.SetGeometry(0, 0, geom);
						Vector3 topLeftCorner3 = new Vector3(topLeftCorner.X, -1, topLeftCorner.Y) - chunkNode.Position;
						model.BoundingBox = new BoundingBox(topLeftCorner3,
															topLeftCorner3 + new Vector3(size.X, 2, size.Y));
					}
				}

				

				void SetModel(ILevelManager level)
				{
					MyGame.InvokeOnMainSafe(SetModelImpl);

					void SetModelImpl()
					{
						StaticModel staticModel = chunkNode.CreateComponent<StaticModel>();
						staticModel.Model = model;
						staticModel.SetMaterial(graphics.material);
						//TODO: Draw distance
						staticModel.DrawDistance = level.App.Config.TerrainDrawDistance;
					}
				}
			}

			class RectangleOperation {
				readonly List<MapChunk> lockedChunks;

				readonly IntVector2 topLeft;
				readonly IntVector2 bottomRight;

				IntVector2 currentPosition;

				readonly MapGraphics graphics;

				public RectangleOperation(IntVector2 topLeft, IntVector2 bottomRight, MapGraphics graphics)
				{
					currentPosition = new IntVector2(topLeft.X - 1, topLeft.Y);
					this.topLeft = topLeft;
					this.bottomRight = bottomRight;
					this.graphics = graphics;
					lockedChunks = new List<MapChunk>();
				}

				/// <summary>
				/// Changes whole rectangle of tiles to <paramref name="newTileType"/>
				/// </summary>
				/// <param name="newTileType"></param>
				public void ChangeTileType(TileType newTileType)
				{
					MapChunk chunk;
					while ((chunk = MoveNext()) != null) {
						chunk.ChangeTileType(currentPosition.X, currentPosition.Y, newTileType);
					}
				}

				public void CorrectTileHeight()
				{
					MapChunk chunk;
					while ((chunk = MoveNext()) != null) {
						chunk.CorrectTileHeight(currentPosition.X, currentPosition.Y);
					}
				}

				MapChunk MoveNext()
				{
					/*
					 * Basically this rewritten to step by step incrementing
					 * for (int y = topLeft.Y; y <= bottomRight.Y; y++) {
						for (int x = topLeft.X; x <= bottomRight.X; x++) {
						MapChunk tileChunk = graphics.GetChunk(currentPosition.X, currentPosition.Y);

						if (!tileChunk.Locked) {
							tileChunk.Lock();
							lockedChunks.Add(tileChunk);
						}

						}
					   }
					   foreach (var chunk in lockedChunks) {
						   chunk.Unlock();
					   }
					 */

					if (++currentPosition.X > bottomRight.X) {
						currentPosition.X = topLeft.X;
						if (++currentPosition.Y > bottomRight.Y) {
							foreach (var chunk in lockedChunks) {
								chunk.Unlock();
							}
							return null;
						}
					}

					MapChunk tileChunk = graphics.GetChunk(currentPosition.X, currentPosition.Y);

					if (!tileChunk.Locked) {
						tileChunk.Lock();
						lockedChunks.Add(tileChunk);
					}
					return tileChunk;
				}
			}

			Material material;

			CustomGeometry highlight;

			IntVector2 numberOfChunks;

			IntVector2 chunkSize;

			readonly Map map;


			readonly List<MapChunk> chunks;

			/// <summary>
			/// Creates a graphical representation of the <paramref name="map"/>.
			/// </summary>
			/// <param name="map"></param>
			/// <param name="chunkSize">Size of chunks the map will be divided into, <see cref="Map.Width"/> and <see cref="Map.Length"/>
			/// of <paramref name="map"/> has to be divisible by <paramref name="chunkSize"/></param>
			/// <returns></returns>
			public static MapGraphics Build(Map map,
											IntVector2 chunkSize,
											LoadingWatcher loadingProgress)
			{
				IntVector2 mapSize = new IntVector2(map.Width, map.Length);

				if (mapSize.X % chunkSize.X != 0 ||
					mapSize.Y % chunkSize.Y != 0) {
					throw new ArgumentException("mapSize was not multiple of chunkSize", nameof(chunkSize));
				}

				MapGraphics graphics = new MapGraphics(map, chunkSize, mapSize);

				loadingProgress.EnterPhase("Creating terrain texture");
				graphics.CreateMaterial();
				loadingProgress.IncrementProgress(5);

				loadingProgress.EnterPhase("Creating map geometry");
				graphics.CreateModel(loadingProgress);

				return graphics;
			}

			MapGraphics(Map map, IntVector2 chunkSize, IntVector2 mapSize) {
				this.map = map;
				this.chunkSize = chunkSize;
				this.numberOfChunks = IntVector2.Divide(mapSize, chunkSize);
				chunks = new List<MapChunk>();
			}

			public bool IsRaycastToMap(RayQueryResult rayQueryResult)
			{
				return rayQueryResult.Node.Parent == map.node;
			}

			public ITile RaycastToTile(List<RayQueryResult> rayQueryResults) {
				
				foreach (var rayQueryResult in rayQueryResults) {
					ITile tile = RaycastToTile(rayQueryResult);

					if (tile != null) {
						return tile;
					}
				}
				return null;
			}

			public ITile RaycastToTile(RayQueryResult rayQueryResult) {
				return IsRaycastToMap(rayQueryResult)
						   ? map.GetTileByTopLeftCorner((int) Math.Floor(rayQueryResult.Position.X),
														(int) Math.Floor(rayQueryResult.Position.Z))
						   : null;
			}

			public Vector3? RaycastToVertex(List<RayQueryResult> rayQueryResults) {
				foreach (var rayQueryResult in rayQueryResults) {
					Vector3? corner = RaycastToVertex(rayQueryResult);

					if (corner.HasValue) {
						return corner;
					}
				}
				return null;
			}

			public Vector3? RaycastToVertex(RayQueryResult rayQueryResult) {
				if (!IsRaycastToMap(rayQueryResult)) return null;

				IntVector2 corner = new IntVector2((int)Math.Round(rayQueryResult.Position.X),
												   (int)Math.Round(rayQueryResult.Position.Z));
				float height = map.GetTerrainHeightAt(corner);
				return new Vector3(corner.X, height, corner.Y);
			}

			public Vector3? RaycastToWorldPosition(List<RayQueryResult> rayQueryResults)
			{
				foreach (var rayQueryResult in rayQueryResults) {
					Vector3? corner = RaycastToWorldPosition(rayQueryResult);

					if (corner.HasValue) {
						return corner;
					}
				}
				return null;
			}


			public Vector3? RaycastToWorldPosition(RayQueryResult rayQueryResult)
			{
				if (!IsRaycastToMap(rayQueryResult)) return null;

				return rayQueryResult.Position;
			}



			public void ChangeTileType(IntVector2 location, TileType newTileType) {
				ChangeTileType(location, location, newTileType);
			}

			/// <summary>
			/// Changes whole rectangle of tiles to <paramref name="newTileType"/>
			/// </summary>
			/// <param name="topLeft"></param>
			/// <param name="newTileType"></param>
			/// <param name="bottomRight"></param>
			public void ChangeTileType(IntVector2 topLeft, IntVector2 bottomRight, TileType newTileType) {
				var tileRectangle = new RectangleOperation(topLeft, bottomRight, this);
				tileRectangle.ChangeTileType(newTileType);
			}

		  

			/// <summary>
			/// Changes tile height to the height of the logical tiles
			/// </summary>
			/// <param name="topLeft"></param>
			/// <param name="bottomRight"></param>
			public void CorrectTileHeight(  IntVector2 topLeft, 
											IntVector2 bottomRight)
			{
				var tileRectangle = new RectangleOperation(topLeft, bottomRight, this);
				tileRectangle.CorrectTileHeight();
			}

			public void ChangeCornerHeights(IEnumerable<IntVector2> cornerPositions) {
				List<MapChunk> lockedChunks = new List<MapChunk>();

				foreach (var corner in cornerPositions) {
					for (int y = -1; y < 1; y++) {
						for (int x = -1; x < 1; x++) {
							IntVector2 currentCorner = corner + new IntVector2(x, y);
							ITile tile;
							if ((tile = map.GetTileByTopLeftCorner(currentCorner)) != null) {
								MapChunk chunk = GetChunk(tile);
								if (!chunk.Locked) {
									chunk.Lock();
									lockedChunks.Add(chunk);
								}

								chunk.CorrectTileHeight(currentCorner.X, currentCorner.Y);
							}
						}
					}
				}

				foreach (var chunk in lockedChunks) {
					chunk.Unlock();
				}

				//Smoothing normals, probably redo a little
				//foreach (var changedCorner in changedCorners) {
				//    foreach (var tile in changedCorner) {
				//        //For all 4 corners of the tile, because their normals could have changed
				//        CalculateSmoothNormals(basePointer, tile.Location);
				//        CalculateSmoothNormals(basePointer, tile.Location + new IntVector2(1, 0));
				//        CalculateSmoothNormals(basePointer, tile.Location + new IntVector2(0, 1));
				//        CalculateSmoothNormals(basePointer, tile.Location + new IntVector2(1, 1));
				//    }
				//}
				
			}

			public void HighlightCornerList(IEnumerable<IntVector2> corners, Func<IntVector2, Color> getColor)
			{
				HighlightInit();

				highlight.BeginGeometry(0, PrimitiveType.TriangleList);

				foreach (var corner in corners) {
					HighlightCorner(corner, getColor(corner));
				}

				HighlightCommit();
			}

			public void HighlightBorder(IntRect rectangle, Color color) {

				HighlightInit();

				highlight.BeginGeometry(0, PrimitiveType.LineStrip);

				//Top side
				for (int x = rectangle.Left; x <= rectangle.Right; x++) {
					highlight.DefineVertex(new Vector3(x, map.GetTerrainHeightAt(x, rectangle.Top) + HighlightHeightAboveTerain, rectangle.Top));
					highlight.DefineColor(color);
				}

				//Right side
				for (int y = rectangle.Top; y <= rectangle.Bottom; y++) {
					highlight.DefineVertex(new Vector3(rectangle.Right + 1, map.GetTerrainHeightAt(rectangle.Right + 1, y) + HighlightHeightAboveTerain, y));
					highlight.DefineColor(color);
				}

				//Bottom side
				for (int x = rectangle.Right + 1; x >= rectangle.Left; x--) {
					highlight.DefineVertex(new Vector3(x, map.GetTerrainHeightAt(x, rectangle.Bottom + 1) + HighlightHeightAboveTerain, rectangle.Bottom + 1));
					highlight.DefineColor(color);
				}

				//Left side
				for (int y = rectangle.Bottom + 1; y >= rectangle.Top; y--) {
					highlight.DefineVertex(new Vector3(rectangle.Left, map.GetTerrainHeightAt(rectangle.Left, y) + HighlightHeightAboveTerain, y));
					highlight.DefineColor(color);
				}

				HighlightCommit();
			}

			public void HighlightRectangle(IntRect rectangle, Func<ITile, Color> getColor)
			{
				HighlightInit();

				HighlightFullTileRectangle(rectangle, getColor);

				HighlightCommit();
			}

			public void HighlightTileList(IEnumerable<ITile> tiles, Func<ITile, Color> getColor)
			{
				HighlightInit();


				highlight.BeginGeometry(0, PrimitiveType.TriangleList);

				foreach (var tile in tiles) {
					if (map.IsTileSplitFromTopLeftToBottomRight(tile)) {
						DefineTopLeftBotRightSplitTile(tile, getColor(tile));
					}
					else {
						DefineTopRightBotLeftSplitTile(tile, getColor(tile));
					}
				}

				HighlightCommit();

			}

			public void DisableHighlight() {
				if (highlight != null) {
					highlight.Enabled = false;
				}
			}

			public void Dispose()
			{
				highlight?.Dispose();
				foreach (var chunk in chunks) {
					chunk.Dispose();
				}
				material.Dispose();
			}

			void CreateMaterial() {
				//Count for output image size
				int tileTypeCount = PackageManager.Instance.ActiveGame.TileTypeCount;

				//TODO: Context
				Image mapImage = new Image();

				if (!mapImage.SetSize(Tile.ImageWidth * tileTypeCount, Tile.ImageHeight, 4)) {
					//TODO: Error;
					throw new Exception("Could not set size of the map texture image");
				}

				int mapImageWidth = Tile.ImageWidth * tileTypeCount;
				int mapImageHeight = Tile.ImageHeight;

				IntRect subimageRect = new IntRect(0, 0, Tile.ImageWidth - 1, Tile.ImageHeight - 1);
				foreach (var tileType in PackageManager.Instance.ActiveGame.TileTypes) {
					var tileTypeImage = tileType.GetImage();

					if (tileTypeImage.Compressed) {
						throw new
							NotImplementedException("UrhoSharp does not implement Urho3D decompression of compressed textures");
					}
					else {
						tileTypeImage = tileTypeImage.ConvertToRGBA();
					}
					
					if (!mapImage.SetSubimage(tileTypeImage, subimageRect)) {
						//TODO: Error;
						throw new Exception("Could not copy tileType image to the map texture image");
					}

					tileTypeImage.Dispose();

					Rect uvRect = new Rect(
						new Vector2(subimageRect.Left / (float)mapImageWidth, subimageRect.Top / (float)mapImageHeight),
						new Vector2(subimageRect.Right / (float)mapImageWidth, subimageRect.Bottom / (float)mapImageHeight));
					tileType.SetTextureCoords(uvRect);

					//Increment subImageRect
					subimageRect.Left += Tile.ImageWidth;
					subimageRect.Right += Tile.ImageWidth;
				}

				material = PackageManager.Instance.GetMaterialFromImage(mapImage);
			}

			void CreateModel(LoadingWatcher loadingProgress)
			{
				for (int y = 0; y < numberOfChunks.Y; y++) {
					for (int x = 0; x < numberOfChunks.X; x++) {
						IntVector2 chunkTopLeftCorner = new IntVector2(x * chunkSize.X + map.Left, y * chunkSize.Y + map.Top);
						chunks.Add(new MapChunk(map, this, chunkTopLeftCorner));
					}

					loadingProgress.IncrementProgress(25.0f / numberOfChunks.Y);
				}
			}

			void HighlightFullTileRectangle(IntRect rectangle, Func<ITile, Color> getColor) {
				//I need triangle list because the tiles can be split in two different ways
				// topLeft to bottomRight or topRight to bottomLeft
				highlight.BeginGeometry(0, PrimitiveType.TriangleList);

				map.ForEachInRectangle(rectangle, (tile) => {
													  if (map.IsTileSplitFromTopLeftToBottomRight(tile)) {
														  DefineTopLeftBotRightSplitTile(tile, getColor(tile));
													  }
													  else {
														  DefineTopRightBotLeftSplitTile(tile, getColor(tile));
													  }
												  });
			}

			/// <summary>
			/// Adds highlight of the <paramref name="corner"/>
			/// Works only with <see cref="PrimitiveType.TriangleList"/>
			/// </summary>
			/// <param name="corner">The corner that will be highlighted in <paramref name="color"/></param>
			/// <param name="color">Color of the highlighted rectangle</param>
			void HighlightCorner(IntVector2 corner, Color color)
			{
				//TODO: Make it look nicer
				const float tetrahedronEdgeSize = 0.1f;
				const float tan30 = 0.57735026919f;
				const float tan60 = 1.73205080757f;
				const float tetrahedronHeight = 0.1f;

				const float medianLength = (tetrahedronEdgeSize / 2) * tan60;
				const float topXoffset = tetrahedronEdgeSize / 2;

				//One third of the median, because median is split 1 : 2 by the center of mass
				const float topZoffset = medianLength * (1.0f / 3.0f);
				const float frontZoffset = medianLength * (2.0f / 3.0f);
				//Inverted tetrahedron
				/*
				 *	---------
				 *  \\    / /
				 *   \ \/  /
				 *    \ | /
				 *     \|/
				 */
				// peak will point down
				Vector3 peak = new Vector3(corner.X, map.GetTerrainHeightAt(corner), corner.Y);
				Vector3 topLeft = new Vector3(peak.X - topXoffset,
											peak.Y + tetrahedronHeight,
											peak.Z - topZoffset);
				Vector3 topRight = new Vector3(peak.X + topXoffset,
												peak.Y + tetrahedronHeight,
												peak.Z - topZoffset);
				Vector3 front = new Vector3(peak.X,
											peak.Y + tetrahedronHeight,
											peak.Z + frontZoffset);
				// top base
				DefineHighlightTriangle(topRight, topLeft, front, color);
				// backside
				DefineHighlightTriangle(topLeft, peak, topRight, color);
				// left side
				DefineHighlightTriangle(topRight, peak, front, color);
				// right side
				DefineHighlightTriangle(front, peak, topLeft, color);
			}

			void HighlightInit()
			{
				if (highlight == null) {
					highlight = map.node.CreateComponent<CustomGeometry>();
					var highlightMaterial = new Material();
					highlightMaterial.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitVCol, 1, 1);
					highlight.SetMaterial(highlightMaterial);
				}
				highlight.Enabled = true;
			}

			void HighlightCommit()
			{
				highlight.Commit();
			}

			void DefineTopLeftBotRightSplitTile(ITile tile, Color color)
			{
				DefineHighlightTriangle(tile.TopLeft3,
										tile.BottomLeft3,
										tile.BottomRight3,
										color);

				DefineHighlightTriangle(tile.BottomRight3,
										tile.TopRight3,
										tile.TopLeft3,
										color);
			}

			void DefineTopRightBotLeftSplitTile(ITile tile, Color color) {
				DefineHighlightTriangle(tile.TopRight3,
										tile.TopLeft3,
										tile.BottomLeft3,
										color);

				DefineHighlightTriangle(tile.BottomLeft3,
										tile.BottomRight3,
										tile.TopRight3,
										color);
			}

			/// <summary>
			/// Defines a triangle with color <paramref name="color"/>
			/// <see cref="highlight"/> must be set to <see cref="PrimitiveType.TriangleList"/>
			/// Automatically adds <see cref="HighlightAboveTerrainOffset"/> offset to positions
			/// </summary>
			/// <param name="first"></param>
			/// <param name="second"></param>
			/// <param name="third"></param>
			/// <param name="color"></param>
			void DefineHighlightTriangle(Vector3 first,
										Vector3 second,
										Vector3 third,
										Color color)
			{
				DefineHighlightTriangle(first, second, third, color, color, color);
			}

			/// <summary>
			/// Defines a triangle with vertex colors
			/// <see cref="highlight"/> must be set to <see cref="PrimitiveType.TriangleList"/>
			/// Automatically adds <see cref="HighlightAboveTerrainOffset"/> offset to positions
			/// </summary>
			/// <param name="first"></param>
			/// <param name="second"></param>
			/// <param name="third"></param>
			/// <param name="firstColor"></param>
			/// <param name="secondColor"></param>
			/// <param name="thirdColor"></param>
			void DefineHighlightTriangle(Vector3 first,
										Vector3 second,
										Vector3 third,
										Color firstColor,
										Color secondColor,
										Color thirdColor)
			{
				highlight.DefineVertex(first + HighlightAboveTerrainOffset);
				highlight.DefineColor(firstColor);
				highlight.DefineVertex(second + HighlightAboveTerrainOffset);
				highlight.DefineColor(secondColor);
				highlight.DefineVertex(third + HighlightAboveTerrainOffset);
				highlight.DefineColor(thirdColor);
			}

			MapChunk GetChunk(ITile tile)
			{
				return chunks[GetChunkIndex(tile)];
			}

			MapChunk GetChunk(int x, int y)
			{
				return chunks[GetChunkIndex(x, y)];
			}
			int GetChunkIndex(int x, int y)
			{
				return (x - map.Left) / chunkSize.X + ((y - map.Top) / chunkSize.Y) * numberOfChunks.X;
			}

			int GetChunkIndex(ITile tile)
			{
				return GetChunkIndex(tile.TopLeft.X , tile.TopLeft.Y);
			}
		
		}

	}
	

	
}
