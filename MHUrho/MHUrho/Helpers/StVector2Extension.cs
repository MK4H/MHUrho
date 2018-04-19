using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Helpers
{
	public static class StVector2Extension
	{
		public static Vector2 ToVector2(this StVector2 stVector2) {
			return new Vector2(stVector2.X, stVector2.Y);
		}
	}
}
