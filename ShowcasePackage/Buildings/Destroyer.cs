using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.CameraMovement;
using MHUrho.Helpers.Extensions;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.UserInterface.MandK;
using ShowcasePackage.Misc;
using Urho;

namespace ShowcasePackage.Buildings
{
	class Destroyer : Builder
	{
		class DestroyBuildingType : BuildingType {
			public DestroyBuildingType()
				:base(0,"Destroy",null,null,new Urho.IntRect(0,100,100,200), new Urho.IntVector2(1,1), null)
			{

			}
		}

		protected readonly GameController Input;
		protected readonly GameUI Ui;
		protected readonly CameraMover Camera;

		readonly BaseCustomWindowUI cwUI;


		public Destroyer(GameController input, GameUI ui, CameraMover camera)
			: base(input.Level, new DestroyBuildingType())
		{
			this.Input = input;
			this.Ui = ui;
			this.Camera = camera;

			cwUI = new BaseCustomWindowUI(ui, "Demolish buildings", "");
		}

		public override void Enable()
		{
			base.Enable();

			cwUI.Show();
		}

		public override void Disable()
		{
			cwUI.Hide();

			base.Disable();
		}

		public override void Dispose()
		{
			cwUI.Dispose();

			base.Dispose();
		}

		public override void OnMouseDown(MouseButtonDownEventArgs e)
		{
			if (e.Button != (int)MouseButton.Left) {
				return;
			}

			var raycast = Input.CursorRaycast();
			foreach (var result in raycast) {
				for (Node current = result.Node; current != Level.LevelNode && current != null; current = current.Parent)
				{
					if (Level.TryGetBuilding(current, out IBuilding building))
					{
						building.RemoveFromLevel();
						return;
					}
				}
				
			}
		}
	}
}
