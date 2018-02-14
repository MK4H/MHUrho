using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Logic
{
    class Map {
        private Tile[] contents;


        public StaticModel Model { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        /// <summary>
        /// X coordinate of the left row of the map
        /// </summary>
        public int Left => 0;
        /// <summary>
        /// X coordinate of the right row of the map
        /// </summary>
        public int Right => Width - 1;
        /// <summary>
        /// Y coordinate of the top row of the map
        /// </summary>
        public int Top => 0;
        /// <summary>
        /// Y coordinate of the bottom row of the map
        /// </summary>
        public int Bottom => Height - 1;

        public static Map CreateDefaultMap(int width, int height) {
            //TODO: Split map into chunks, that will be separately in memory

            //4 verticies for every tile, so that we can map every tile to different texture
            // and the same tile types to the same textures
            int numVerticies = width * height * 4;
            //TODO: maybe connect the neighbouring verticies
            //two triangles per tile, 3 indicies per triangle
            int numIndicies = width * height * 6;
            // 3 floats for position, 3 floats for normal vector, 2 floats for texture position
            float[] vertexData = new float[numVerticies * 8];
            short[] indexData = new short[numIndicies];

            int vertexDataPos = 0;
            int indexDataPos = 0;
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    //Create verticies
                    for (int verY = 0; verY < 2; verY++) {
                        for (int verX = 0; verX < 2; verX++) {
                            //Position
                            vertexData[vertexDataPos] = x + verX;
                            vertexData[vertexDataPos + 1] = 0;
                            vertexData[vertexDataPos + 2] = y + verY;
                            //Normal vector
                            vertexData[vertexDataPos + 3] = 0;
                            vertexData[vertexDataPos + 4] = 1;
                            vertexData[vertexDataPos + 5] = 0;
                            //Texture
                            vertexData[vertexDataPos + 6] = verX;
                            vertexData[vertexDataPos + 7] = verY;
                            vertexDataPos += 8;
                        }
                    }

                    int firstVertex = x * 4 + y * width * 4;

                    //Connect verticies to triangles
                    indexData[indexDataPos + 0] = (short)(firstVertex + 0);
                    indexData[indexDataPos + 1] = (short)(firstVertex + 2);
                    indexData[indexDataPos + 2] = (short)(firstVertex + 3);

                    indexData[indexDataPos + 3] = (short)(firstVertex + 0);
                    indexData[indexDataPos + 4] = (short)(firstVertex + 3);
                    indexData[indexDataPos + 5] = (short)(firstVertex + 1);

                    indexDataPos += 6;
                }
            }

            Model model = new Model();
            VertexBuffer vb = new VertexBuffer(Application.CurrentContext, false);
            IndexBuffer ib = new IndexBuffer(Application.CurrentContext, false);
            Geometry geom = new Geometry();

            vb.Shadowed = true;
            vb.SetSize((uint)numVerticies, ElementMask.Position | ElementMask.Normal | ElementMask.TexCoord1, false);
            vb.SetData(vertexData);

            ib.Shadowed = true;
            ib.SetSize((uint)numIndicies, false, false);
            ib.SetData(indexData);

            geom.SetVertexBuffer(0, vb);
            geom.IndexBuffer = ib;
            geom.SetDrawRange(PrimitiveType.TriangleList, 0, (uint)numIndicies, true);

            model.NumGeometries = 1;
            model.SetGeometry(0, 0, geom);
            model.BoundingBox = new BoundingBox(new Vector3(0, 0, 0), new Vector3(width, 1, height));

            StaticModel sm = new StaticModel();
            sm.Model = model;
            sm.Material = CoreAssets.Materials.DefaultGrey;
            return new Map(width, height, sm);
        }

        protected Map(int width, int height, StaticModel model) {
            Width = width;
            Height = height;
            this.Model = model;
            contents = new Tile[width * height];
        }


        /// <summary>
        /// Checks if the point is inside the map, which means it could be used for indexing into the map
        /// </summary>
        /// <param name="point">the point to check</param>
        /// <returns>True if it is inside, False if not</returns>
        public bool IsInside(IntVector2 point) {
            return point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
        }

        /// <summary>
        /// Gets tile at the coordinates [x,y]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>the tile at [x,y]</returns>
        public Tile GetTile(int x, int y) {
            return contents[x + y * Width];
        }

        /// <summary>
        /// Gets tile at the coordinates
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns>the tile at [X,Y]</returns>
        public Tile GetTile(IntVector2 coordinates) {
            return GetTile(coordinates.X, coordinates.Y);
        }

    }
}
