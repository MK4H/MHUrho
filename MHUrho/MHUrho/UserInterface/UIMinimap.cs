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
    abstract class UIMinimap {
		public event Action<HoverBeginEventArgs> HoverBegin;
		public event Action<HoverEndEventArgs> HoverEnd;

		protected readonly Button Button;
		protected readonly CameraMover CameraMover;
		protected readonly ILevelManager Level;

		protected bool MinimapHover = false;
		protected IntVector2 MinimapClickPos;
		protected Vector2 PreviousCameraMovement;

		protected UIMinimap(Button minimapButton, CameraMover cameraMover, ILevelManager level)
		{
			this.Button = minimapButton;
			this.CameraMover = cameraMover;
			this.Level = level;

			Button.Texture = Level.Minimap.Texture;
			Button.Pressed += Pressed;
			Button.Released += Released;
			Button.HoverBegin += OnHoverBegin;
			Button.HoverEnd += OnHoverEnd;
		}

		protected abstract void Pressed(PressedEventArgs e);

		protected abstract void Released(ReleasedEventArgs e);

		protected virtual void OnHoverBegin(HoverBeginEventArgs e)
		{
			MinimapHover = true;
			HoverBegin?.Invoke(e);
		}

		protected virtual void OnHoverEnd(HoverEndEventArgs e)
		{
			MinimapHover = false;

			StopCameraMovement();
			HoverEnd?.Invoke(e);
		}



		protected void StopCameraMovement()
		{

			var cameraMovement = CameraMover.StaticHorizontalMovement - PreviousCameraMovement;
			cameraMovement.X = FloatHelpers.FloatsEqual(cameraMovement.X, 0) ? 0 : cameraMovement.X;
			cameraMovement.Y = FloatHelpers.FloatsEqual(cameraMovement.Y, 0) ? 0 : cameraMovement.Y;

			CameraMover.SetStaticHorizontalMovement(cameraMovement);

			PreviousCameraMovement = Vector2.Zero;
		}

	}
}
