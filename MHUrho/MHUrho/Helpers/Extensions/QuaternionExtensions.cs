using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Helpers.Extensions
{
	public static class QuaternionExtensions
	{
		public static StQuaternion ToStQuaternion(this Quaternion quaternion)
		{
			return new StQuaternion {X = quaternion.X, Y = quaternion.Y, Z = quaternion.Z, W = quaternion.W};
		}
	}
}
