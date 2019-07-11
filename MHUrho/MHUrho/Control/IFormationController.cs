using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.DefaultComponents;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Control
{
	public interface IFormationController {
		bool MoveToFormation(UnitSelector unit);

		bool MoveToFormation(UnitGroup units);
	}

}
