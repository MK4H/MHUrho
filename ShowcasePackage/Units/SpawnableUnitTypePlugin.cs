using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.CameraMovement;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.UserInterface.MandK;
using ShowcasePackage.Misc;

namespace ShowcasePackage.Units
{
	public abstract class SpawnableUnitTypePlugin : UnitTypePlugin
	{
		public abstract Cost Cost { get; }

		public abstract UnitType UnitType { get; }

		public abstract Spawner GetSpawner(GameController input, GameUI ui, CameraMover camera);
	}
}
