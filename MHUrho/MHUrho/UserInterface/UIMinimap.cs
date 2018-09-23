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
    abstract class UIMinimap : IDisposable {
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

		protected abstract void Pressed(PressedEventArgs args);

		protected abstract void Released(ReleasedEventArgs args);

		protected virtual void OnHoverBegin(HoverBeginEventArgs args)
		{
			MinimapHover = true;
			HoverBegin?.Invoke(args);
		}

		protected virtual void OnHoverEnd(HoverEndEventArgs args)
		{
			MinimapHover = false;
			HoverEnd?.Invoke(args);

		}



		protected void StopCameraMovement()
		{

			var cameraMovement = CameraMover.StaticHorizontalMovement - PreviousCameraMovement;
			cameraMovement.X = FloatHelpers.FloatsEqual(cameraMovement.X, 0) ? 0 : cameraMovement.X;
			cameraMovement.Y = FloatHelpers.FloatsEqual(cameraMovement.Y, 0) ? 0 : cameraMovement.Y;

			CameraMover.SetStaticHorizontalMovement(cameraMovement);

			PreviousCameraMovement = Vector2.Zero;
		}

		public virtual void Dispose()
		{
			Button.Pressed -= Pressed;
			Button.Released -= Released;
			Button.HoverBegin -= OnHoverBegin;
			Button.HoverEnd -= OnHoverEnd;

			Button.Dispose();
			CameraMover.Dispose();
		}
	}
}
