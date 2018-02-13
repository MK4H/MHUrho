using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Helpers
{
    public static class IntRectExtension
    {
        public static IntVector2 TopLeft(this IntRect rectangle) {
            return new IntVector2(rectangle.Left, rectangle.Top);
        }
    }
}
