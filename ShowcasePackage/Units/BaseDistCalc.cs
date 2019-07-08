using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers.Extensions;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.PathFinding.AStar;
using MHUrho.WorldMap;
using Urho;

namespace ShowcasePackage.Units
{
	public abstract class BaseDistCalc : NodeDistCalculator
	{
	
		protected sealed override bool GetTime(ITileNode source, ITileNode target, MovementType movementType, out float time)
		{
			switch (movementType)
			{
				case MovementType.Linear:
					if (!CanPass(source, target)) {
						time = -1;
						return false;
					}
					Vector3 edgePosition = source.GetEdgePosition(target);
					time = GetLinearTime(source.Position, edgePosition) + GetLinearTime(edgePosition, target.Position);
					return true;
				case MovementType.Teleport:
					if (!CanTeleport(source, target)) {
						time = -1;
						return false;
					}
					time = GetTeleportTime(source, target);
					return true;
				default:
					time = -1;
					return false;
			}
		}

		protected sealed override bool GetTime(ITempNode source, IBuildingNode target, MovementType movementType, out float time)
		{
			switch (movementType)
			{
				case MovementType.Linear:
					time = GetLinearTime(source.Position, target.Position);
					return true;
				case MovementType.Teleport:
					time = GetTeleportTime(source, target);
					return true;
				default:
					time = -1;
					return false;
			}
		}

		protected sealed override bool GetTime(ITempNode source, ITempNode target, MovementType movementType, out float time)
		{
			switch (movementType)
			{
				case MovementType.Linear:
					time = GetLinearTime(source.Position, target.Position);
					return true;
				case MovementType.Teleport:
					time = GetTeleportTime(source, target);
					return true;
				default:
					time = -1;
					return false;
			}
		}

		protected sealed override bool GetTime(IBuildingNode source, ITempNode target, MovementType movementType, out float time)
		{
			switch (movementType)
			{
				case MovementType.Linear:
					time = GetLinearTime(source.Position, target.Position);
					return true;
				case MovementType.Teleport:
					time = GetTeleportTime(source, target);
					return true;
				default:
					time = -1;
					return false;
			}
		}

		protected sealed override bool GetTime(ITempNode source, ITileNode target, MovementType movementType, out float time)
		{
			switch (movementType)
			{
				case MovementType.Linear:
					time = GetLinearTime(source.Position, target.Position);
					return true;
				case MovementType.Teleport:
					time = GetTeleportTime(source, target);
					return true;
				default:
					time = -1;
					return false;
			}
		}

		protected sealed override bool GetTime(IBuildingNode source, IBuildingNode target, MovementType movementType, out float time)
		{
			switch (movementType)
			{
				case MovementType.Linear:
					if (!CanPass(source, target))
					{
						time = -1;
						return false;
					}
					time = GetLinearTime(source.Position, target.Position);
					return true;
				case MovementType.Teleport:
					if (!CanTeleport(source, target))
					{
						time = -1;
						return false;
					}
					time = GetTeleportTime(source, target);
					return true;
				default:
					time = -1;
					return false;
			}
		}

		protected sealed override bool GetTime(ITileNode source, IBuildingNode target, MovementType movementType, out float time)
		{
			switch (movementType)
			{
				case MovementType.Linear:
					if (!CanPass(source, target))
					{
						time = -1;
						return false;
					}
					time = GetLinearTime(source.Position, target.Position);
					return true;
				case MovementType.Teleport:
					if (!CanTeleport(source, target))
					{
						time = -1;
						return false;
					}
					time = GetTeleportTime(source, target);
					return true;
				default:
					time = -1;
					return false;
			}
		}

		protected sealed override bool GetTime(ITileNode source, ITempNode target, MovementType movementType, out float time)
		{
			switch (movementType)
			{
				case MovementType.Linear:
					time = GetLinearTime(source.Position, target.Position);
					return true;
				case MovementType.Teleport:
					time = GetTeleportTime(source, target);
					return true;
				default:
					time = -1;
					return false;
			}
		}

		protected sealed override bool GetTime(IBuildingNode source, ITileNode target, MovementType movementType, out float time)
		{
			switch (movementType)
			{
				case MovementType.Linear:
					if (!CanPass(source, target))
					{
						time = -1;
						return false;
					}
					time = GetLinearTime(source.Position, target.Position);
					return true;
				case MovementType.Teleport:
					if (!CanTeleport(source, target))
					{
						time = -1;
						return false;
					}
					time = GetTeleportTime(source, target);
					return true;
				default:
					time = -1;
					return false;
			}
		}

		protected abstract float GetLinearTime(Vector3 source, Vector3 to);

		protected abstract float GetTeleportTime(INode source, INode target);


		protected abstract bool CanPass(ITileNode source, ITileNode target);
		protected abstract bool CanPass(ITileNode source, IBuildingNode target);
		protected abstract bool CanPass(IBuildingNode source, ITileNode target);
		protected abstract bool CanPass(IBuildingNode source, IBuildingNode target);

		protected abstract bool CanTeleport(ITileNode source, ITileNode target);
		protected abstract bool CanTeleport(ITileNode source, IBuildingNode target);
		protected abstract bool CanTeleport(IBuildingNode source, ITileNode target);
		protected abstract bool CanTeleport(IBuildingNode source, IBuildingNode target);

	}
}
