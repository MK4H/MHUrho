﻿using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Helpers
{
    public static class IntRectExtension
    {
        /// <summary>
        /// Gets Top Left corner from IntRect, copies the IntRectangle to do this
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns>Position of the top left corner of the rectangle</returns>
        public static IntVector2 TopLeft(this IntRect rectangle) {
            return new IntVector2(rectangle.Left, rectangle.Top);
        }

        public static IntVector2 TopRight(this IntRect rectangle) {
            return new IntVector2(rectangle.Right, rectangle.Top);
        }

        public static IntVector2 BottomLeft(this IntRect rectangle) {
            return new IntVector2(rectangle.Left, rectangle.Bottom);
        }

        public static IntVector2 BottomRight(this IntRect rectangle) {
            return new IntVector2(rectangle.Right, rectangle.Bottom);
        }

        public static IntVector2 Size(this IntRect rectangle) {
            return new IntVector2(Width(rectangle), Height(rectangle));
        }

        public static int Width(this IntRect rectangle) {
            return rectangle.Right - rectangle.Left;
        }

        public static int Height(this IntRect rectangle) {
            return rectangle.Bottom - rectangle.Top;
        }
    }
}
