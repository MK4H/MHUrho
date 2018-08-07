using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.EditorTools.TerrainManipulation
{
    class VertexSelector : TerrainManipulator
    {

		public IEnumerable<IntVector2> SelectedVerticies => verticies;

		readonly List<IntVector2> verticies;

		readonly IMap map;
		readonly IGameController input;

		public VertexSelector(IMap map, IGameController input)
		{
			this.map = map;
			this.input = input;

			verticies = new List<IntVector2>();
		}

		public override void Dispose()
		{
			
		}

		public override void OnEnabled()
		{
			map.HighlightCornerList(SelectedVerticies, Color.Green);
		}

		public override void OnDisabled()
		{
			map.DisableHighlight();
		}

		public override void OnMouseDown(MouseButtonDownEventArgs args)
		{
			var raycastResult = input.CursorRaycast();
			var vertex = map.RaycastToVertex(raycastResult);
			if (vertex.HasValue) {
				//TODO: this is slow, make it faster
				if (!verticies.Remove(vertex.Value)) {
					verticies.Add(vertex.Value);
				}

				map.HighlightCornerList(SelectedVerticies, Color.Green);
			}
		}
	}
}
