using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.PathFinding.AStar;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.UnitComponents
{
	public class StaticRangeTarget : RangeTargetComponent {

		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => StaticRangeTarget;

			public StaticRangeTarget StaticRangeTarget { get; private set; }

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

			public static StDefaultComponent SaveState(StaticRangeTarget staticRangeTarget)
			{
				var storedStaticRangeTarget = new StStaticRangeTarget
											{
												Enabled = staticRangeTarget.Enabled,
												InstanceID = staticRangeTarget.InstanceID,
												Position = staticRangeTarget.CurrentPosition.ToStVector3()
											};

				return new StDefaultComponent {StaticRangeTarget = storedStaticRangeTarget};
			}

			public override void StartLoading() {

				if (storedData.ComponentCase != StDefaultComponent.ComponentOneofCase.StaticRangeTarget) {
					throw new ArgumentException("Invalid component type data passed to loader", nameof(storedData));
				}

				var storedStaticRangeTarget = storedData.StaticRangeTarget;

				StaticRangeTarget = new StaticRangeTarget(storedStaticRangeTarget.InstanceID,
														level, 
														storedStaticRangeTarget.Position.ToVector3())
									{
										Enabled = storedStaticRangeTarget.Enabled
									};
				level.LoadRangeTarget(StaticRangeTarget);
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

		public override bool Moving => false;

		public override Vector3 CurrentPosition { get; }


		protected StaticRangeTarget(int instanceID, ILevelManager level, Vector3 position)
			: base(instanceID, level)
		{
			this.CurrentPosition = position;
		}

		protected StaticRangeTarget(ILevelManager level, Vector3 position) 
			:base(level)
		{
			this.CurrentPosition = position;
		}

		public static StaticRangeTarget CreateNew(EntityInstancePlugin plugin, ILevelManager level, Vector3 position) {

			var newInstance = new StaticRangeTarget(level, position);

			((LevelManager)level).RegisterRangeTarget(newInstance);
			plugin.Entity.AddComponent(newInstance);

			return newInstance;
		}

		public override StDefaultComponent SaveState() {
			return Loader.SaveState(this);
		}

		public override IEnumerable<Waypoint> GetFutureWaypoints() {
			yield return new Waypoint(new TempNode(CurrentPosition, Map), 0, MovementType.Linear);
		}


		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.AddedToEntity(entityDefaultComponents);
			AddedToEntity(typeof(StaticRangeTarget), entityDefaultComponents);
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(StaticRangeTarget), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}
	}
}
