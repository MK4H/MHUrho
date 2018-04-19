using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Helpers
{
	public static class StIntVector2Extension
	{
		public static IntVector2 ToIntVector2(this StIntVector2 stIntVector2) {
			return new IntVector2() {X = stIntVector2.X, Y = stIntVector2.Y};
		}
	}
}
