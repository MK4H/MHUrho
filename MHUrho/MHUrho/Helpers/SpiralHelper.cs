using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Helpers
{
	

    public class Spiral : IEnumerable<IntVector2>
    {
		public class SpiralEnumerator : IEnumerator<IntVector2> {

			public IntVector2 Current { get; private set; }

			object IEnumerator.Current => Current;

			public int ContainingSquareSize {
				get {
					IntVector2 diff = Current - spiral.center;
					return Math.Max(Math.Abs(diff.X), Math.Abs(diff.Y)) * 2 + 1;
				}
			}

			public IntVector2 Center => spiral.center;

			readonly Spiral spiral;
			IntVector2 spiralCoords = new IntVector2(0, 0);
			IntVector2 d = new IntVector2(0, -1);

			public SpiralEnumerator(Spiral spiral) {
				this.spiral = spiral;
			}

			public bool MoveNext() {

				Current = spiral.center + spiralCoords;

				//Move the spiral coords
				if (spiralCoords.X == spiralCoords.Y ||
					(spiralCoords.X < 0 && spiralCoords.X == -spiralCoords.Y) ||
					(spiralCoords.X > 0 && spiralCoords.X == 1 - spiralCoords.Y)) {
					int tmp = d.X;
					d.X = -d.Y;
					d.Y = tmp;
				}

				spiralCoords += d;
				return true;
			}

			public void Reset() {
				spiralCoords = new IntVector2(0, 0);
				d = new IntVector2(0, -1);
			}

			public IntRect GetContainingSquare()
			{
				IntVector2 diff = Current - spiral.center;
				int diffMax = Math.Max(Math.Abs(diff.X), Math.Abs(diff.Y));
				return new IntRect(Center.X - diffMax, Center.Y - diffMax, Center.X + diffMax, Center.Y + diffMax);
			}

			public void Dispose() {

			}

			
		}

		readonly IntVector2 center;

		public Spiral(IntVector2 center)
		{
			this.center = center;
		}

		public IEnumerator<IntVector2> GetEnumerator()
		{
			return GetSpiralEnumerator();
		}

		public SpiralEnumerator GetSpiralEnumerator()
		{
			return new SpiralEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
