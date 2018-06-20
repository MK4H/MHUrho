using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Helpers
{
    public class BidirectionalIterator<T> {

		public T Current => values[currentIndex];

		List<T> values;

		int currentIndex = -1;

		public BidirectionalIterator(IEnumerable<T> enumerator)
		{
			values = new List<T>(enumerator);
		}

		public bool IsAfterEnd()
		{
			return currentIndex >= values.Count;
		}

		public bool IsBeforeStart()
		{
			return currentIndex < 0;
		}

		public bool IsValid()
		{
			return !IsBeforeStart() && !IsAfterEnd();
		}

		public bool TryMoveNext()
		{
			currentIndex++;
			if (currentIndex >= values.Count) {
				currentIndex = values.Count;
				return false;
			}

			return true;
		}

		public bool TryMovePrevious()
		{
			currentIndex--;
			if (currentIndex < 0) {
				currentIndex = -1;
				return false;
			}

			return true;
		}
    }
}
