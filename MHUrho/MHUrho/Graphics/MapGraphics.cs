﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho;
using Urho.Resources;

namespace MHUrho.Graphics
{
    public delegate bool IsBorder(int x, int y);

    public class MapGraphics : IDisposable {

        internal enum BorderType { Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight }

        internal interface BorderTile {
            float TopLeftHeight { get; }
            float TopRightHeight { get; }
            float BotLeftHeight { get; }
            float BotRightHeight { get; }

            BorderType BorderType { get; }
        }

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
                TopRight = new TileVertex(  new Vector3(tile.MapArea.Right, 0, tile.MapArea.Top),
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

                LocalNormals();
            }

            public static TileInVB InnerTile(ITile[] tiles, int index, int rowSize) {
                return new TileInVB(tile: tiles[index],
                                    topLeftHeight: tiles[index].Height,
                                    topRightHeight: tiles[index + 1].Height,
                                    botLeftHeight: tiles[index + rowSize].Height,
                                    botRightHeight: tiles[index + rowSize + 1].Height);
            }

            public static TileInVB TopBorder(ITile[] tiles, int index, int rowSize, float borderHeight) {
                return new TileInVB(tile: tiles[index],
                                    topLeftHeight: borderHeight,
                                    topRightHeight: borderHeight,
                                    botLeftHeight: tiles[index + rowSize].Height,
                                    botRightHeight: tiles[index + rowSize + 1].Height);
            }

            public static TileInVB BottomBorder(ITile[] tiles, int index, int rowSize, float borderHeight) {
                return new TileInVB(tile: tiles[index],
                                    topLeftHeight: tiles[index].Height,
                                    topRightHeight: tiles[index + 1].Height,
                                    botLeftHeight: borderHeight,
                                    botRightHeight: borderHeight);
            }

            public static TileInVB LeftBorder(ITile[] tiles, int index, int rowSize, float borderHeight) {
                return new TileInVB(tile: tiles[index],
                                    topLeftHeight: borderHeight,
                                    topRightHeight: tiles[index + 1].Height,
                                    botLeftHeight: borderHeight,
                                    botRightHeight: tiles[index + rowSize + 1].Height);
            }

            public static TileInVB RightBorder(ITile[] tiles, int index, int rowSize, float borderHeight) {
                return new TileInVB(tile: tiles[index],
                                    topLeftHeight: tiles[index].Height,
                                    topRightHeight: borderHeight,
                                    botLeftHeight: tiles[index + rowSize].Height,
                                    botRightHeight: borderHeight);
            }

            public static TileInVB TopLeftCorner(ITile[] tiles, int index, int rowSize, float borderHeight) {
                return new TileInVB(tile: tiles[index],
                                    topLeftHeight: borderHeight,
                                    topRightHeight: borderHeight,
                                    botLeftHeight: borderHeight,
                                    botRightHeight: tiles[index + rowSize + 1].Height);
            }

            public static TileInVB TopRightCorner(ITile[] tiles, int index, int rowSize, float borderHeight) {
                return new TileInVB(tile: tiles[index],
                                    topLeftHeight: borderHeight,
                                    topRightHeight: borderHeight,
                                    botLeftHeight: tiles[index + rowSize].Height,
                                    botRightHeight: borderHeight);
            }

            public static TileInVB BotLeftCorner(ITile[] tiles, int index, int rowSize, float borderHeight) {
                return new TileInVB(tile: tiles[index],
                                    topLeftHeight: borderHeight,
                                    topRightHeight: tiles[index + 1].Height,
                                    botLeftHeight: borderHeight,
                                    botRightHeight: borderHeight);
            }

            public static TileInVB BotRightCorner(ITile[] tiles, int index, int rowSize, float borderHeight) {
                return new TileInVB(tile: tiles[index],
                                    topLeftHeight: tiles[index].Height,
                                    topRightHeight: borderHeight,
                                    botLeftHeight: borderHeight,
                                    botRightHeight: borderHeight);
            }


            /// <summary>
            /// Creates normals just from this tile, disregarding the angle of surrounding tiles
            /// </summary>
            private void LocalNormals() {
                TopLeft.Normal = Vector3.Cross(BottomLeft.Position - TopLeft.Position,
                                               TopRight.Position - TopLeft.Position);
                TopRight.Normal = Vector3.Cross(BottomRight.Position - TopRight.Position,
                                                TopLeft.Position - TopRight.Position);
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
                                        IntVector2 size,
                                        float borderHeight,
                                        IsBorder isBorder,
                                        IsBorder isTopBorder,
                                        IsBorder isBottomBorder,
                                        IsBorder isLeftBorder,
                                        IsBorder isRightBorder) {
            MapGraphics graphics = new MapGraphics(map, mapNode);
            graphics.CreateMaterial();
            graphics.CreateModel(tiles, size, borderHeight, isBorder, isTopBorder, isBottomBorder, isLeftBorder, isRightBorder);


            StaticModel model = mapNode.CreateComponent<StaticModel>();
            model.Model = graphics.model;
            model.SetMaterial(graphics.material);

            return graphics;
        }

