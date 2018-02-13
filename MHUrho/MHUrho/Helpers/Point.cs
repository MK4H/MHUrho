using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Urho;

namespace MHUrho.Helpers
{
    public struct Point {

        private int x, y;

        public int X {
            get {
                return x;
            }
            set {
                x = value;
            }
        }
        public int Y {
            get {
                return y;
            }
            set {
                y = value;
            }
        }

        public Point(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public static bool operator ==( Point left,Point right) {
            return (left.x == right.x) && (left.y == right.y);
        }

        public static bool operator !=(Point left, Point right) {
            return left.x != right.x || left.x != right.x;
        }

        public override bool Equals(object obj) {
            if (!(obj is Point)) {
                return false;
            }
            Point p = (Point)obj;
            return (x == p.x) && (y == p.y);
        }

        public float DistanceTo(Point target) {
            int dx = x - target.x;
            int dy = y - target.y;

            int hyp = dx * dx + dy * dy;
            return (float)Math.Sqrt(hyp);
        }

        public static float Distance(Point value1, Point value2) {
            return Distance(ref value1, ref value2);
        }

        public static float Distance(ref Point value1, ref Point value2) {
            int dx = value1.x - value2.x;
            int dy = value1.y - value2.y;

            int hyp = dx * dx + dy * dy;
            return (float)Math.Sqrt(hyp);
        }

        public override int GetHashCode() {
            return unchecked(x ^ y);
        }

        public override string ToString() {
            return "{X=" + X.ToString(CultureInfo.CurrentCulture) + ",Y=" + Y.ToString(CultureInfo.CurrentCulture) + "}";
        }


    }
}
