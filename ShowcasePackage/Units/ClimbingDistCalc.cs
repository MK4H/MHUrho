using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers.Extensions;
using Urho;

namespace ShowcasePackage.Units
{
	public abstract class ClimbingDistCalc : BaseDistCalc {
		
		/// <summary>
		/// Base speed of linear motion
		/// </summary>
		public float BaseCoef { get; set; }

		/// <summary>
		/// Coefficient of the speed change based on angle.
		/// </summary>
		public float AngleCoef { get; set; }




		public ClimbingDistCalc(float baseCoef, float angleCoef)
		{
			this.BaseCoef = baseCoef;
			this.AngleCoef = angleCoef;
		}

		public override float GetMinimalAproxTime(Vector3 source, Vector3 target)
		{
			return ((source.XZ2() - target.XZ2()).Length / 2) * BaseCoef;
		}

		protected override float GetLinearTime(Vector3 source, Vector3 to)
		{
			//Check for complete equality, which breaks the code below
			if (source == to)
			{
				return 0;
			}

			Vector3 diff = to - source;

			//In radians
			float angle = (float)Math.Max(Math.Asin(Math.Abs(diff.Y) / diff.Length), 0);

			//NOTE: Maybe cache the Length in the Edge
			return (diff.Length / 2) * BaseCoef + angle * AngleCoef;

		}
	}
}
