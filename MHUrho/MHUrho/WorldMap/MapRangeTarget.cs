using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using MHUrho.Helpers;
using Urho;

namespace MHUrho.WorldMap
{
	internal class MapRangeTarget : IRangeTarget {
		public int InstanceID { get; set; }

		public bool Moving => false;

		public Vector3 CurrentPosition { get; }

		protected List<RangeTarget.IShooter> shooters;

		LevelManager level;

		protected MapRangeTarget(LevelManager level, Vector3 position) {
			this.level = level;
			this.CurrentPosition = position;
			shooters = new List<RangeTarget.IShooter>();
		}

		internal static MapRangeTarget CreateNew(LevelManager level, Vector3 position) {

			var mapTarget = new MapRangeTarget(level, position);
			level.RegisterRangeTarget(mapTarget);
			return mapTarget;
		}

		internal static MapRangeTarget Load(LevelManager level, StMapTarget storedMapTarget)
		{
			var newTarget =
				new MapRangeTarget(level, storedMapTarget.Position.ToVector3()) {InstanceID = storedMapTarget.InstanceID};
			level.LoadRangeTarget(newTarget);
			return newTarget;
		}

		public StMapTarget Save()
		{
			return new StMapTarget {InstanceID = this.InstanceID, Position = this.CurrentPosition.ToStVector3()};
		}

		public IEnumerator<Waypoint> GetWaypoints() {
			yield return new Waypoint(CurrentPosition, 0);
		}

		public void AddShooter(RangeTarget.IShooter shooter) {
			shooters.Add(shooter);
		}

		public void RemoveShooter(RangeTarget.IShooter shooter) {
			shooters.Remove(shooter);

			if (shooters.Count == 0) {
				level.UnRegisterRangeTarget(InstanceID);
				level.Map.RemoveRangeTarget(this);
			}
		}


	}
}
