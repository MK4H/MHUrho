using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.UnitComponents
{
	public delegate IEnumerator<Waypoint> GetWaypointsDelegate(MovingRangeTarget target);


	public class MovingRangeTarget : RangeTargetComponent {

		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => MovingRangeTarget;

			public MovingRangeTarget MovingRangeTarget { get; private set; }

			public Loader() {

			}

			public static StDefaultComponent SaveState(MovingRangeTarget movingRangeTarget)
			{
				var storedMovingRangeTarget = new StMovingRangeTarget
											{
												Enabled = movingRangeTarget.Enabled,
												InstanceID = movingRangeTarget.InstanceID,
												Offset = movingRangeTarget.offset.ToStVector3()
											};
				return new StDefaultComponent {MovingRangeTarget = storedMovingRangeTarget};
			}

			public override void StartLoading(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData) {
				var user = plugin as IUser;
				if (user == null) {
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(IUser)} interface", nameof(plugin));
				}

				if (storedData.ComponentCase != StDefaultComponent.ComponentOneofCase.MovingRangeTarget) {
					throw new ArgumentException("Invalid component type data passed to loader", nameof(storedData));
				}

				var storedMovingRangeTarget = storedData.MovingRangeTarget;

				user.GetMandatoryDelegates(out GetWaypointsDelegate getWaypoints);

				MovingRangeTarget = new MovingRangeTarget(storedMovingRangeTarget.InstanceID,
														level,
														storedMovingRangeTarget.Offset.ToVector3(),
														getWaypoints)
									{
										Enabled = storedMovingRangeTarget.Enabled
									};
				level.LoadRangeTarget(MovingRangeTarget);
			}

			public override void ConnectReferences(LevelManager level) {

			}

			public override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone()
			{
				return new Loader();
			}
		}

		public interface IUser {

			void GetMandatoryDelegates(out GetWaypointsDelegate getWaypoints);

		}


		public override bool Moving => true;

		public override Vector3 CurrentPosition => Entity.Position + Entity.Node.Rotation * offset;

		readonly GetWaypointsDelegate getWaypoints;

		/// <summary>
		/// Offset from <see cref="IEntity.Position"/> in the entity space (rotates with entity)
		/// </summary>
		readonly Vector3 offset;

		protected MovingRangeTarget(ILevelManager level, Vector3 offset, GetWaypointsDelegate getWaypoints)
			:base(level)
		{
			this.offset = offset;
			//TODO: Check that delegates are not null
			this.getWaypoints = getWaypoints;

		}

		protected MovingRangeTarget(int ID, ILevelManager level, Vector3 offset, GetWaypointsDelegate getWaypoints)
			: base(ID, level)
		{
			this.offset = offset;
			//TODO: Check that delegates are not null
			this.getWaypoints = getWaypoints;
		}

		public static MovingRangeTarget CreateNew<T>(T instancePlugin, ILevelManager level, Vector3 offset)
			where T : InstancePlugin, IUser {

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			instancePlugin.GetMandatoryDelegates(out GetWaypointsDelegate getWaypoints);

			var newTarget = new MovingRangeTarget(level, offset, getWaypoints);
			((LevelManager)level).RegisterRangeTarget(newTarget);
			return newTarget;
		}

		public override IEnumerator<Waypoint> GetWaypoints() {
			return getWaypoints(this);
		}

		public override StDefaultComponent SaveState() {
			return Loader.SaveState(this);
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.AddedToEntity(entityDefaultComponents);
			AddedToEntity(typeof(MovingRangeTarget), entityDefaultComponents);

			Entity.PositionChanged += TargetMoved;
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			Entity.PositionChanged -= TargetMoved;

			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(MovingRangeTarget), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}

	}
}
