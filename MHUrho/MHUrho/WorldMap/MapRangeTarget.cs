using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.PathFinding;
using MHUrho.PathFinding.AStar;
using Urho;

namespace MHUrho.WorldMap
{
	class MapRangeTarget : IRangeTarget {
		public int InstanceID { get; set; }

		public bool Moving => false;
		//Map targets never move
		public event TargetMovedDelegate TargetMoved;

		public Vector3 CurrentPosition { get; }

		protected List<RangeTargetComponent.IShooter> shooters;

		readonly LevelManager level;
		readonly Map map;

		protected MapRangeTarget(LevelManager level, Map map, Vector3 position) {
			this.level = level;
			this.map = map;
			this.CurrentPosition = position;
			shooters = new List<RangeTargetComponent.IShooter>();
		}

		internal static MapRangeTarget CreateNew(LevelManager level, Map map, Vector3 position) {

			var mapTarget = new MapRangeTarget(level, map, position);
			level.RegisterRangeTarget(mapTarget);
			return mapTarget;
		}

		internal static MapRangeTarget Load(LevelManager level, Map map, StMapTarget storedMapTarget)
		{
			var newTarget =
				new MapRangeTarget(level, map, storedMapTarget.Position.ToVector3()) {InstanceID = storedMapTarget.InstanceID};
			level.LoadRangeTarget(newTarget);
			return newTarget;
		}

		public StMapTarget Save()
		{
			return new StMapTarget {InstanceID = this.InstanceID, Position = this.CurrentPosition.ToStVector3()};
		}

		public IEnumerable<Waypoint> GetFutureWaypoints() {
			yield return new Waypoint(new TempNode(CurrentPosition, level.Map), 0, MovementType.Linear);
		}

		public void AddShooter(RangeTargetComponent.IShooter shooter) {
			shooters.Add(shooter);
		}

		public void RemoveShooter(RangeTargetComponent.IShooter shooter) {
			shooters.Remove(shooter);

			if (shooters.Count == 0) {
				level.UnRegisterRangeTarget(InstanceID);
				map.RemoveRangeTarget(this);
			}
		}


	}
}
