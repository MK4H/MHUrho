using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Urho.Gui;
using Urho;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.WorldMap;

namespace MHUrho.EditorTools
{
	class DynamicRectangleToolMandK : DynamicRectangleTool, IMandKTool {
		public delegate void HandleSelectedRectangle(IntVector2 topLeft, IntVector2 bottomRight);

		public delegate void HandleSingleClick(MouseButtonUpEventArgs e);

		public override IEnumerable<Button> Buttons => Enumerable.Empty<Button>();

		public event HandleSelectedRectangle SelectionHandler;
		public event HandleSingleClick SingleClickHandler;

		private readonly GameMandKController input;
		private Map Map => input.LevelManager.Map;

		private IntVector2 mouseDownPos;
		private IntVector2 lastMousePos;
		//TODO: Raycast into a plane and get point even outside the map
		private bool validMouseDown;
		private bool rectangle;

		private bool enabled;

		public DynamicRectangleToolMandK(GameMandKController input) {
			this.input = input;
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
			
			enabled = true;
		}

		public override void Disable() {
			if (!enabled) return;

			input.MouseDown -= MouseDown;
			input.MouseUp -= MouseUp;
			input.MouseMove -= MouseMove;
			
			enabled = false;
		}


		public override void Dispose() {
			Disable();
		}

		private void MouseDown(MouseButtonDownEventArgs e) {
			var tile = input.GetTileUnderCursor();
			//TODO: Raycast into a plane and get point even outside the map
			if (tile != null) {
				mouseDownPos = tile.MapLocation;
				lastMousePos = tile.MapLocation;
				validMouseDown = true;
				rectangle = false;
			}
		}

		private void MouseUp(MouseButtonUpEventArgs e) {
			var tile = input.GetTileUnderCursor();

			if (!validMouseDown) {
				return;
			}
			

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

				SelectionHandler?.Invoke(topLeft, bottomRight);
				//TODO: Different highlight
				Map.DisableHighlight();
			}
		  

		   
			validMouseDown = false;
		}

		private void MouseMove(MouseMovedEventArgs e) {
			//TODO: DRAW HIGHLIGHT SOMEHOW
			if (!validMouseDown) return;

			var tile = input.GetTileUnderCursor();

			if (tile == null) {
				//TODO: THIS
				return;
			}

		   
			if (validMouseDown && tile.MapLocation != mouseDownPos) {
				var endTilePos = tile.MapLocation;
				var topLeft = new IntVector2(Math.Min(mouseDownPos.X, endTilePos.X),
											 Math.Min(mouseDownPos.Y, endTilePos.Y));
				var bottomRight = new IntVector2(Math.Max(mouseDownPos.X, endTilePos.X),
												 Math.Max(mouseDownPos.Y, endTilePos.Y));

				Map.HighlightArea(topLeft, bottomRight,WorldMap.HighlightMode.Full, Color.Green);
				lastMousePos = tile.MapLocation;
				rectangle = true;
			}
		}

		
	}
}
