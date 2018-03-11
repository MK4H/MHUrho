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
    public partial class Map {

        private enum BorderType { None, Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight }

        private class MapGraphics : IDisposable {

            private const float HighlightHeightAboveTerain = 0.005f;

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

                private TileInVB(ITile tile) {
                    TopLeft = new TileVertex(new Vector3(tile.MapArea.Left, 0, tile.MapArea.Top),
                                             new Vector3(0, 1, 0),
                                             new Vector2(tile.Type.TextureCoords.Min.X, tile.Type.TextureCoords.Min.Y));
                    TopRight = new TileVertex(new Vector3(tile.MapArea.Right, 0, tile.MapArea.Top),
                                                new Vector3(0, 1, 0),
                                                new Vector2(tile.Type.TextureCoords.Max.X, tile.Type.TextureCoords.Min.Y));
                    BottomLeft = new TileVertex(new Vector3(tile.MapArea.Left, 0, tile.MapArea.Bottom),
                                                new Vector3(0, 1, 0),
                                                new Vector2(tile.Type.TextureCoords.Min.X, tile.Type.TextureCoords.Max.Y));
                    BottomRight = new TileVertex(new Vector3(tile.MapArea.Right, 0, tile.MapArea.Bottom),
                                                 new Vector3(0, 1, 0),
                                                 new Vector2(tile.Type.TextureCoords.Max.X, tile.Type.TextureCoords.Max.Y));
                }

                private TileInVB(ITile tile,
                                 float topLeftHeight,
                                 float topRightHeight,
                                 float botLeftHeight,
                                 float botRightHeight) {

                    TopLeft = new TileVertex(new Vector3(tile.MapArea.Left, topLeftHeight, tile.MapArea.Top),
                                             new Vector3(0, 1, 0),
                                             new Vector2(tile.Type.TextureCoords.Min.X, tile.Type.TextureCoords.Min.Y));
                    TopRight = new TileVertex(new Vector3(tile.MapArea.Right, topRightHeight, tile.MapArea.Top),
                                              new Vector3(0, 1, 0),
                                              new Vector2(tile.Type.TextureCoords.Max.X, tile.Type.TextureCoords.Min.Y));
                    BottomLeft = new TileVertex(new Vector3(tile.MapArea.Left, botLeftHeight, tile.MapArea.Bottom),
                                                new Vector3(0, 1, 0),
                                                new Vector2(tile.Type.TextureCoords.Min.X, tile.Type.TextureCoords.Max.Y));
                    BottomRight = new TileVertex(new Vector3(tile.MapArea.Right, botRightHeight, tile.MapArea.Bottom),
                                                 new Vector3(0, 1, 0),
                                                 new Vector2(tile.Type.TextureCoords.Max.X, tile.Type.TextureCoords.Max.Y));

                    CalculateLocalNormals();
                }

                public static TileInVB InnerTile(ITile[] tiles, int index, int rowSize) {
                    return new TileInVB(tile: tiles[index],
                                        topLeftHeight: tiles[index].Height,
                                        topRightHeight: tiles[index + 1].Height,
                                        botLeftHeight: tiles[index + rowSize].Height,
                                        botRightHeight: tiles[index + rowSize + 1].Height);
                }

                public static TileInVB BorderTile(BorderTile borderTile) {
                    return new TileInVB(tile: borderTile,
                                        topLeftHeight: borderTile.TopLeftHeight,
                                        topRightHeight: borderTile.TopRightHeight,
                                        botLeftHeight: borderTile.BotLeftHeight,
                                        botRightHeight: borderTile.BotRightHeight);
                }

                

                /// <summary>
                /// Creates normals just from this tile, disregarding the angle of surrounding tiles
                /// </summary>
                public void CalculateLocalNormals() {
                    TopLeft.Normal = Vector3.Cross(BottomLeft.Position - TopLeft.Position,
                                                   TopRight.Position - TopLeft.Position);
                    TopRight.Normal = Vector3.Cross(TopLeft.Position - TopRight.Position,
                                                    BottomRight.Position - TopRight.Position);
                    BottomLeft.Normal = Vector3.Cross(BottomRight.Position - BottomLeft.Position,
                                                      TopLeft.Position - BottomLeft.Position);
                    BottomRight.Normal = Vector3.Cross(TopRight.Position - BottomRight.Position,
                                                       BottomLeft.Position - BottomRight.Position);
                    TopLeft.Normal.Normalize();
                    TopRight.Normal.Normalize();
                    BottomLeft.Normal.Normalize();
                    BottomRight.Normal.Normalize();
                }

            }


            private class CornerTiles : IEnumerable<ITile>,IEnumerator<ITile> {
                public ITile TopLeft;
                public ITile TopRight;
                public ITile BottomLeft;
                public ITile BottomRight;

                private int state = -1;

                public IEnumerator<ITile> GetEnumerator() {
                    state = 0;
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

            private Material material;
            private CustomGeometry highlight;

            private readonly Map map;
            //TODO: Probably split map into more parts to speed up raycasts and drawing
            private readonly Node mapNode;

            private bool smoothing;

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
                    if (rayQueryResult.Node == mapNode) {
                        return map.GetTile((int)Math.Floor(rayQueryResult.Position.X), (int)Math.Floor(rayQueryResult.Position.Z));
                    }
                }
                return null;
            }

            public Vector3? RaycastToVertex(List<RayQueryResult> rayQueryResults) {
                foreach (var rayQueryResult in rayQueryResults) {
                    if (rayQueryResult.Node == mapNode) {
                        IntVector2 corner = new IntVector2((int) Math.Round(rayQueryResult.Position.X),
                                                           (int) Math.Round(rayQueryResult.Position.Z));
                        float height = map.GetHeightAt(corner);
                        return new Vector3(corner.X, height, corner.Y);
                    }
                }
                return null;
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

            public void ChangeCornerHeights(List<IntVector2> cornerPositions, float heightDelta) {
                //TODO: Lock just the needed part
                IntPtr vbPointer = mapVertexBuffer.Lock(0, (uint)map.tiles.Length * TileInVB.VerticiesPerTile);
                if (vbPointer == IntPtr.Zero) {
                    //TODO: Error
                    throw new Exception("Could not lock tile vertex buffer position to memory to change it");
                }

                unsafe {
                    TileInVB* basePointer = (TileInVB*)vbPointer.ToPointer();

                    List<CornerTiles> changedCorners = new List<CornerTiles>(cornerPositions.Count); 

                    foreach (var corner in cornerPositions) {
                        ChangeCornerHeight(basePointer, corner, heightDelta, changedCorners);
                    }

                    foreach (var changedCorner in changedCorners) {
                        foreach (var tile in changedCorner) {
                            var vbTile = basePointer + map.GetTileIndex(tile);
                            vbTile->CalculateLocalNormals();
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
            }

            public void HighlightArea(IntRect rectangle) {

                if (highlight == null) {
                    highlight = mapNode.CreateComponent<CustomGeometry>();
                    var highlightMaterial = new Material();
                    highlightMaterial.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitVCol, 1, 1);
                    highlight.SetMaterial(highlightMaterial);
                }
                highlight.Enabled = true;

                highlight.BeginGeometry(0, PrimitiveType.LineStrip);

                //Top side
                for (int x = rectangle.Left; x <= rectangle.Right; x++) {
                    highlight.DefineVertex(new Vector3(x, map.GetHeightAt(x, rectangle.Top) + HighlightHeightAboveTerain, rectangle.Top));
                    highlight.DefineColor(Color.Green);
                }

                //Right side
                for (int y = rectangle.Top; y <= rectangle.Bottom; y++) {
                    highlight.DefineVertex(new Vector3(rectangle.Right + 1, map.GetHeightAt(rectangle.Right + 1, y) + HighlightHeightAboveTerain, y));
                    highlight.DefineColor(Color.Green);
                }

                //Bottom side
                for (int x = rectangle.Right + 1; x >= rectangle.Left; x--) {
                    highlight.DefineVertex(new Vector3(x, map.GetHeightAt(x, rectangle.Bottom + 1) + HighlightHeightAboveTerain, rectangle.Bottom + 1));
                    highlight.DefineColor(Color.Green);
                }

                //Left side
                for (int y = rectangle.Bottom + 1; y >= rectangle.Top; y--) {
                    highlight.DefineVertex(new Vector3(rectangle.Left, map.GetHeightAt(rectangle.Left, y) + HighlightHeightAboveTerain, y));
                    highlight.DefineColor(Color.Green);
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
                int tileTypeCount = PackageManager.Instance.TileTypeCount;

                //TODO: Context
                Image mapImage = new Image();

                if (!mapImage.SetSize(Tile.ImageWidth * tileTypeCount, Tile.ImageHeight, 4)) {
                    //TODO: Error;
                    throw new Exception("Could not set size of the map texture image");
                }

                int mapImageWidth = Tile.ImageWidth * tileTypeCount;
                int mapImageHeight = Tile.ImageHeight;

                IntRect subimageRect = new IntRect(0, 0, Tile.ImageWidth - 1, Tile.ImageHeight - 1);
                foreach (var tileType in PackageManager.Instance.TileTypes) {
                    var tileTypeImage = tileType.GetImage().ConvertToRGBA();
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
                    short* inBuff = (short*)ibPointer.ToPointer();

                    int vertexIndex = 0;
                    for (int i = 0; i < tiles.Length; i++) {


                        ITile tile = tiles[i];
                        TileInVB val;

                        if (map.IsBorder(tile.Location.X, tile.Location.Y)) {
                            var borderTile = (BorderTile)tile;
                            if (borderTile.BorderType == BorderType.None) {
                                throw new Exception("Implementation error, value should not be None");
                            }

                            val = TileInVB.BorderTile(borderTile);
                        }
                        else {
                            val = TileInVB.InnerTile(tiles, i, map.WidthWithBorders);
                        }

                        //Create verticies
                        *(verBuff++) = val;

                        //Connect verticies to triangles                 
                        *(inBuff++) = (short)(vertexIndex + 2);
                        *(inBuff++) = (short)(vertexIndex + 3);
                        *(inBuff++) = (short)(vertexIndex + 1);

                        *(inBuff++) = (short)(vertexIndex + 1);
                        *(inBuff++) = (short)(vertexIndex + 0);
                        *(inBuff++) = (short)(vertexIndex + 2);

                        vertexIndex += 4;
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


            private unsafe void CalculateSmoothNormals(TileInVB* vertexBufferBase, IntVector2 corner) {
                ITile tile;
                TileInVB* topLeftTile = null, topRightTile = null, bottomLeftTile = null, bottomRightTile = null;
                Vector3 normal = new Vector3();
                if ((tile = map.TileByTopLeftCorner(corner, true)) != null) {
                    topLeftTile = (vertexBufferBase + map.GetTileIndex(tile));
                    normal += topLeftTile->TopLeft.Normal;
                }

                if ((tile = map.TileByTopRightCorner(corner, true)) != null) {
                    topRightTile = (vertexBufferBase + map.GetTileIndex(tile));
                    normal += topRightTile->TopRight.Normal;
                }

                if ((tile = map.TileByBottomLeftCorner(corner, true)) != null) {
                    bottomLeftTile = (vertexBufferBase + map.GetTileIndex(tile));
                    normal += bottomLeftTile->BottomLeft.Normal;
                }

                if ((tile = map.TileByBottomRightCorner(corner, true)) != null) {
                    bottomRightTile = (vertexBufferBase + map.GetTileIndex(tile));
                    normal += bottomRightTile->BottomRight.Normal;
                }

                normal.Normalize();
                if (topLeftTile != null) topLeftTile->TopLeft.Normal = normal;
                if (topRightTile != null) topRightTile->TopRight.Normal = normal;
                if (bottomLeftTile != null) bottomLeftTile->BottomLeft.Normal = normal;
                if (bottomRightTile != null) bottomRightTile->BottomRight.Normal = normal;
            }
        }

    }
    

    
}