        protected MapGraphics(Map map, Node mapNode) {
            this.map = map;
            this.mapNode = mapNode;
        }

        public ITile HandleRaycast(RayQueryResult rayQueryResult) {
            if (rayQueryResult.Node == mapNode) {
                return map.GetTile((int)Math.Floor(rayQueryResult.Position.X), (int)Math.Floor(rayQueryResult.Position.Z));
            }
            return null;
        }

        public void ChangeTileType(IntVector2 location, TileType newTileType) {
            ChangeTileType(location, newTileType, new IntVector2(1, 1));
        }

        /// <summary>
        /// Changes whole rectangle of tiles to <paramref name="newTileType"/>
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="newTileType"></param>
        /// <param name="rectangleSize"></param>
        public void ChangeTileType(IntVector2 topLeft, TileType newTileType, IntVector2 rectangleSize) {

            for (int y = topLeft.Y; y < topLeft.Y + rectangleSize.Y; y++) {
                int startTileIndex = GetTileIndex(topLeft.X, y);
                uint start = (uint) startTileIndex * TileInVB.VerticiesPerTile;
                uint count = (uint) rectangleSize.X * TileInVB.VerticiesPerTile;

                {
                    IntPtr vbPointer = mapVertexBuffer.Lock(start, count);
                    if (vbPointer == IntPtr.Zero) {
                        //TODO: Error
                        throw new Exception("Could not lock tile vertex buffer position to memory to change it");
                    }

                    unsafe {
                        TileInVB* tileInVertexBuffer = (TileInVB*)vbPointer.ToPointer();

                        for (int x = 0; x < rectangleSize.X; x++) {
                            tileInVertexBuffer->ChangeTextureCoords(newTileType.TextureCoords);
                            tileInVertexBuffer++;
                        }
                    }
                }
                
                mapVertexBuffer.Unlock();
            }
            
        }
        /// <summary>
        /// Changes corner height of the specified corner of all four neighbouring tiles
        /// </summary>
        /// <param name="cornerPosition">Position of a corner iside a map or on the border of the map</param>
        /// <param name="newHeight">New height of the corner</param>
        public void ChangeCornerHeight(IntVector2 cornerPosition, float newHeight) {

            if (!map.IsInside(cornerPosition)) {
                throw new ArgumentException("argument is outside of the map", nameof(cornerPosition));
            }

            if (map.IsInside(cornerPosition.X - 1, cornerPosition.Y - 1)) {
                //Everything is inside
                int start = GetTileIndex(cornerPosition.X - 1,cornerPosition.Y - 1) * TileInVB.VerticiesPerTile;

                
                {
                    //TODO: Maybe lock the whole needed part at once
                    //First two tiles, changing bottomRight corner and bottomLeft corner
                    IntPtr vbPointer = mapVertexBuffer.Lock((uint)start, TileInVB.VerticiesPerTile * 2);
                    if (vbPointer == IntPtr.Zero) {
                        //TODO: Error
                        throw new Exception("Could not lock tile vertex buffer position to memory to change it");
                    }

                    unsafe {
                        //TODO: Recalculate normals
                        TileInVB* tileInVertexBuffer = (TileInVB*)vbPointer.ToPointer();
                        tileInVertexBuffer->BottomRight.Position.Y = newHeight;
                        tileInVertexBuffer++;
                        tileInVertexBuffer->BottomLeft.Position.Y = newHeight;
                    }

                    mapVertexBuffer.Unlock();
                    start = GetTileIndex(cornerPosition.X - 1, cornerPosition.Y);

                    vbPointer = mapVertexBuffer.Lock((uint)start, TileInVB.VerticiesPerTile * 2);
                    if (vbPointer == IntPtr.Zero) {
                        //TODO: Error
                        throw new Exception("Could not lock tile vertex buffer position to memory to change it");
                    }

                    unsafe {
                        //TODO: Recalculate normals
                        TileInVB* tileInVertexBuffer = (TileInVB*)vbPointer.ToPointer();
                        tileInVertexBuffer->TopRight.Position.Y = newHeight;
                        tileInVertexBuffer++;
                        tileInVertexBuffer->TopLeft.Position.Y = newHeight;
                    }

                    mapVertexBuffer.Unlock();
                }
            }
            else if (map.IsInside(cornerPosition.X, cornerPosition.Y - 1)) {
                //We are changing left border
                {
                    //Lock just one tile
                    int start = GetTileIndex(cornerPosition.X, cornerPosition.Y - 1);
                    IntPtr vbPointer = mapVertexBuffer.Lock((uint)start, TileInVB.VerticiesPerTile);
                    if (vbPointer == IntPtr.Zero) {
                        //TODO: Error
                        throw new Exception("Could not lock tile vertex buffer position to memory to change it");
                    }

                    unsafe {
                        //TODO: Recalculate normals
                        TileInVB* tileInVertexBuffer = (TileInVB*)vbPointer.ToPointer();
                        tileInVertexBuffer->BottomRight.Position.Y = newHeight;
                        tileInVertexBuffer++;
                        tileInVertexBuffer->BottomLeft.Position.Y = newHeight;
                    }

                    mapVertexBuffer.Unlock();
                    start = GetTileIndex(cornerPosition.X - 1, cornerPosition.Y);

                    vbPointer = mapVertexBuffer.Lock((uint)start, TileInVB.VerticiesPerTile * 2);
                    if (vbPointer == IntPtr.Zero) {
                        //TODO: Error
                        throw new Exception("Could not lock tile vertex buffer position to memory to change it");
                    }

                    unsafe {
                        //TODO: Recalculate normals
                        TileInVB* tileInVertexBuffer = (TileInVB*)vbPointer.ToPointer();
                        tileInVertexBuffer->TopRight.Position.Y = newHeight;
                        tileInVertexBuffer++;
                        tileInVertexBuffer->TopLeft.Position.Y = newHeight;
                    }

                    mapVertexBuffer.Unlock();
                }
            }
            

            
            
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

        public void HideHighlight() {
            highlight.Enabled = false;
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

        private void CreateModel(ITile[] tiles, 
                                 IntVector2 size,
                                 float borderHeight,
                                 IsBorder isBorder,
                                 IsBorder isTopBorder,
                                 IsBorder isBottomBorder,
                                 IsBorder isLeftBorder,
                                 IsBorder isRightBorder) {

            //4 verticies for every tile, so that we can map every tile to different texture
            // and the same tile types to the same textures
            uint numVerticies = (uint)(size.X * size.Y * 4);
            //TODO: maybe connect the neighbouring verticies
            //two triangles per tile, 3 indicies per triangle
            uint numIndicies = (uint)(size.X * size.Y * 6);

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

                    if (isBorder(tile.Location.X, tile.Location.Y)) {
                        if (isLeftBorder(tile.Location.X, tile.Location.Y)) {
                            if (isTopBorder(tile.Location.X, tile.Location.Y)) {
                                val = TileInVB.TopLeftCorner(tiles, i, size.X, borderHeight);
                            }
                            else if (isBottomBorder(tile.Location.X, tile.Location.Y)) {
                                val = TileInVB.BotLeftCorner(tiles, i, size.X, borderHeight);
                            }
                            else {
                                val = TileInVB.LeftBorder(tiles, i, size.X, borderHeight);
                            }
                        }
                        else if (isRightBorder(tile.Location.X, tile.Location.Y)) {
                            if (isTopBorder(tile.Location.X, tile.Location.Y)) {
                                val = TileInVB.TopRightCorner(tiles, i, size.X, borderHeight);
                            }
                            else if (isBottomBorder(tile.Location.X, tile.Location.Y)) {
                                val = TileInVB.BotRightCorner(tiles, i, size.X, borderHeight);
                            }
                            else {
                                val = TileInVB.RightBorder(tiles, i, size.X, borderHeight);
                            }
                        }
                        else if (isTopBorder(tile.Location.X, tile.Location.Y)) {
                            val = TileInVB.TopBorder(tiles, i, size.X, borderHeight);
                        }
                        else { //BottomBorder
                            val = TileInVB.BottomBorder(tiles, i, size.X, borderHeight);
                        }
                    }
                    else {
                        val = TileInVB.InnerTile(tiles, i, size.X);
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
            model.BoundingBox = new BoundingBox(new Vector3(0, 0, 0), new Vector3(size.X, 1, size.Y));

            this.model = model;
            this.mapVertexBuffer = vb;
        }

        private int GetTileIndex(int x, int y) {
            return x + map.Width * y;
        }

        private int GetTileIndex(IntVector2 position) {
            return GetTileIndex(position.X, position.Y);
        }
    }
}
