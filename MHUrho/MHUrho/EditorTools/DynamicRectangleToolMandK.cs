using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Urho.Gui;
using Urho;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.UserInterface;
using MHUrho.WorldMap;

namespace MHUrho.EditorTools
{
	class DynamicRectangleToolMandK : DynamicRectangleTool, IMandKTool {
		public delegate void HandleSelectedRectangle(IntVector2 topLeft, IntVector2 bottomRight, MouseButtonUpEventArgs e);

		public delegate void HandleSingleClick(MouseButtonUpEventArgs e);

		public event HandleSelectedRectangle SelectionHandler;
		public event HandleSingleClick SingleClickHandler;

		readonly GameMandKController input;
		readonly MandKGameUI ui;
		readonly CameraMover camera;

		IntVector2 mouseDownPos;
		IntVector2 lastMousePos;
		//TODO: Raycast into a plane and get point even outside the map
		bool validMouseDown;
		bool rectangle;

		bool enabled;

		public DynamicRectangleToolMandK(GameMandKController input, MandKGameUI ui, CameraMover camera)
			: base(input)
		{
			this.input = input;
			this.ui = ui;
			this.camera = camera;
		}

		public override void Enable() {
			if (enabled) return;

			if (SelectionHandler == null) {
				throw new
					InvalidOperationException($"{nameof(SelectionHandler)} and {nameof(SingleClickHandler)} were not set, cannot enable without handler");
			}

			input.MouseDown += MouseDown;
			input.MouseUp += MouseUp;
			input.MouseMove += MouseMove;
			camera.CameraMoved += CameraMove;

			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			input.MouseDown -= MouseDown;
			input.MouseUp -= MouseUp;
			input.MouseMove -= MouseMove;
			camera.CameraMoved -= CameraMove;

			enabled = false;
		}


		public override void ClearPlayerSpecificState() {

		}

		public override void Dispose() {
			Disable();
		}

		void MouseDown(MouseButtonDownEventArgs e) {
			if (ui.UIHovering) return;

			var tile = input.GetTileUnderCursor();
			//TODO: Raycast into a plane and get point even outside the map
			if (tile != null) {
				mouseDownPos = tile.MapLocation;
				lastMousePos = tile.MapLocation;
				validMouseDown = true;
				rectangle = false;
			}
		}

		void MouseUp(MouseButtonUpEventArgs e) {

			if (!validMouseDown) {
				return;
			}

			var tile = input.GetTileUnderCursor();

			if (!rectangle && tile != null) {
				SingleClickHandler?.Invoke(e);
			}
			else {
				IntVector2 topLeft, bottomRight;
				if (tile != null) {
					var endTilePos = tile.MapLocation;
					topLeft = new IntVector2(Math.Min(mouseDownPos.X, endTilePos.X),
												 Math.Min(mouseDownPos.Y, endTilePos.Y));
					bottomRight = new IntVector2(Math.Max(mouseDownPos.X, endTilePos.X),
													 Math.Max(mouseDownPos.Y, endTilePos.Y));
				}
				else { 
					topLeft = new IntVector2(Math.Min(mouseDownPos.X, lastMousePos.X),
												 Math.Min(mouseDownPos.Y, lastMousePos.Y));
					bottomRight = new IntVector2(Math.Max(mouseDownPos.X, lastMousePos.X),
													 Math.Max(mouseDownPos.Y, lastMousePos.Y));
				}

				SelectionHandler?.Invoke(topLeft, bottomRight, e);
				//TODO: Different highlight
				Map.DisableHighlight();
			}
		  

		   
			validMouseDown = false;
		}

		void MouseMove(MHUrhoMouseMovedEventArgs e) {
			MouseAndCameraMove();
		}

		void CameraMove(CameraMovedEventArgs args)
		{
			MouseAndCameraMove();
		}

		void MouseAndCameraMove()
		{
			if (!validMouseDown) return;

			var tile = input.GetTileUnderCursor();

			if (tile == null) {
				//TODO: THIS
				return;
			}


			if (tile.MapLocation != mouseDownPos) {
				var endTilePos = tile.MapLocation;
				var topLeft = new IntVector2(Math.Min(mouseDownPos.X, endTilePos.X),
											Math.Min(mouseDownPos.Y, endTilePos.Y));
				var bottomRight = new IntVector2(Math.Max(mouseDownPos.X, endTilePos.X),
												Math.Max(mouseDownPos.Y, endTilePos.Y));

				Map.HighlightRectangle(topLeft, bottomRight, Color.Green);
				lastMousePos = tile.MapLocation;
				rectangle = true;
			}
		}


	}
}
