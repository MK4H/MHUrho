using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.CameraMovement;
using Urho.Gui;
using Urho;
using MHUrho.Input;
using MHUrho.Input.MouseKeyboard;
using MHUrho.Logic;
using MHUrho.UserInterface;
using MHUrho.UserInterface.MouseKeyboard;
using MHUrho.WorldMap;

namespace MHUrho.EditorTools.MouseKeyboard.MapHighlighting
{
	public delegate void HandleSelectedRectangle(IntVector2 topLeft, IntVector2 bottomRight, MouseButtonUpEventArgs e);
	public delegate void HandleSingleClick(MouseButtonUpEventArgs e);

	//NOTE: Maybe add raycast into a plane and get point even outside the map
	public class DynamicSizeHighlighter : Base.MapHighlighting.DynamicSizeHighlighter {
		
		public event HandleSelectedRectangle Selected;
		public event HandleSingleClick SingleClick;

		readonly GameController input;
		readonly GameUI ui;
		readonly CameraMover camera;

		IntVector2 mouseDownPos;
		IntVector2 lastMousePos;

		bool validMouseDown;
		bool rectangle;

		bool enabled;

		public DynamicSizeHighlighter(GameController input, GameUI ui, CameraMover camera)
			:base(input)
		{
			this.input = input;
			this.ui = ui;
			this.camera = camera;
		}

		public override void Enable() {
			if (enabled) return;

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

		public override void Dispose() {
			Disable();
		}

		void MouseDown(MouseButtonDownEventArgs e) {
			if (ui.UIHovering) return;

			var tile = input.GetTileUnderCursor();
			//NOTE: Add raycast into a plane and get point even outside the map
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
				InvokeSingleClick(e);
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

				InvokeSelected(topLeft, bottomRight, e);
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
				//NOTE: Add raycast into a plane and get point even outside the map
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

		void InvokeSelected(IntVector2 topLeft, IntVector2 bottomRight, MouseButtonUpEventArgs args)
		{
			try {
				Selected?.Invoke(topLeft, bottomRight, args);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(Selected)}: {e.Message}");
	
			}
		}

		void InvokeSingleClick(MouseButtonUpEventArgs args)
		{
			try
			{
				SingleClick?.Invoke(args);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(SingleClick)}: {e.Message}");

			}
		}
	}
}
