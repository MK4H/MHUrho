using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Helpers
{
    public static class StColorExtensions
    {
		public static Color ToColor(this StColor storedColor)
		{
			return new Color(storedColor.R, storedColor.G, storedColor.B, storedColor.A);
		}
    }
}
