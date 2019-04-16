using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Storage;

namespace MHUrho.Helpers.Extensions
{
    public static class ColorExtensions
    {
		public static StColor ToStColor(this Color color)
		{
			StColor storedColor = new StColor();
			storedColor.R = color.R;
			storedColor.G = color.G;
			storedColor.B = color.B;
			storedColor.A = color.A;
			return storedColor;
		}
    }
}
