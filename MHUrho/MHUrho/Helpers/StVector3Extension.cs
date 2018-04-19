using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Storage;

namespace MHUrho.Helpers
{
	public static class StVector3Extension
	{
		public static Vector3 ToVector3(this StVector3 stVector3) {
			return new Vector3(stVector3.X, stVector3.Y, stVector3.Z);
		}
	}
}
