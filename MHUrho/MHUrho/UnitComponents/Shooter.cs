using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;
using MHUrho.Helpers;
using MHUrho.Plugins;
using MHUrho.WorldMap;
using Urho.Physics;

namespace MHUrho.UnitComponents
{
	internal delegate void HostileTargetAquiredDelegate(Shooter shooter, Unit targetUnit);
	internal delegate void ShotReloadedDelegate(Shooter shooter);
	internal delegate void ShotFiredDelegate(Shooter shooter, Projectile projectile);

	public class Shooter : DefaultComponent
	{

		public interface INotificationReceiver {
			void OnTargetAcquired(Shooter shooter, Unit targetUnit);

			void OnShotFired(Shooter shooter, Projectile projectile);

			void OnShotReloaded(Shooter shooter);
		}

		public static string ComponentName = nameof(Shooter);
		public static DefaultComponents ComponentID = DefaultComponents.Shooter;
		public override string ComponentTypeName => ComponentName;
		public override DefaultComponents ComponentTypeID => ComponentID;

		/// <summary>
		/// Shots per minute
		/// </summary>
		public float RateOfFire { get; set; }

		public bool SearchForTarget { get; set; }

		public float TargetSearchDelay { get; set; }

		internal event HostileTargetAquiredDelegate OnTargetAcquired;
		internal event ShotReloadedDelegate OnShotReloaded;
		internal event ShotFiredDelegate OnShotFired;


		private IRangeTarget target;

		private float shotDelay;
		private float searchDelay;

		private ProjectileType projectileType;

		/// <summary>
		/// Offset of the projectile source from Unit Node in the direction to the target
		/// </summary>
		private readonly float horizontalOffset;
		/// <summary>
		/// Offset of the projectile source from Unit Node vertically
		/// </summary>
		private readonly float verticalOffset;



		private Entity entity;

		private INotificationReceiver notificationReceiver;

		private IPlayer Player => entity.Player;

		private ILevelManager Level => entity.Level;

		private Map Map => Level.Map;

		private int entityID;

		protected Shooter(ILevelManager level,
						INotificationReceiver notificationReceiver,
						ProjectileType projectileType,
						float rateOfFire,
						float horizontalOffset,
						float verticalOffset) {
			this.notificationReceiver = notificationReceiver;


			this.projectileType = projectileType;
			this.RateOfFire = rateOfFire;
			this.horizontalOffset = horizontalOffset;
			this.verticalOffset = verticalOffset;
			this.shotDelay = 60 / RateOfFire;
			this.searchDelay = 0;
			ReceiveSceneUpdates = true;
		}

		protected Shooter(	ILevelManager level,
							int entityID,
							INotificationReceiver notificationReceiver,
							ProjectileType projectileType,
							float rateOfFire,
							float horizontalOffset,
							float verticalOffset)
			:this(level, notificationReceiver, projectileType, rateOfFire, horizontalOffset, verticalOffset)
		{
			this.entityID = entityID;
		}

		public static Shooter CreateNew<T>(T instancePlugin, 
										   ILevelManager level,
										   ProjectileType projectileType,
										   float rateOfFire,
										   float horizontalOffset,
										   float verticalOffset)
			where T : InstancePluginBase, INotificationReceiver {

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			return new Shooter(level, 
							   instancePlugin, 
							   projectileType, 
							   rateOfFire, 
							   horizontalOffset,
							   verticalOffset);
		}

		internal static Shooter Load(ILevelManager level, InstancePluginBase plugin, PluginData storedData) {
			var notificationReceiver = plugin as INotificationReceiver;
			if (notificationReceiver == null) {
				throw new
					ArgumentException($"provided plugin does not implement the {nameof(INotificationReceiver)} interface", nameof(plugin));
			}

			var sequentialDataReader = new SequentialPluginDataReader(storedData);
			sequentialDataReader.MoveNext();
			var entityID = sequentialDataReader.GetCurrent<int>();
			sequentialDataReader.MoveNext();
			var rateOfFire = sequentialDataReader.GetCurrent<float>();
			sequentialDataReader.MoveNext();
			var horizontalOffset = sequentialDataReader.GetCurrent<float>();
			sequentialDataReader.MoveNext();
			var verticalOffset = sequentialDataReader.GetCurrent<float>();
			sequentialDataReader.MoveNext();
			var projectileTypeID = sequentialDataReader.GetCurrent<int>();

			var shooter = new Shooter(level,
									entityID,
									notificationReceiver,
									level.PackageManager.ActiveGame.GetProjectileType(projectileTypeID),
									rateOfFire,
									horizontalOffset,
									verticalOffset);

			return shooter;
		}

		internal override void ConnectReferences(ILevelManager level) {
			entity = level.GetEntity(entityID);
		}

		public override PluginData SaveState() {
			var sequentialData = new SequentialPluginDataWriter();

			sequentialData.StoreNext<int>(entity.ID);
			sequentialData.StoreNext<float>(RateOfFire);
			sequentialData.StoreNext<float>(horizontalOffset);
			sequentialData.StoreNext<float>(verticalOffset);
			sequentialData.StoreNext<int>(projectileType.ID);

			return sequentialData.PluginData;
		}

		public override void OnAttachedToNode(Node node) {
			base.OnAttachedToNode(node);

			entity = node.GetComponent<Entity>();

			OnShotFired += notificationReceiver.OnShotFired;
			OnTargetAcquired += notificationReceiver.OnTargetAcquired;
			OnShotReloaded += notificationReceiver.OnShotReloaded;
		}

		public bool ShootAt(IRangeTarget newTarget) {
			if (!projectileType.IsInRange(entity.Position, newTarget)) return false;

			target = newTarget;
			return true;
		}

		protected override void OnUpdate(float timeStep) {
			base.OnUpdate(timeStep);
			if (!EnabledEffective) return;


			if (shotDelay > 0) {
				shotDelay -= timeStep;
				return;
			}

			OnShotReloaded?.Invoke(this);

			if (SearchForTarget && target == null && searchDelay < 0) {
				searchDelay = TargetSearchDelay;

				//Check for target in range
				var possibleTargets = Player.GetEnemyPlayers()
											.SelectMany(enemy => enemy.GetAllUnits())
											//.AsParallel()
											.Where(unit => projectileType.IsInRange(Node.Position, unit.Position))
											.OrderBy(unit => Vector3.Distance(Node.Position, unit.Position));


				foreach (var possibleTarget in possibleTargets) {

					var newTarget = possibleTarget.GetDefaultComponent<RangeTargetComponent>();
					if (newTarget == null || !projectileType.IsInRange(Node.Position, newTarget)) {
						continue;
					}

					target = newTarget;
					break;
				}

				if (target == null) {
					return;
				}
			}
			else if (searchDelay >= 0){
				searchDelay -= timeStep;
				return;
			}

			if (target != null) {
				var projectile = Level.SpawnProjectile(projectileType, Node.Position, Player, target);
				OnShotFired?.Invoke(this, projectile);

				shotDelay = 60 / RateOfFire;
			}

			
		}


		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			AddedToEntity(typeof(Shooter), entityDefaultComponents);

		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			return RemovedFromEntity(typeof(Shooter), entityDefaultComponents);
		}

	}
}
