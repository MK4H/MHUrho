using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Helpers
{
    public static class Vector2Extension
    {
        public static Vector3 XZ(this Vector3 vector) {
            return new Vector3(vector.X, 0, vector.Z);
        }

        public static Vector2 XZ2(this Vector3 vector) {
            return new Vector2(vector.X, vector.Z);
        }
    }
}
