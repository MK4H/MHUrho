using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using Urho;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.WorldMap;

namespace MHUrho.UnitComponents
{
	public class UnpoweredFlier : DefaultComponent
	{
		public interface INotificationReceiver {

			void OnMovementStarted(UnpoweredFlier flier);

			void OnGroundHit(UnpoweredFlier flier);
		}

		public static DefaultComponents ComponentID = DefaultComponents.UnpoweredFlier;
		public static string ComponentName = nameof(UnpoweredFlier);

		public override DefaultComponents ComponentTypeID => ComponentID;

		public override string ComponentTypeName => ComponentName;

		public Vector3 Movement { get; private set; }

		private INotificationReceiver notificationReceiver;

		private ILevelManager level;
		private Map Map => level.Map;

		protected UnpoweredFlier(INotificationReceiver notificationReceiver,
							  ILevelManager level) {
			ReceiveSceneUpdates = true;
			this.notificationReceiver = notificationReceiver;
			this.level = level;
		}

		protected UnpoweredFlier(INotificationReceiver notificationReceiver,
								 ILevelManager level,
								 Vector3 movement,
								 bool enabled) {
			ReceiveSceneUpdates = true;
			this.notificationReceiver = notificationReceiver;
			this.level = level;
			this.Movement = movement;
			this.Enabled = enabled;
		}

		public static UnpoweredFlier GetInstanceFor<T>(T instancePlugin, 
													   ILevelManager level)
			where T : InstancePluginBase, INotificationReceiver
		{
			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			return new UnpoweredFlier(instancePlugin, level);
		}

		internal static UnpoweredFlier Load(ILevelManager level, InstancePluginBase plugin, PluginData data) {

			var notificationReceiver = plugin as INotificationReceiver;
			if (notificationReceiver == null) {
				throw new
					ArgumentException($"provided plugin does not implement the {nameof(INotificationReceiver)} interface", nameof(plugin));
			}

			var sequentialData = new SequentialPluginDataReader(data);
			var movement = sequentialData.GetCurrent<Vector3>();
			sequentialData.MoveNext();
			var enabled = sequentialData.GetCurrent<bool>();
			sequentialData.MoveNext();

			return new UnpoweredFlier(notificationReceiver,
									  level,
									  movement,
									  enabled);
		}

		internal override void ConnectReferences(ILevelManager level) {
			//NOTHING
		}

		/// <summary>
		/// Calculates the movement vectors for projectile with initial speed <paramref name="initialProjectileSpeed"/>, to go from <paramref name="sourcePosition"/> to <paramref name="targetPosition"/>
		/// 
		/// </summary>
		/// <param name="targetPosition"></param>
		/// <param name="sourcePosition"></param>
		/// <param name="initialProjectileSpeed"></param>
		/// <param name="lowTime"></param>
		/// <param name="lowVector"></param>
		/// <param name="highTime"></param>
		/// <param name="highVector"></param>
		/// <returns>True if it is possible to hit the <paramref name="targetPosition"/> with the given <paramref name="initialProjectileSpeed"/>,
		/// and the out parameters are valid, or false if it is not possible and the out params are invalid</returns>
		public static bool GetUnpoweredProjectileTimesAndAngles(Vector3 targetPosition,
																Vector3 sourcePosition,
																float initialProjectileSpeed,
																out float lowTime,
																out Vector3 lowVector,
																out float highTime,
																out Vector3 highVector) {
			//Source https://blog.forrestthewoods.com/solving-ballistic-trajectories-b0165523348c
			// https://en.wikipedia.org/wiki/Projectile_motion

			//TODO: Try this https://gamedev.stackexchange.com/questions/114522/how-can-i-launch-a-gameobject-at-a-target-if-i-am-given-everything-except-for-it

			var diff = targetPosition - sourcePosition;
			Vector3 directionXZ = diff.XZ();
			directionXZ.Normalize();


			var v2 = initialProjectileSpeed * initialProjectileSpeed;
			var v4 = initialProjectileSpeed * initialProjectileSpeed * initialProjectileSpeed * initialProjectileSpeed;

			var y = diff.Y;
			var x = diff.XZ2().Length;

			var g = 10f;

			var root = v4 - g * (g * x * x + 2 * y * v2);

			if (root < 0) {
				//TODO: No solution, cant do
				lowTime = 0;
				lowVector = Vector3.Zero;
				highTime = 0;
				highVector = Vector3.Zero;
				return false;
			}

			root = (float)Math.Sqrt(root);

			float lowAngle = (float)Math.Atan2(v2 - root, g * x);
			float highAngle = (float)Math.Atan2(v2 + root, g * x);


			lowVector = (directionXZ * (float)Math.Cos(lowAngle) +
						 Vector3.UnitY * (float)Math.Sin(lowAngle)) * initialProjectileSpeed;

			highVector = (directionXZ * (float)Math.Cos(highAngle) +
						  Vector3.UnitY * (float)Math.Sin(highAngle)) * initialProjectileSpeed;

			lowTime = x / lowVector.XZ2().Length;
			highTime = x / highVector.XZ2().Length;


			return true;
		}

		public void StartFlight(Vector3 initialMovement) {
			Enabled = true;

			Movement = initialMovement;
		}

		public override PluginData SaveState() {
			var sequentialData = new SequentialPluginDataWriter();
			sequentialData.StoreNext(Movement);
			sequentialData.StoreNext(Enabled);
			return sequentialData.PluginData;
		}

		protected override void OnUpdate(float timeStep) {
			base.OnUpdate(timeStep);

			if (!EnabledEffective) return;

			if (Map.IsInside(Node.Position)) {
				Node.Position += Movement * timeStep;
				Node.LookAt(Node.Position + Movement, Vector3.UnitY);

				Movement += (-Vector3.UnitY * 10) * timeStep;
			}
			else {
				//Stop movement
				Movement = Vector3.Zero;
				notificationReceiver.OnGroundHit(this);
			}
		}
	}
}
