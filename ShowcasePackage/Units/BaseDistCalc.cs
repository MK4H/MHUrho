using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers.Extensions;
using MHUrho.PathFinding;
using MHUrho.PathFinding.AStar;
using Urho;

namespace ShowcasePackage.Units
{
	abstract class BaseDistCalc : NodeDistCalculator
	{

		public override float GetMinimalAproxTime(Vector3 source, Vector3 target)
		{
			return (source.XZ2() - target.XZ2()).Length / 2;
		}

		protected override bool GetTime(ITileNode source, ITileNode target, out float time)
		{
			Vector3 edgePosition = source.GetEdgePosition(target);
			time = GetTime(source.Position, edgePosition) + GetTime(edgePosition, target.Position);
			return true;
		}

		protected override bool GetTime(ITempNode source, IBuildingNode target, out float time)
		{
			time = GetTime(source.Position, target.Position);
			return true;
		}

		protected override bool GetTime(ITempNode source, ITempNode target, out float time)
		{
			time = GetTime(source.Position, target.Position);
			return true;
		}

		protected override bool GetTime(IBuildingNode source, ITempNode target, out float time)
		{
			time = GetTime(source.Position, target.Position);
			return true;
		}

		protected override bool GetTime(ITempNode source, ITileNode target, out float time)
		{
			time = GetTime(source.Position, target.Position);
			return true;
		}

		protected override bool GetTime(IBuildingNode source, IBuildingNode target, out float time)
		{
			time = GetTime(source.Position, target.Position);
			return true;
		}

		protected override bool GetTime(ITileNode source, IBuildingNode target, out float time)
		{
			time = 1;
			return true;
		}

		protected override bool GetTime(ITileNode source, ITempNode target, out float time)
		{
			time = GetTime(source.Position, target.Position);
			return true;
		}

		protected override bool GetTime(IBuildingNode source, ITileNode target, out float time)
		{
			time = 1;
			return true;
		}

		protected abstract float GetTime(Vector3 source, Vector3 to);



	}
}
