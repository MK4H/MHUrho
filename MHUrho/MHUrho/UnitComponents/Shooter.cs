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

		public interface INotificationReciever {
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

		//TODO: Change to Target component
		private RangeTargetComponent target;

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

		private ILevelManager level;

		private IPlayer player;

		private INotificationReciever notificationReciever;

		private Map Map => level.Map;

		protected Shooter(ILevelManager level,
						  INotificationReciever notificationReciever,
						  IPlayer player,
						  ProjectileType projectileType,
						  float rateOfFire,
						  float horizontalOffset,
						  float verticalOffset) {
			this.notificationReciever = notificationReciever;
			this.level = level;
			this.player = player;
			this.projectileType = projectileType;
			this.RateOfFire = rateOfFire;
			this.horizontalOffset = horizontalOffset;
			this.verticalOffset = verticalOffset;
			this.shotDelay = 60 / RateOfFire;
			this.searchDelay = 0;
			ReceiveSceneUpdates = true;
		}

		public static Shooter CreateNew<T>(T instancePlugin, 
										   ILevelManager level,
										   IPlayer player,
										   ProjectileType projectileType,
										   float rateOfFire,
										   float horizontalOffset,
										   float verticalOffset)
			where T : InstancePluginBase, INotificationReciever {

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			return new Shooter(level, 
							   instancePlugin, 
							   player,
							   projectileType, 
							   rateOfFire, 
							   horizontalOffset,
							   verticalOffset);
		}

		internal static Shooter Load(ILevelManager level, InstancePluginBase plugin, PluginData storedData) {
			var notificationReciever = plugin as INotificationReciever;
			if (notificationReciever == null) {
				throw new
					ArgumentException($"provided plugin does not implement the {nameof(INotificationReciever)} interface", nameof(plugin));
			}

			var sequentialDataReader = new SequentialPluginDataReader(storedData);
			sequentialDataReader.MoveNext();
			var playerID = sequentialDataReader.GetCurrent<int>();
			sequentialDataReader.MoveNext();
			var rateOfFire = sequentialDataReader.GetCurrent<float>();
			sequentialDataReader.MoveNext();
			var horizontalOffset = sequentialDataReader.GetCurrent<float>();
			sequentialDataReader.MoveNext();
			var verticalOffset = sequentialDataReader.GetCurrent<float>();
			sequentialDataReader.MoveNext();
			var projectileTypeID = sequentialDataReader.GetCurrent<int>();
			return new Shooter(level,
							   notificationReciever,
							   level.GetPlayer(playerID),
							   level.PackageManager.ActiveGame.GetProjectileType(projectileTypeID),
							   rateOfFire,
							   horizontalOffset,
							   verticalOffset);
		}

		internal override void ConnectReferences(ILevelManager level) {
			//NOTHING
		}

		public override PluginData SaveState() {
			var sequentialData = new SequentialPluginDataWriter();

			sequentialData.StoreNext<int>(player.ID);
			sequentialData.StoreNext<float>(RateOfFire);
			sequentialData.StoreNext<float>(horizontalOffset);
			sequentialData.StoreNext<float>(verticalOffset);
			sequentialData.StoreNext<int>(projectileType.ID);

			return sequentialData.PluginData;
		}

		public override void OnAttachedToNode(Node node) {
			base.OnAttachedToNode(node);

			OnShotFired += notificationReciever.OnShotFired;
			OnTargetAcquired += notificationReciever.OnTargetAcquired;
			OnShotReloaded += notificationReciever.OnShotReloaded;
		}

		protected override void OnUpdate(float timeStep) {
			base.OnUpdate(timeStep);
			if (!EnabledEffective) return;


			if (shotDelay > 0) {
				shotDelay -= timeStep;
				return;
			}

			OnShotReloaded?.Invoke(this);

			if (target == null && searchDelay < 0) {
				searchDelay = TargetSearchDelay;

				//Check for target in range
				var possibleTargets = player.GetEnemyPlayers()
											.SelectMany(enemy => enemy.GetAllUnits())
											.AsParallel()
											.Where(unit => projectileType.IsInRange(Node.Position, unit.Position))
											.OrderBy(unit => Vector3.Distance(Node.Position, unit.Position));


				foreach (var possibleTarget in possibleTargets) {
					var newTarget = possibleTarget.GetComponent<RangeTargetComponent>();
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

			var projectile = level.SpawnProjectile(projectileType, Node.Position, player, target);
			OnShotFired?.Invoke(this, projectile);

			shotDelay = 60 / RateOfFire;
		}

		
	}
}
