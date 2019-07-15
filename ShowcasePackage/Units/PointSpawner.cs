using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.CameraMovement;
using MHUrho.Input.MouseKeyboard;
using MHUrho.Logic;
using MHUrho.UserInterface.MouseKeyboard;
using Urho;

namespace ShowcasePackage.Units
{
	abstract class PointSpawner : Spawner
	{
		readonly GameController input;
		readonly GameUI ui;
		readonly CameraMover camera;

		protected PointSpawner(GameController input, GameUI ui, CameraMover camera, UnitType type)
			: base(input.Level, type)
		{
			this.input = input;
			this.ui = ui;
			this.camera = camera;
		}

		public override void OnMouseDown(MouseButtonDownEventArgs e)
		{
			if (ui.UIHovering)
			{
				return;
			}

			
			foreach (var result in input.CursorRaycast())
			{
				if (Level.Map.IsRaycastToMap(result))
				{
					//Try spawn at the map position, don't raycast through map
					SpawnAt(Map.GetContainingTile(result.Position), input.Player);
					return;
				}

				for (Node current = result.Node; current != Level.LevelNode && current != null; current = current.Parent)
				{
					//If it is part of a building
					if (Level.TryGetBuilding(current, out IBuilding building))
					{
						//Try spawn at the building hit, if successfully spawned, return
						if (SpawnAt(Map.GetContainingTile(result.Position), input.Player) != null)
						{
							return;
						}
					}
				}
			}
		}
	}
}
