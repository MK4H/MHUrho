using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.CameraMovement;
using MHUrho.Input.MandK;
using MHUrho.Plugins;
using MHUrho.UserInterface.MandK;

namespace ShowcasePackage.Buildings
{
	public abstract class BaseBuildingTypePlugin : BuildingTypePlugin {
		public abstract Builder GetBuilder(GameController input, GameUI ui, CameraMover camera);
	}
}
