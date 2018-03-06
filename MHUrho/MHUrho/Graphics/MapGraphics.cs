using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho;
using Urho.Resources;

namespace MHUrho.Graphics
{
    public class MapGraphics : IDisposable {

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
        }


        private Model model;
        private VertexBuffer mapVertexBuffer;

        private Material material;
        private CustomGeometry highlight;

        private readonly Map map;
        //TODO: Probably split map into more parts to speed up raycasts and drawing
        private readonly Node mapNode;

        public static MapGraphics Build(Node mapNode, Map map, ITile[] tiles, IntVector2 size) {
            MapGraphics graphics = new MapGraphics(map, tiles, mapNode);
            graphics.CreateMaterial();
            graphics.CreateModel(tiles, size);


            StaticModel model = mapNode.CreateComponent<StaticModel>();
            model.Model = graphics.model;
            model.SetMaterial(graphics.material);

            return graphics;
        }

        protected MapGraphics(Map map, ITile[] tiles, Node mapNode) {
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
                int startTileIndex = topLeft.X + y * map.Width;
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

        public void HighlightArea(IntRect rectangle) {

            if (highlight == null) {
                highlight = mapNode.CreateComponent<CustomGeometry>();
                var highlightMaterial = new Material();
                highlightMaterial.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitVCol, 1, 1);
                highlight.SetMaterial(highlightMaterial);
            }


            highlight.BeginGeometry(0, PrimitiveType.LineStrip);
            
            //Top side
            for (int x = rectangle.Left; x <= rectangle.Right; x++) {
                highlight.DefineVertex(new Vector3(x, map.GetHeightAt(x, rectangle.Top) + 0.1f, rectangle.Top));
                highlight.DefineColor(Color.Green);
            }

            //Right side
            for (int y = rectangle.Top; y <= rectangle.Bottom; y++) {
                highlight.DefineVertex(new Vector3(rectangle.Right + 1, map.GetHeightAt(rectangle.Right + 1, y) + 0.1f, y));
                highlight.DefineColor(Color.Green);
            }

            //Bottom side
            for (int x = rectangle.Right + 1; x >= rectangle.Left; x--) {
                highlight.DefineVertex(new Vector3(x, map.GetHeightAt(x, rectangle.Bottom + 1) + 0.1f, rectangle.Bottom + 1));
                highlight.DefineColor(Color.Green);
            }

            //Left side
            for (int y = rectangle.Bottom + 1; y >= rectangle.Top; y--) {
                highlight.DefineVertex(new Vector3(rectangle.Left, map.GetHeightAt(rectangle.Left, y) + 0.1f, y));
                highlight.DefineColor(Color.Green);
            }
        }

        public void HideHighlight() {
            highlight.Remove();
            highlight = null;
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

        private void CreateModel(ITile[] tiles, IntVector2 size) {

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
                foreach (var tile in tiles) {
                    //Create verticies
                    *(verBuff++) = new TileInVB(tile);

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

        public void Dispose() {
            model.Dispose();
            material.Dispose();
        }
    }
}
