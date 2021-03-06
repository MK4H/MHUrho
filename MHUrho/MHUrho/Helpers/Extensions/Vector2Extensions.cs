﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Helpers.Extensions
{
	public static class Vector2Extensions
	{
		public static Vector3 XZ(this Vector3 vector) {
			return new Vector3(vector.X, 0, vector.Z);
		}

		public static Vector2 XZ2(this Vector3 vector) {
			return new Vector2(vector.X, vector.Z);
		}

		public static StVector2 ToStVector2(this Vector2 vector2) {
			return new StVector2 {X = vector2.X, Y = vector2.Y};
		}

		public static Vector2 Round(this Vector2 vector2) {
			return new Vector2((float) Math.Round(vector2.X), (float) Math.Round(vector2.Y));
		}

		public static IntVector2 ToIntVector2(this Vector2 vector2) {
			return new IntVector2((int) vector2.X, (int) vector2.Y);
		}

		public static IntVector2 RoundToIntVector2(this Vector2 vector2) {
			return new IntVector2((int)Math.Round(vector2.X), (int)Math.Round(vector2.Y));
		}

		public static IntVector2 FloorToIntVector2(this Vector2 vector2)
		{
			return new IntVector2((int)Math.Floor(vector2.X), (int)Math.Floor(vector2.Y));
		}

		public static IntVector2 CeilingToIntVector2(this Vector2 vector2)
		{
			return new IntVector2((int)Math.Ceiling(vector2.X), (int)Math.Ceiling(vector2.Y));
		}

		public static bool IsNear(this Vector2 vector2, Vector2 other, float tolerance)
		{
			var diff = vector2 - other;
			return diff.Length < tolerance;
		}

	}
}
