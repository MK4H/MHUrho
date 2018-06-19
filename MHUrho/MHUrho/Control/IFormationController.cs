using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Control
{
	public interface IFormationController {
		bool MoveToFormation(UnitSelector unit);

		bool MoveToFormation(IEnumerator<UnitSelector> units);
	}
}
