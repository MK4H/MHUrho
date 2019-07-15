using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.CameraMovement;
using MHUrho.Input.MouseKeyboard;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.UserInterface.MouseKeyboard;
using Urho;

namespace ShowcasePackage.Buildings
{
	public abstract class BaseBuildingTypePlugin : BuildingTypePlugin {
		public abstract Builder GetBuilder(GameController input, GameUI ui, CameraMover camera);

		protected bool HeightDiffLow(IntVector2 topLeft, IntVector2 bottomRight, ILevelManager level, float maxHeightDiff)
		{
			bottomRight += new IntVector2(1, 1);

			float maxHeight = level.Map.GetTerrainHeightAt(topLeft);
			float minHeight = maxHeight;
			for (int y = topLeft.Y; y <= bottomRight.Y; y++)
			{
				for (int x = topLeft.X; x <= bottomRight.X; x++)
				{
					float height = level.Map.GetTerrainHeightAt(x, y);
					if (height > maxHeight)
					{
						maxHeight = height;
					}
					else if (height < minHeight)
					{
						minHeight = height;
					}
				}
			}

			return maxHeight - minHeight < maxHeightDiff;
		}
	}
}
