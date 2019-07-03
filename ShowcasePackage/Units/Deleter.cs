using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.CameraMovement;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.UserInterface.MandK;
using ShowcasePackage.Misc;
using Urho;

namespace ShowcasePackage.Units
{
	class Deleter : Spawner
	{
		class DeleteUnitType : UnitType
		{
			public DeleteUnitType()
				: base(0, "Delete", null, null, new Urho.IntRect(0, 100, 100, 200), null)
			{

			}
		}

		public override Cost Cost => new Cost(new Dictionary<ResourceType, double>());

		readonly GameController input;
		readonly GameUI ui;
		readonly CameraMover camera;

		public Deleter(GameController input, GameUI ui, CameraMover camera)
			: base(input.Level, new DeleteUnitType())
		{
			this.input = input;
			this.ui = ui;
			this.camera = camera;
		}

		

		public override void OnMouseDown(MouseButtonDownEventArgs e)
		{
			if (e.Button != (int)MouseButton.Left)
			{
				return;
			}

			var raycast = input.CursorRaycast();
			foreach (var result in raycast) {
				
				for (Node currentNode = result.Node; currentNode != Level.LevelNode; currentNode = currentNode.Parent) {
					if (Level.TryGetUnit(currentNode, out IUnit unit))
					{
						unit.RemoveFromLevel();
						return;
					}
				}				
			}
		}

		public override IUnit SpawnAt(ITile tile, IPlayer player)
		{
			throw new InvalidOperationException("Cannot spawn deleter.");
		}
	}
}
