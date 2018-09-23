using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using Urho;

namespace MHUrho.EditorTools
{
	public struct StaticSquareChangedArgs {
		
		public ITile CenterTile { get; private set; }
		public int EdgeSize { get; private set; }
		public IntVector2 Size => new IntVector2(EdgeSize, EdgeSize);
		/// <summary>
		/// Square of tile MapLocations
		/// </summary>
		public IntRect Square {
			get {
				IntVector2 topLeft = CenterTile.TopLeft - (Size / 2);
				IntVector2 bottomRight = topLeft + Size - new IntVector2(1, 1);
				return new IntRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
			}
		}

		public StaticSquareChangedArgs(ITile centerTile, int edgeSize)
		{
			this.CenterTile = centerTile;
			this.EdgeSize = edgeSize;
		}
	}

	public abstract class StaticSquareTool : Tool {
		public event Action<StaticSquareChangedArgs> SquareChanged;

		protected StaticSquareTool(IGameController input)
			: base(input, new IntRect(0,0,0,0))
		{

		}

		protected virtual void OnSquareChanged(ITile centerTile, int size)
		{
			SquareChanged?.Invoke(new StaticSquareChangedArgs(centerTile, size));
		}
	}
}
