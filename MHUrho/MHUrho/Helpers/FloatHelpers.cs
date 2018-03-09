using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Helpers
{
    class FloatHelpers
    {
        public static bool FloatsEqual(float a, float b, float epsilon = 0.000001f) {
            float diff = Math.Abs(a - b);
            return (a == b) || diff < float.Epsilon || diff / (Math.Abs(a) + Math.Abs(b)) < epsilon;
        }
    }
}
