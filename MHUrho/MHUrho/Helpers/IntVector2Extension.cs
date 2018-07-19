using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Storage;

namespace MHUrho.Helpers
{
	public static class IntVector2Extension
	{
		public static StIntVector2 ToStIntVector2(this IntVector2 intVector2) 
		{
			return new StIntVector2 {X = intVector2.X, Y = intVector2.Y};
		}

		public static Vector2 ToVector2(this IntVector2 intVector2)
		{
			return new Vector2(intVector2.X, intVector2.Y);
		}

		public static IntVector2 WithX(this IntVector2 intVector2, int x)
		{
			return new IntVector2(x, intVector2.Y);
		}

		public static IntVector2 WithY(this IntVector2 intVector2, int y)
		{
			return new IntVector2(intVector2.X, y);
		}
	}
}
