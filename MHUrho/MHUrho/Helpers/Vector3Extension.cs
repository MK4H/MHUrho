using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Helpers
{
	public static class Vector3Extension
	{
		public static StVector3 ToStVector3(this Vector3 vector3) {
			return new StVector3 {X = vector3.X, Y = vector3.Y, Z = vector3.Z};
		}

		public static bool IsNear(this Vector3 a, Vector3 b, float tolerance) {
			var diff = (a - b).Length;
			return diff < tolerance;
		}
	}
}
