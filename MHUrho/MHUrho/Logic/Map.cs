﻿using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using ProtoBuf;
using System.IO;

namespace MHUrho.Logic
{
    [ProtoContract]
    public class Map : IMap {

        [ProtoMember(1)]
        private readonly Tile[] contents;
       
        public Model Model { get; private set; }

        public Material Material { get; private set; }


        /// <summary>
        /// Coordinates of the top left corner of the map
        /// </summary>
        [ProtoMember(2)]
        public IntVector2 TopLeft { get; private set; }


        /// <summary>
        /// Coordinates of the bottom right corner of the map
        /// </summary>
        [ProtoMember(3)]
        public IntVector2 BottomRight { get; private set; }

        public int Width => Right + 1;

        public int Height => Bottom + 1;

        /// <summary>
        /// X coordinate of the left row of the map
        /// </summary>
        public int Left => TopLeft.X;
        /// <summary>
        /// X coordinate of the right row of the map
        /// </summary>
        public int Right => BottomRight.X;

        /// <summary>
        /// Y coordinate of the top row of the map
        /// </summary>
        public int Top => TopLeft.Y;
        /// <summary>
        /// Y coordinate of the bottom row of the map
        /// </summary>
        public int Bottom => BottomRight.Y;

        public static Map CreateDefaultMap(int width, int height, Context context) {
            //TODO: Split map into chunks, that will be separately in memory

            //4 verticies for every tile, so that we can map every tile to different texture
            // and the same tile types to the same textures
            uint numVerticies = (uint)(width * height * 4);
            //TODO: maybe connect the neighbouring verticies
            //two triangles per tile, 3 indicies per triangle
            uint numIndicies = (uint)(width * height * 6);

            Model model = new Model();
            VertexBuffer vb = new VertexBuffer(context, false);
            IndexBuffer ib = new IndexBuffer(context, false);
            
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
                float* verBuff = (float *)vbPointer.ToPointer();
                short* inBuff = (short*) ibPointer.ToPointer();

                for (int y = 0; y < height; y++) {
                    for (int x = 0; x < width; x++) {
                        //Create verticies
                        for (int verY = 0; verY < 2; verY++) {
                            for (int verX = 0; verX < 2; verX++) {
                                //Position
                                *(verBuff++) = (float)(x + verX);
                                *(verBuff++) = 0.0f;
                                *(verBuff++) = (float)(y + verY);
                                //Normal vector
                                *(verBuff++) = 0.0f;
                                *(verBuff++) = 1.0f;
                                *(verBuff++) = 0.0f;
                                //Texture
                                *(verBuff++) = (float)verX;
                                *(verBuff++) = (float)verY;
                            }
                        }

                        int firstVertex = x * 4 + y * width * 4;

                        //Connect verticies to triangles
                        *(inBuff++) = (short)(firstVertex + 0);
                        *(inBuff++) = (short)(firstVertex + 2);
                        *(inBuff++) = (short)(firstVertex + 3);

                        *(inBuff++) = (short)(firstVertex + 3);
                        *(inBuff++) = (short)(firstVertex + 1);
                        *(inBuff++) = (short)(firstVertex + 0);
                    }
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
            model.BoundingBox = new BoundingBox(new Vector3(0, 0, 0), new Vector3(width, 0, height));

            return new Map(width, height, model, CoreAssets.Materials.DefaultGrey);
        }

        protected Map(int width, int height, Model model, Material material) {
            this.Model = model;
            this.Material = material;
            TopLeft = new IntVector2(0, 0);
            BottomRight = new IntVector2(width - 1, height - 1);
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

        public bool IsXInside(int x) {
            return Left <= x && x <= Right;
        }

        public bool IsXInside(IntVector2 vector) {
            return IsXInside(vector.X);
        }

        public bool IsYInside(int y) {
            return Top <= y && y <= Bottom;
        }

        public bool IsYInside(IntVector2 vector) {
            return IsYInside(vector.Y);
        }

        /// <summary>
        /// Compares x with the coords of Left and Right side, returns where the x is
        /// </summary>
        /// <param name="x">x coord to copare with the map boundaries</param>
        /// <returns>-1 if X is to the left, 0 if inside, 1 if to the right of the map rectangle</returns>
        public int WhereIsX(int x) {
            if (x < Left) {
                return -1;
            }

            if (x > Right) {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Compares x with the coords of Left and Right side, returns where the x is
        /// </summary>
        /// <param name="vector">compares x coord of this vector</param>
        /// <returns>-1 if X is to the left, 0 if inside, 1 if to the right of the map rectangle</returns>
        public int WhereIsX(IntVector2 vector) {
            return WhereIsX(vector.X);
        }


        /// <summary>
        /// Compares y with the coords of Top and Bottom side, returns where the y is
        /// </summary>
        /// <param name="y">y coord to copare with the map boundaries</param>
        /// <returns>-1 if Y is above, 0 if inside, 1 if below the map rectangle</returns>
        public int WhereIsY(int y) {
            if (y < Top) {
                return -1;
            }

            if (y > Bottom) {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Compares y with the coords of Top and Bottom side, returns where the y is
        /// </summary>
        /// <param name="vector">compares y of this vector</param>
        /// <returns>-1 if Y is above, 0 if inside, 1 if below the map rectangle</returns>
        public int WhereIsY(IntVector2 vector) {
            return WhereIsY(vector.Y);
        }

        /// <summary>
        /// Gets tile at the coordinates [x,y]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>the tile at [x,y]</returns>
        public ITile GetTile(int x, int y) {
            return contents[x + y * Width];
        }

        /// <summary>
        /// Gets tile at the coordinates
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns>the tile at [X,Y]</returns>
        public ITile GetTile(IntVector2 coordinates) {
            return GetTile(coordinates.X, coordinates.Y);
        }



        /// <summary>
        /// Moves the rectangle defined by topLeft and bottomRight corners so that
        /// the whole rectangle is inside the map
        /// </summary>
        /// <param name="topLeft">top left corner of the rectangle</param>
        /// <param name="bottomRight">bottom right corner of the rectangle</param>
        /// <returns>True if it is possible to snap to map, false if it is not possible</returns>
        public bool SnapToMap(ref IntVector2 topLeft, ref IntVector2 bottomRight) {

            if ( IsInside(topLeft) && IsInside(bottomRight)) {
                return true;
            }
            int recWidth = bottomRight.X - topLeft.X;
            int recHeight = bottomRight.Y - topLeft.Y;

            bool fits = true;
            int where;

            if (recWidth > Width) {
                //Rectangle is wider than the map, center it on the map
                int diff = recWidth - Width;
                topLeft.X = diff / 2;
                bottomRight.X = topLeft.X + recWidth;

                fits = false;
            }
            else if ((where = WhereIsX(topLeft.X)) != 0) {
                int dist = (where == -1) ? this.TopLeft.X - topLeft.X : this.BottomRight.X - bottomRight.X;
                topLeft.X += dist;
                bottomRight.X += dist;
            }

            if (recHeight > Height) {
                //Rectangle is wider than the map, center it on the map
                int diff = recHeight - Height;
                topLeft.Y = diff / 2;
                bottomRight.Y = topLeft.Y + recHeight;

                fits = false;
            }
            else if ((where = WhereIsY(topLeft.Y)) != 0) {
                int dist = (where == -1) ? this.TopLeft.Y - topLeft.Y : this.BottomRight.Y - bottomRight.Y;
                topLeft.Y += dist;
                bottomRight.Y += dist;
            }

            return fits;
        }


        public void SquishToMap(ref IntVector2 topLeft, ref IntVector2 bottomRight) {
            if (IsInside(topLeft) && IsInside(bottomRight)) {
                return;
            }

            switch (WhereIsX(topLeft.X)) {
                case 0: // NOTHING
                    break;
                case -1:
                    topLeft.X = Left;
                    break;
                case 1:
                    topLeft.X = Right;
                    break;
                default:
                    //TODO: Exceptions
                    throw new Exception("Switch not updated for the current implementation of WhereIsX");
            }

            switch (WhereIsX(bottomRight.X)) {
                case 0: // NOTHING
                    break;
                case -1:
                    bottomRight.X = Left;
                    break;
                case 1:
                    bottomRight.X = Right;
                    break;
                default:
                    //TODO: Exceptions
                    throw new Exception("Switch not updated for the current implementation of WhereIsX");
            }

            switch (WhereIsY(topLeft.Y)) {
                case 0:
                    break;
                case -1:
                    topLeft.Y = Top;
                    break;
                case 1:
                    topLeft.Y = Bottom;
                    break;
                default:
                    //TODO: Exceptions
                    throw new Exception("Switch not updated for the current implementation of WhereIsY");
            }

            switch (WhereIsY(bottomRight.Y)) {
                case 0:
                    break;
                case -1:
                    bottomRight.Y = Top;
                    break;
                case 1:
                    bottomRight.Y = Bottom;
                    break;
                default:
                    //TODO: Exceptions
                    throw new Exception("Switch not updated for the current implementation of WhereIsY");
            }
        }

        public ITile FindClosestEmptyTile(ITile closestTo) {
            int dist = 1;
            while (true) {
                for (int dx = -dist; dx < dist + 1; dx++) {
                    for (int dy = -dist; dy < dist + 1; dy++) {
                        IntVector2 pos = closestTo.Location;
                        pos.X += dx;
                        pos.Y += dy;
                        if (!IsInside(pos)) {
                            continue;
                        }

                        if (GetTile(pos).Unit == null) {
                            return GetTile(pos);
                        }
                    }
                }
                dist++;
                //TODO: Cutoff
            }
        }

        [ProtoAfterDeserialization]
        internal void AfterDeserialization() {

        }
    }
}
