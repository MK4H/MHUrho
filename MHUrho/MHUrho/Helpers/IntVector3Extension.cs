using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Helpers
{
	public static class IntVector3Extension
	{
		public static StIntVector3 ToStIntVector3(this IntVector3 intVector3) {
			return new StIntVector3 {X = intVector3.X, Y = intVector3.Y, Z = intVector3.Z};
		}
	}
}
