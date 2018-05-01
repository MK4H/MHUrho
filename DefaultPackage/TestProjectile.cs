using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Urho;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using MHUrho.Helpers;

namespace DefaultPackage
{
	public class TestProjectileType : ProjectileTypePluginBase {
		public float Speed { get;private set; }

		public override bool IsMyType(string typeName) {
			return typeName == "TestProjectile";
		}

		public override ProjectileInstancePluginBase CreateNewInstance(ILevelManager level, Projectile projectile) {
			return new TestProjectileInstance(level, projectile, this);
		}

		public override ProjectileInstancePluginBase GetInstanceForLoading()
		{
			return new TestProjectileInstance(this);
		}

		public override bool IsInRange(Vector3 source, IRangeTarget target) {

			return BallisticProjectile.GetTimesAndVectorsForStaticTarget(target.CurrentPosition,
																		source, 
																		Speed,
																		out var loweTime,
																		out var lowVector,
																		out var highTime,
																		out var highVector);
			
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {
			var speedElement = XmlHelpers.GetChild(extensionElement, "speed");
			Speed = XmlHelpers.GetFloat(speedElement);
		}

	}

	public class TestProjectileInstance : ProjectileInstancePluginBase, BallisticProjectile.INotificationReceiver {
		private static readonly Random seedRng = new Random();

		private BallisticProjectile flier;
		private TestProjectileType myType;

		private const float baseTimeToSplit = 0.5f;
		private float timeToSplit = baseTimeToSplit;

		private Random rng;

		private int splits = 10;

		private bool despawning;
		private float timeToDespawn = 6;

		public TestProjectileInstance(TestProjectileType type)
		{
			this.myType = type;
			this.rng = new Random(seedRng.Next());
		}

		public TestProjectileInstance(ILevelManager level, Projectile projectile, TestProjectileType type)
			:base (level, projectile)
		{
			this.rng = new Random(seedRng.Next());
			flier = BallisticProjectile.GetInstanceFor(this, level);
			projectile.Node.AddComponent(flier);
			myType = type;
		}

		public override void OnUpdate(float timeStep) {

			if (despawning) {
				timeToDespawn -= timeStep;

				if (timeToDespawn < 0) {
					projectile.Despawn();
					return;
				}
			}

			timeToSplit -= timeStep;
			if (timeToSplit > 0) return;

			timeToSplit = baseTimeToSplit;

			for (int i = 0; i < splits; i++) {
				var movement = flier.Movement;

				movement = new Quaternion((float)rng.NextDouble() * 5, (float)rng.NextDouble() * 5, (float)rng.NextDouble() * 5) * movement;

				var newProjectile = Level.SpawnProjectile(projectile.ProjectileType, projectile.Position, projectile.Player,  movement);
				((TestProjectileInstance) newProjectile.Plugin).splits = 0;
				
			}

			if (splits != 0) {
				splits = 0;
				projectile.Despawn();
			}
			
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var sequential = pluginData.GetWriterForWrappedSequentialData();
			sequential.StoreNext(timeToSplit);
			sequential.StoreNext(splits);
			sequential.StoreNext(despawning);
			sequential.StoreNext(timeToDespawn);
		}

		public override void LoadState(ILevelManager level, Projectile projectile, PluginDataWrapper pluginData) {
			this.Level = level;
			this.projectile = projectile;
			this.flier = projectile.GetDefaultComponent<BallisticProjectile>();

			var sequential = pluginData.GetReaderForWrappedSequentialData();
			sequential.MoveNext();
			timeToSplit = sequential.GetCurrent<float>();
			sequential.MoveNext();
			splits = sequential.GetCurrent<int>();
			sequential.MoveNext();
			despawning = sequential.GetCurrent<bool>();
			sequential.MoveNext();
			timeToDespawn = sequential.GetCurrent<float>();
			sequential.MoveNext();
		}

		public override void ReInitialize(ILevelManager level) {
			timeToSplit = baseTimeToSplit;
			splits = 10;
			timeToDespawn = 6;
			despawning = false;
		}

		public override bool ShootProjectile(IRangeTarget target) {
			
			if (BallisticProjectile.GetTimesAndVectorsForStaticTarget(target.CurrentPosition,
																	projectile.Position,
																	myType.Speed,
																	out var lowTime,
																	out var lowVector,
																	out var highTime,
																	out var highVector)) {
				flier.StartFlight(lowVector);
				return true;
			}
			
			return false;
		}

		public override bool ShootProjectile(Vector3 movement) {
			flier.StartFlight(movement);
			return true;
		}

		public override void OnEntityHit(Entity hitEntity)
		{
			projectile.Despawn();
		}

		public override void OnTerrainHit()
		{
			despawning = true;
		}

	}
}
