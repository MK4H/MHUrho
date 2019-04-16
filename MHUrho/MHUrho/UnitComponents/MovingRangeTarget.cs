using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.UnitComponents
{

	public class MovingRangeTarget : RangeTargetComponent {

		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => MovingRangeTarget;

			public MovingRangeTarget MovingRangeTarget { get; private set; }

			readonly LevelManager level;
			readonly InstancePlugin plugin;
			readonly StDefaultComponent storedData;

			public Loader() {

			}

			protected Loader(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData)
			{
				this.level = level;
				this.plugin = plugin;
				this.storedData = storedData;
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

			public override void StartLoading() {
				var user = plugin as IUser;
				if (user == null) {
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(IUser)} interface", nameof(plugin));
				}

				if (storedData.ComponentCase != StDefaultComponent.ComponentOneofCase.MovingRangeTarget) {
					throw new ArgumentException("Invalid component type data passed to loader", nameof(storedData));
				}

				var storedMovingRangeTarget = storedData.MovingRangeTarget;


				MovingRangeTarget = new MovingRangeTarget(storedMovingRangeTarget.InstanceID,
														level,
														user,
														storedMovingRangeTarget.Offset.ToVector3())
									{
										Enabled = storedMovingRangeTarget.Enabled
									};
				level.LoadRangeTarget(MovingRangeTarget);
			}

			public override void ConnectReferences() {

			}

			public override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData)
			{
				return new Loader(level, plugin, storedData);
			}
		}

		public interface IUser {

			/// <summary>
			/// Gets waypoints beginning with the current position with the time 0,
			/// and all the remaining waypoints
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			IEnumerable<Waypoint> GetFutureWaypoints(MovingRangeTarget target);
		}


		public override bool Moving => true;

		public override Vector3 CurrentPosition => Entity.Position + Entity.Node.Rotation * offset;

		readonly IUser user;

		/// <summary>
		/// Offset from <see cref="IEntity.Position"/> in the entity space (rotates with entity)
		/// </summary>
		readonly Vector3 offset;

		protected MovingRangeTarget(ILevelManager level, IUser user, Vector3 offset)
			:base(level)
		{
			this.user = user;
			this.offset = offset;
		}

		protected MovingRangeTarget(int ID, ILevelManager level, IUser user, Vector3 offset)
			: base(ID, level)
		{
			this.user = user;
			this.offset = offset;
		}

		public static MovingRangeTarget CreateNew<T>(T plugin, ILevelManager level, Vector3 offset)
			where T : EntityInstancePlugin, IUser {

			if (plugin == null) {
				throw new ArgumentNullException(nameof(plugin));
			}



			var newInstance = new MovingRangeTarget(level, plugin, offset);
			((LevelManager)level).RegisterRangeTarget(newInstance);
			plugin.Entity.AddComponent(newInstance);
			return newInstance;
		}

		public override IEnumerable<Waypoint> GetFutureWaypoints() {
			return user.GetFutureWaypoints(this);
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
