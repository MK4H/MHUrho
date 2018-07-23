using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Input;
using MHUrho.Logic;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class MandKUIMinimap : UIMinimap
    {
		readonly MandKGameUI uiManager;

		GameMandKController InputCtl => uiManager.InputCtl;

		public MandKUIMinimap(Button minimapButton, MandKGameUI uiManager, CameraMover cameraMover, ILevelManager level)
			: base(minimapButton, cameraMover, level)
		{
			this.uiManager = uiManager;
			InputCtl.MouseWheelMoved += MouseWheel;
		}

		public override void Dispose()
		{
			base.Dispose();

			InputCtl.MouseWheelMoved -= MouseWheel;
		}

		protected override void Pressed(PressedEventArgs e)
		{
			MinimapClickPos = Button.ScreenToElement(InputCtl.CursorPosition);

			Vector2? worldPosition = Level.Minimap.MinimapToWorld(MinimapClickPos);

			if (worldPosition.HasValue) {
				CameraMover.MoveTo(worldPosition.Value);
			}

			Level.Minimap.Refresh();
			InputCtl.MouseMove += MouseMove;
		}

		protected override void Released(ReleasedEventArgs e)
		{
			InputCtl.MouseMove -= MouseMove;

			StopCameraMovement();
		}

		void MouseWheel(MouseWheelEventArgs e)
		{
			if (!MinimapHover) return;

			Level.Minimap.Zoom(e.Wheel);
		}

		void MouseMove(MHUrhoMouseMovedEventArgs e)
		{
			Vector2 newMovement = (Button.ScreenToElement(e.CursorPosition) - MinimapClickPos).ToVector2();
			newMovement.Y = -newMovement.Y;
			CameraMover.SetStaticHorizontalMovement(CameraMover.StaticHorizontalMovement + (newMovement - PreviousCameraMovement));

			PreviousCameraMovement = newMovement;
		}

		
	}
}
