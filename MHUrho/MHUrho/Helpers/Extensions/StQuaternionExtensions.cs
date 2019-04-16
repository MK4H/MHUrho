using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Helpers.Extensions
{
	public static class StQuaternionExtensions
	{
		public static Quaternion ToQuaternion(this StQuaternion stQuaternion)
		{
			return new Quaternion(stQuaternion.X, stQuaternion.Y, stQuaternion.Z, stQuaternion.W);
		}
	}
}
