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

		public static Vector3 WithX(this Vector3 vector, float newX)
		{
			return new Vector3(newX, vector.Y, vector.Z);
		}

		public static Vector3 WithY(this Vector3 vector, float newY)
		{
			return new Vector3(vector.X, newY, vector.Z);
		}

		public static Vector3 WithZ(this Vector3 vector, float newZ)
		{
			return new Vector3(vector.X, vector.Y, newZ);
		}
	}
}
