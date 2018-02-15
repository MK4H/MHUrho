using System;
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
    }
}
