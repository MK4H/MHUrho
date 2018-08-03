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
	public class StaticRangeTarget : RangeTargetComponent {

		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => StaticRangeTarget;

			public StaticRangeTarget StaticRangeTarget { get; private set; }

			public Loader() {

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

			public override void StartLoading(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData) {

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

			public override void ConnectReferences(LevelManager level) {

			}

			public override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone() {
				return new Loader();
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

		public static StaticRangeTarget CreateNew(ILevelManager level, Vector3 position) {

			var newTarget = new StaticRangeTarget(level, position);

			((LevelManager)level).RegisterRangeTarget(newTarget);

			return newTarget;
		}

		public override StDefaultComponent SaveState() {
			return Loader.SaveState(this);
		}

		public override IEnumerator<Waypoint> GetWaypoints() {
			yield return new Waypoint(new TempNode(CurrentPosition), 0, MovementType.Linear);
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
