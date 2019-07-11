using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.DefaultComponents;

namespace MHUrho.Control
{
    public class UnitGroup {
		BidirectionalIterator<UnitSelector> units;

		public UnitSelector Current => units.Current;

		public UnitGroup(IEnumerable<UnitSelector> units)
		{
			this.units = new BidirectionalIterator<UnitSelector>(units);
			this.units.TryMoveNext();
		}

		public bool AnyLeft()
		{
			return !units.IsAfterEnd();
		}

		public bool TryMoveNext()
		{
			return units.TryMoveNext();
		}

		public bool TryMovePrevious()
		{
			return units.TryMovePrevious();
		}

		public bool IsValid()
		{
			return units.IsValid();
		}
    }
}
