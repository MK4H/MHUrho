using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho;
using Urho.Resources;

namespace MHUrho.Graphics
{
    public class MapGraphics : IDisposable {
        private Model model;

        private Material material;

        public static MapGraphics Build(Node mapNode, Tile[] tiles, IntVector2 size) {
            MapGraphics graphics = new MapGraphics();
            graphics.material = CreateMaterial();
            graphics.model = CreateModel(tiles, size);

            StaticModel model = mapNode.CreateComponent<StaticModel>();
            model.Model = graphics.model;
            model.SetMaterial(graphics.material);

            return graphics;
        }

        protected MapGraphics() {

        }

        private static Material CreateMaterial() {
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

            return Material.FromImage(mapImage);
        }

        private static Model CreateModel(Tile[] tiles, IntVector2 size) {

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
                float* verBuff = (float*)vbPointer.ToPointer();
                short* inBuff = (short*)ibPointer.ToPointer();

                int vertexIndex = 0;
                foreach (var tile in tiles) {
                    //Create verticies
                    verBuff = FillVertex(
                        verBuff,
                        new Vector3(tile.MapArea.Left, 0, tile.MapArea.Top),
                        new Vector3(0, 1, 0),
                        new Vector2(tile.Type.TextureCoords.Min.X, tile.Type.TextureCoords.Min.Y));

                    verBuff = FillVertex(
                        verBuff,
                        new Vector3(tile.MapArea.Right, 0, tile.MapArea.Top),
                        new Vector3(0, 1, 0),
                        new Vector2(tile.Type.TextureCoords.Max.X, tile.Type.TextureCoords.Min.Y));

                    verBuff = FillVertex(
                        verBuff,
                        new Vector3(tile.MapArea.Left, 0, tile.MapArea.Bottom),
                        new Vector3(0, 1, 0),
                        new Vector2(tile.Type.TextureCoords.Min.X, tile.Type.TextureCoords.Max.Y));

                    verBuff = FillVertex(
                        verBuff,
                        new Vector3(tile.MapArea.Right, 0, tile.MapArea.Bottom),
                        new Vector3(0, 1, 0),
                        new Vector2(tile.Type.TextureCoords.Max.X, tile.Type.TextureCoords.Max.Y));

                    //Connect verticies to triangles
                    *(inBuff++) = (short)(vertexIndex + 0);
                    *(inBuff++) = (short)(vertexIndex + 2);
                    *(inBuff++) = (short)(vertexIndex + 3);

                    *(inBuff++) = (short)(vertexIndex + 3);
                    *(inBuff++) = (short)(vertexIndex + 1);
                    *(inBuff++) = (short)(vertexIndex + 0);

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

            return model;
        }

        private static unsafe float* FillVertex(float* vertexBuffer, Vector3 position, Vector3 normal, Vector2 texCoords) {
            //Position
            *(vertexBuffer++) = position.X;
            *(vertexBuffer++) = position.Y;
            *(vertexBuffer++) = position.Z;
            //Normal
            *(vertexBuffer++) = normal.X;
            *(vertexBuffer++) = normal.Y;
            *(vertexBuffer++) = normal.Z;
            //Texture
            *(vertexBuffer++) = texCoords.X;
            *(vertexBuffer++) = texCoords.Y;

            return vertexBuffer;
        }

        public void Dispose() {
            model.Dispose();
            material.Dispose();
        }
    }
}
