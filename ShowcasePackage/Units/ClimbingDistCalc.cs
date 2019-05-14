using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace ShowcasePackage.Units
{
	class ClimbingDistCalc : BaseDistCalc {
		float baseCoef;
		float angleCoef;

		public ClimbingDistCalc(float baseCoef, float angleCoef)
		{
			this.baseCoef = baseCoef;
			this.angleCoef = angleCoef;
		}

		public override float GetMinimalAproxTime(Vector3 source, Vector3 target)
		{
			return base.GetMinimalAproxTime(source, target) * baseCoef;
		}

		protected override float GetTime(Vector3 source, Vector3 to)
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
			return (diff.Length / 2) * baseCoef + angle * angleCoef;
			
		}
	}
}
