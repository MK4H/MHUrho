using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho;
using Urho.Resources;
using MHUrho.Storage;

namespace MHUrho.WorldMap
{
	public enum HighlightMode { None, Borders, Full }

	public partial class Map {

		private enum BorderType { None, Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight }

		private class MapGraphics : IDisposable {

			private const float HighlightHeightAboveTerain = 0.005f;

			private static Vector3 HighlightAboveTerrainOffset = new Vector3(0, HighlightHeightAboveTerain, 0);


			[StructLayout(LayoutKind.Sequential)]
			struct TileVertex {
				public Vector3 Position;
				public Vector3 Normal;
				public Vector2 TexCoords;

				public TileVertex(Vector3 position, Vector3 normal, Vector2 texCoords) {
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

				public void ChangeTextureCoords(Rect rect) {
					TopLeft.TexCoords = rect.Min;
					TopRight.TexCoords = new Vector2(rect.Max.X, rect.Min.Y);
					BottomLeft.TexCoords = new Vector2(rect.Min.X, rect.Max.Y);
					BottomRight.TexCoords = rect.Max;
				}

				public TileInVB(ITile tile) {
					TopLeft = new TileVertex(new Vector3(tile.MapArea.Left, tile.TopLeftHeight, tile.MapArea.Top),
											 new Vector3(0, 1, 0),
											 new Vector2(tile.Type.TextureCoords.Min.X, tile.Type.TextureCoords.Min.Y));
					TopRight = new TileVertex(new Vector3(tile.MapArea.Right, tile.TopRightHeight, tile.MapArea.Top),
											  new Vector3(0, 1, 0),
											  new Vector2(tile.Type.TextureCoords.Max.X, tile.Type.TextureCoords.Min.Y));
					BottomLeft = new TileVertex(new Vector3(tile.MapArea.Left, tile.BottomLeftHeight, tile.MapArea.Bottom),
												new Vector3(0, 1, 0),
												new Vector2(tile.Type.TextureCoords.Min.X, tile.Type.TextureCoords.Max.Y));
					BottomRight = new TileVertex(new Vector3(tile.MapArea.Right, tile.BottomRightHeight, tile.MapArea.Bottom),
												 new Vector3(0, 1, 0),
												 new Vector2(tile.Type.TextureCoords.Max.X, tile.Type.TextureCoords.Max.Y));
					CalculateLocalNormals();
				}
		   

				/// <summary>
				/// Creates normals just from this tile, disregarding the angle of surrounding tiles
				/// </summary>
				public void CalculateLocalNormals() {
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

				public bool IsTopLeftBotRightDiagHigher() {
					return TopLeft.Position.Y + BottomRight.Position.Y >= TopRight.Position.Y + BottomLeft.Position.Y ;
				}

				public bool IsTopRightBotLeftDiagHigher() {
					return !IsTopLeftBotRightDiagHigher();
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
				private Vector3 AverageNormalNotNormalized(ref Vector3 center,
														   ref Vector3 first,
														   ref Vector3 second,
														   ref Vector3 third) {

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

				private short cornerA1;
				private short middleA;
				private short cornerA2;
				private short cornerB1;
				private short middleB;
				private short cornerB2;

				public unsafe void TestAndRotate(TileInVB * tileInVB) {
					var cornerA1LocalIndex = cornerA1 % 4;
					if ((tileInVB->IsTopLeftBotRightDiagHigher() && (cornerA1LocalIndex == 1 || cornerA1LocalIndex == 2)) ||
						(tileInVB->IsTopRightBotLeftDiagHigher() && (cornerA1LocalIndex == 0 || cornerA1LocalIndex == 3))) {
						//If the diagonal is not the one that is higher, rotate the diagonal
						Rotate();
					}
				}

				public TileInIB(short topLeft, short topRight, short bottomLeft, short bottomRight,ref TileInVB tileInVB ) {
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
				private void Rotate() {
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

			private class CornerTiles : IEnumerable<ITile>,IEnumerator<ITile> {
				public ITile TopLeft;
				public ITile TopRight;
				public ITile BottomLeft;
				public ITile BottomRight;

				private int state = -1;

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

			private Model model;
			private VertexBuffer mapVertexBuffer;
			private IndexBuffer mapIndexBuffer;

			private Material material;

			private HighlightMode highlightMode;
			private CustomGeometry highlight;

			private readonly Map map;
			//TODO: Probably split map into more parts to speed up raycasts and drawing
			private readonly Node mapNode;

			public static MapGraphics Build(Node mapNode,
											Map map,
											ITile[] tiles,
											IntVector2 size) {
				MapGraphics graphics = new MapGraphics(map, mapNode);
				graphics.CreateMaterial();
				graphics.CreateModel(tiles);


				StaticModel model = mapNode.CreateComponent<StaticModel>();
				model.Model = graphics.model;
				model.SetMaterial(graphics.material);

				return graphics;
			}

			protected MapGraphics(Map map, Node mapNode) {
				this.map = map;
				this.mapNode = mapNode;
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
				return rayQueryResult.Node == mapNode
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
				if (rayQueryResult.Node != mapNode) return null;

				IntVector2 corner = new IntVector2((int)Math.Round(rayQueryResult.Position.X),
												   (int)Math.Round(rayQueryResult.Position.Z));
				float height = map.GetTerrainHeightAt(corner);
				return new Vector3(corner.X, height, corner.Y);
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
				//+ [1,1] because i want to change the bottomRight tile to
				// example TL[1,1], BR[3,3], BT-TL = [2,2],but really i want to change 3 by 3
				IntVector2 rectSize = bottomRight - topLeft + new IntVector2(1, 1);
				for (int y = topLeft.Y; y <= bottomRight.Y; y++) {
					int startTileIndex = map.GetTileIndex(topLeft.X, y);
					uint start = (uint)startTileIndex * TileInVB.VerticiesPerTile;
					uint count = (uint)rectSize.X * TileInVB.VerticiesPerTile;

					{
						IntPtr vbPointer = mapVertexBuffer.Lock(start, count);
						if (vbPointer == IntPtr.Zero) {
							//TODO: Error
							throw new Exception("Could not lock tile vertex buffer position to memory to change it");
						}

						unsafe {
							TileInVB* tileInVertexBuffer = (TileInVB*)vbPointer.ToPointer();

							for (int x = 0; x < rectSize.X; x++) {
								tileInVertexBuffer->ChangeTextureCoords(newTileType.TextureCoords);
								tileInVertexBuffer++;
							}
						}
					}

					mapVertexBuffer.Unlock();
				}

			}

		  

			/// <summary>
			/// Changes tile height to the height of the logical tiles
			/// </summary>
			/// <param name="topLeft"></param>
			/// <param name="bottomRight"></param>
			public void CorrectTileHeight(  IntVector2 topLeft, 
											IntVector2 bottomRight) {

				//+ [1,1] because i want to change the bottomRight tile to
				// example TL[1,1], BR[3,3], BT-TL = [2,2],but really i want to change 3 by 3
				IntVector2 rectSize = bottomRight - topLeft + new IntVector2(1, 1);


				for (int y = topLeft.Y; y <= bottomRight.Y; y++) {
					int startTileIndex = map.GetTileIndex(topLeft.X, y);


					{
						IntPtr vbPointer = mapVertexBuffer.Lock((uint)startTileIndex * TileInVB.VerticiesPerTile,
																(uint)rectSize.X * TileInVB.VerticiesPerTile);
						IntPtr ibPointer = mapIndexBuffer.Lock((uint)startTileIndex * TileInIB.IndiciesPerTile,
															   (uint)rectSize.X * TileInIB.IndiciesPerTile);
						if (vbPointer == IntPtr.Zero || ibPointer == IntPtr.Zero) {
							//TODO: Error
							throw new Exception("Could not lock buffer to memory to change it");
						}

						unsafe {
							TileInVB* tileInVertexBuffer = (TileInVB*)vbPointer.ToPointer();
							TileInIB* tileInIndexBuffer = (TileInIB*)ibPointer.ToPointer();

							for (int x = topLeft.X; x <= bottomRight.X; x++) {

								tileInVertexBuffer->TopLeft.Position.Y = map.GetTerrainHeightAt(x, y);
								tileInVertexBuffer->TopRight.Position.Y = map.GetTerrainHeightAt(x + 1, y);
								tileInVertexBuffer->BottomLeft.Position.Y = map.GetTerrainHeightAt(x, y + 1);
								tileInVertexBuffer->BottomRight.Position.Y = map.GetTerrainHeightAt(x + 1, y + 1);

								tileInVertexBuffer->CalculateLocalNormals();
								tileInIndexBuffer->TestAndRotate(tileInVertexBuffer);

								tileInVertexBuffer++;
								tileInIndexBuffer++;
							}
						}
					}

					mapVertexBuffer.Unlock();
					mapIndexBuffer.Unlock();
				}

			}

			public void ChangeCornerHeights(List<IntVector2> cornerPositions, float heightDelta) {
				//TODO: Lock just the needed part
				IntPtr vbPointer = mapVertexBuffer.Lock(0, (uint)map.tiles.Length * TileInVB.VerticiesPerTile);
				IntPtr ibPointer = mapIndexBuffer.Lock(0, (uint) map.tiles.Length * TileInIB.IndiciesPerTile);
				if (vbPointer == IntPtr.Zero || ibPointer == IntPtr.Zero) {
					//TODO: Error
					throw new Exception("Could not lock buffer to memory to change it");
				}

				unsafe {
					TileInVB* vbBasePointer = (TileInVB*)vbPointer.ToPointer();
					TileInIB* ibBasePointer = (TileInIB*) ibPointer.ToPointer();

					List<CornerTiles> changedCorners = new List<CornerTiles>(cornerPositions.Count); 

					foreach (var corner in cornerPositions) {
						ChangeCornerHeight(vbBasePointer, corner, heightDelta, changedCorners);
					}

					foreach (var changedCorner in changedCorners) {
						foreach (var tile in changedCorner) {
							var vbTile = vbBasePointer + map.GetTileIndex(tile);
							var ibTile = ibBasePointer + map.GetTileIndex(tile);
							vbTile->CalculateLocalNormals();
							ibTile->TestAndRotate(vbTile);
						}
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

				mapVertexBuffer.Unlock();
				mapIndexBuffer.Unlock();
			}

			public void HighlightArea(IntRect rectangle, HighlightMode mode, Color color) {

				if (highlight == null) {
					highlight = mapNode.CreateComponent<CustomGeometry>();
					var highlightMaterial = new Material();
					highlightMaterial.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitVCol, 1, 1);
					highlight.SetMaterial(highlightMaterial);
				}
				highlight.Enabled = true;


				switch (mode) {
					case HighlightMode.None:
						DisableHighlight();
						break;
					case HighlightMode.Borders:
						HighlightBorder(rectangle, color);
						break;
					case HighlightMode.Full:
						HighlightFullRectangle(rectangle, color);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown Highlight mode");
				}


				highlight.Commit();
			}

			public void DisableHighlight() {
				if (highlight != null) {
					highlight.Enabled = false;
				}
			}

			public void Dispose() {
				model.Dispose();
				material.Dispose();
			}

			private void CreateMaterial() {
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

				material = Material.FromImage(mapImage);
			}

			private void CreateModel(ITile[] tiles) {

				//4 verticies for every tile, so that we can map every tile to different texture
				// and the same tile types to the same textures
				uint numVerticies = (uint)(map.WidthWithBorders * map.LengthWithBorders * 4);
				//TODO: maybe connect the neighbouring verticies
				//two triangles per tile, 3 indicies per triangle
				uint numIndicies = (uint)(map.WidthWithBorders * map.LengthWithBorders * 6);

				Model model = new Model();

				//TODO: Context
				VertexBuffer vb = new VertexBuffer(Application.CurrentContext, false);
				IndexBuffer ib = new IndexBuffer(Application.CurrentContext, false);

				vb.Shadowed = true;
				vb.SetSize(numVerticies, ElementMask.Position | ElementMask.Normal | ElementMask.TexCoord1, false);

				ib.Shadowed = true;
				ib.SetSize(numIndicies, false, false);

				IntPtr vbPointer = vb.Lock(0, numVerticies);
				IntPtr ibPointer = ib.Lock(0, numIndicies);

				if (vbPointer == IntPtr.Zero || ibPointer == IntPtr.Zero) {
					//TODO: Error, could not lock buffers into memory, cannot create map
					throw new Exception("Could not lock buffer into memory for map model creation");
				}

				unsafe {
					TileInVB* verBuff = (TileInVB*)vbPointer.ToPointer();
					TileInIB* inBuff = (TileInIB*)ibPointer.ToPointer();

					int vertexIndex = 0;
					foreach (var tile in tiles) {
						var tileInVB = new TileInVB(tile);

						//Create verticies
						* (verBuff++) = tileInVB;

						//Connect verticies to triangles        
						*(inBuff++) = new TileInIB((short) (vertexIndex + 0),
												   (short) (vertexIndex + 1),
												   (short) (vertexIndex + 2),
												   (short) (vertexIndex + 3),
												   ref tileInVB);

						vertexIndex += TileInVB.VerticiesPerTile;
					}

				}

				vb.Unlock();
				ib.Unlock();

				Geometry geom = new Geometry();
				geom.SetVertexBuffer(0, vb);
				geom.IndexBuffer = ib;
				geom.SetDrawRange(PrimitiveType.TriangleList, 0, numIndicies, true);

				model.NumGeometries = 1;
				model.SetGeometry(0, 0, geom);
				model.BoundingBox = new BoundingBox(new Vector3(0, 0, 0), new Vector3(map.WidthWithBorders, 1, map.LengthWithBorders));

				this.model = model;
				this.mapVertexBuffer = vb;
				this.mapIndexBuffer = ib;
			}

			private unsafe void ChangeCornerHeight(TileInVB* vertexBufferBase, 
												   IntVector2 cornerPosition, 
												   float heightDelta, 
												   List<CornerTiles> changedCorners) {

				CornerTiles cornerTiles = new CornerTiles();

				if ((cornerTiles.TopLeft = map.TileByTopLeftCorner(cornerPosition, true)) != null) {
					var topLeftTileInVB = (vertexBufferBase + map.GetTileIndex(cornerTiles.TopLeft));
					topLeftTileInVB->TopLeft.Position.Y += heightDelta;
				}

				if ((cornerTiles.TopRight = map.TileByTopRightCorner(cornerPosition, true)) != null) {
					var topRightTileInVB = (vertexBufferBase + map.GetTileIndex(cornerTiles.TopRight));
					topRightTileInVB->TopRight.Position.Y += heightDelta;
				}

				if ((cornerTiles.BottomLeft = map.TileByBottomLeftCorner(cornerPosition, true)) != null) {
					var bottomLeftTileInVB = (vertexBufferBase + map.GetTileIndex(cornerTiles.BottomLeft));
					bottomLeftTileInVB->BottomLeft.Position.Y += heightDelta;
				}

				if ((cornerTiles.BottomRight = map.TileByBottomRightCorner(cornerPosition, true)) != null) {
					var bottomRightTileInVB = (vertexBufferBase + map.GetTileIndex(cornerTiles.BottomRight));
					bottomRightTileInVB->BottomRight.Position.Y += heightDelta;
				}

				changedCorners.Add(cornerTiles);
			}

			private void HighlightBorder(IntRect rectangle, Color color) {
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
			}

			private void HighlightFullRectangle(IntRect rectangle, Color color) {
				//I need triangle list because the tiles can be split in two different ways
				// topLeft to bottomRight or topRight to bottomLeft
				highlight.BeginGeometry(0, PrimitiveType.TriangleList);

				map.ForEachInRectangle(rectangle, (tile) => {
													  if (map.IsTileSplitFromTopLeftToBottomRight(tile)) {
														  DefineTopLeftBotRightSplitTile(highlight, tile, color);
													  }
													  else {
														  DefineTopRightBotLeftSplitTile(highlight, tile, color);
													  }
												  });
			}

			private void DefineTopLeftBotRightSplitTile(CustomGeometry geometry, ITile tile, Color color) {
				geometry.DefineVertex(tile.TopLeft3 + HighlightAboveTerrainOffset);
				geometry.DefineColor(color);
				geometry.DefineVertex(tile.BottomLeft3 + HighlightAboveTerrainOffset);
				geometry.DefineColor(color);
				geometry.DefineVertex(tile.BottomRight3 + HighlightAboveTerrainOffset);
				geometry.DefineColor(color);

				geometry.DefineVertex(tile.BottomRight3 + HighlightAboveTerrainOffset);
				geometry.DefineColor(color);
				geometry.DefineVertex(tile.TopRight3 + HighlightAboveTerrainOffset);
				geometry.DefineColor(color);
				geometry.DefineVertex(tile.TopLeft3 + HighlightAboveTerrainOffset);
				geometry.DefineColor(color);
			}

			private void DefineTopRightBotLeftSplitTile(CustomGeometry geometry, ITile tile, Color color) {
				geometry.DefineVertex(tile.TopRight3 + HighlightAboveTerrainOffset);
				geometry.DefineColor(color);
				geometry.DefineVertex(tile.TopLeft3 + HighlightAboveTerrainOffset);
				geometry.DefineColor(color);
				geometry.DefineVertex(tile.BottomLeft3 + HighlightAboveTerrainOffset);
				geometry.DefineColor(color);

				geometry.DefineVertex(tile.BottomLeft3 + HighlightAboveTerrainOffset);
				geometry.DefineColor(color);
				geometry.DefineVertex(tile.BottomRight3 + HighlightAboveTerrainOffset);
				geometry.DefineColor(color);
				geometry.DefineVertex(tile.TopRight3 + HighlightAboveTerrainOffset);
				geometry.DefineColor(color);
			}
		}

	}
	

	
}
