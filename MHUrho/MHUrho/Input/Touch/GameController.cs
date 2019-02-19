using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.Logic;
using MHUrho.Control;
using MHUrho.Packaging;
using MHUrho.UserInterface;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.Input.Touch
{
	public class GameController : Controller, IGameController
	{
		enum CameraMovementType { Horizontal, Vertical, FreeFloat }


		GameUIManager IGameController.UIManager => UIManager;

		public TouchUI UIManager { get; private set; }

		public InputType InputType => InputType.Touch;

		public ILevelManager Level { get; private set; }

		public IPlayer Player { get; set; }

		public float Sensitivity { get; set; }
		public bool ContinuousMovement { get; private set; } = true;

		public bool DoOnlySingleRaycasts { get; set; }
		public void Pause()
		{
			throw new NotImplementedException();
		}

		public void UnPause()
		{
			throw new NotImplementedException();
		}

		public void EndLevel()
		{
			throw new NotImplementedException();
		}

		public List<RayQueryResult> CursorRaycast()
		{
			throw new NotImplementedException();
		}

		public ITile GetTileUnderCursor()
		{
			throw new NotImplementedException();
		}

		public void ChangeControllingPlayer(IPlayer newControllingPlayer)
		{
			throw new NotImplementedException();
		}

		public bool UIPressed { get; set; }

		readonly CameraMover cameraMover;
		readonly Octree octree;
		

		CameraMovementType movementType;

		readonly Dictionary<int, Vector2> activeTouches = new Dictionary<int, Vector2>();


		

		public GameController(ILevelManager level, Octree octree, IPlayer player, CameraMover cameraMover, float sensitivity = 0.1f) 
		{
			this.cameraMover = cameraMover;
			this.Sensitivity = sensitivity;
			this.Level = level;
			this.octree = octree;
			this.DoOnlySingleRaycasts = true;
			this.Player = player;

			//SwitchToContinuousMovement();
			SwitchToDiscontinuousMovement();
			movementType = CameraMovementType.Horizontal;

			Enable();

		}

		public void SwitchToContinuousMovement() {
			ContinuousMovement = true;

			cameraMover.SmoothMovement = true;
		}

		public void SwitchToDiscontinuousMovement() {
			ContinuousMovement = false;

			cameraMover.SmoothMovement = true;
			
		}

		public void Dispose() {
			Disable();
		}



		protected override void TouchBegin(TouchBeginEventArgs e) {
			if (!UIPressed) {
				activeTouches.Add(e.TouchID, new Vector2(e.X, e.Y));

				var clickedRay = cameraMover.Camera.GetScreenRay(e.X / (float)UI.Root.Width,
																	  e.Y / (float)UI.Root.Height);

				if (DoOnlySingleRaycasts) {
					var raycastResult = octree.RaycastSingle(clickedRay);
					if (raycastResult.HasValue) {
						//levelManager.HandleRaycast(Player, raycastResult.Value);
					}
				}
				else {
					var raycastResults = octree.Raycast(clickedRay);
					if (raycastResults.Count != 0) {
						//levelManager.HandleRaycast(Player, raycastResults);
					}
				}
			}
			else {
				UIPressed = false;
			}
		}

		protected override void TouchEnd(TouchEndEventArgs e) {
			activeTouches.Remove(e.TouchID);

			cameraMover.AddDecayingHorizontalMovement(cameraMover.StaticHorizontalMovement);
			cameraMover.SetStaticHorizontalMovement(Vector2.Zero);
		}

		protected override void TouchMove(TouchMoveEventArgs e) {
			try {
				switch (movementType) {
					case CameraMovementType.Horizontal:
						HorizontalMove(e);
						break;
					case CameraMovementType.Vertical:
						break;
					case CameraMovementType.FreeFloat:
						break;
					default:
						throw new InvalidOperationException("Unsupported CameraMovementType in TouchController");
				}

			}
			catch (KeyNotFoundException) {
				Urho.IO.Log.Write(LogLevel.Warning, "TouchID was not valid in TouchMove");
			}
		}

		private void HorizontalMove(TouchMoveEventArgs e) {
			if (ContinuousMovement) {
				var movement = (new Vector2(e.X, e.Y) - activeTouches[e.TouchID]) * Sensitivity;
				movement.Y = -movement.Y;
				cameraMover.SetStaticHorizontalMovement(movement);
			}
			else {
				var movement = new Vector2(e.DX, -e.DY) * Sensitivity;
				cameraMover.AddDecayingHorizontalMovement(movement);
			}
		}


	}
}
