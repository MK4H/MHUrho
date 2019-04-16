using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Helpers.Extensions
{
	public static class StIntVector3Extensions
	{
		public static IntVector3 ToIntVector3(this StIntVector3 stIntVector3) {
			return new IntVector3{ X = stIntVector3.X, Y = stIntVector3.Y, Z = stIntVector3.Z };
		}
	}
}
