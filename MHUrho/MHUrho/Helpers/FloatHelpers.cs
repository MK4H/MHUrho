using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Helpers {
	public static class FloatHelpers
	{
		/// <summary>
		/// Adapted from https://stackoverflow.com/questions/3874627/floating-point-comparison-functions-for-c-sharp
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="epsilon"></param>
		/// <returns></returns>
		public static bool FloatsEqual(float a, float b, float epsilon = 0.000001f) {
			
			float diff = Math.Abs(a - b);

			if (a == b) { 
				// shortcut, handles infinities
				return true;
			}
			else if (a == 0 || b == 0 || diff < float.Epsilon) {
				// a or b is zero or both are extremely close to it
				// relative error is less meaningful here
				return diff < epsilon;
			}
			else { // use relative error
				return diff / (Math.Abs(a) + Math.Abs(b)) < epsilon;
			}
		}
	}
}
