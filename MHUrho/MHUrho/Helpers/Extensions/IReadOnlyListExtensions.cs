using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Helpers.Extensions
{
    public static class ReadOnlyListExtensions
    {
		public static int IndexOf<T>(this IReadOnlyList<T> list, T item, IEqualityComparer<T> comparer = null)
		{
			IEqualityComparer<T> eqComparer = comparer ?? EqualityComparer<T>.Default;

			for (int i = 0; i < list.Count; i++) {
				if (eqComparer.Equals(list[i], item)) {
					return i;
				}
			}

			return -1;
		}
    }
}
