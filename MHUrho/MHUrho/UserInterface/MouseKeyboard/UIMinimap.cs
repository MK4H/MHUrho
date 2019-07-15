using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Input;
using MHUrho.Input.MouseKeyboard;
using MHUrho.Logic;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface.MouseKeyboard
{
    class UIMinimap : MHUrho.UserInterface.UIMinimap
    {
		readonly GameUI uiManager;

		GameController InputCtl => uiManager.InputCtl;

		bool controllingMovement;

		public UIMinimap(Button minimapButton, GameUI uiManager, CameraMover cameraMover, ILevelManager level)
			: base(minimapButton, cameraMover, level)
		{
			this.uiManager = uiManager;
			InputCtl.MouseWheelMoved += MouseWheel;
			controllingMovement = false;
		}

		public override void Dispose()
		{
			base.Dispose();

			InputCtl.MouseWheelMoved -= MouseWheel;
		}

		protected override void Pressed(PressedEventArgs args)
		{
			MinimapClickPos = Button.ScreenToElement(InputCtl.CursorPosition);

			Vector2? worldPosition = Level.Minimap.MinimapToWorld(MinimapClickPos);

			if (worldPosition.HasValue) {
				CameraMover.MoveTo(worldPosition.Value);
			}

			Level.Minimap.Refresh();
			InputCtl.MouseMove += MouseMove;
			InputCtl.MouseUp += MouseUp;
			controllingMovement = true;
		}

		protected override void Released(ReleasedEventArgs args)
		{
			if (controllingMovement) {
				StopMovementControl();
			}
		}

		void MouseWheel(MouseWheelEventArgs args)
		{
			if (!MinimapHover) return;

			Level.Minimap.Zoom(args.Wheel);
		}

		void MouseMove(MHUrhoMouseMovedEventArgs args)
		{
			Vector2 newMovement = (Button.ScreenToElement(args.CursorPosition) - MinimapClickPos).ToVector2();
			newMovement.Y = -newMovement.Y;
			CameraMover.SetStaticHorizontalMovement(CameraMover.StaticHorizontalMovement + (newMovement - PreviousCameraMovement));

			PreviousCameraMovement = newMovement;
		}

		void MouseUp(MouseButtonUpEventArgs args)
		{
			if (controllingMovement) {
				StopMovementControl();
			}
		}

		void StopMovementControl()
		{
			InputCtl.MouseMove -= MouseMove;

			StopCameraMovement();
			controllingMovement = false;
		}
	}
}
