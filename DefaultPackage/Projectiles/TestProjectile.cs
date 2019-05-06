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
	public class TestProjectileType : ProjectileTypePlugin {

		public override int ID => 1;

		public override string Name => "TestProjectile";

		public float Speed { get; private set; }

		public override ProjectileInstancePlugin CreateNewInstance(ILevelManager level, IProjectile projectile)
		{
			return TestProjectileInstance.CreateNew(level, projectile, this);
		}

		public override ProjectileInstancePlugin GetInstanceForLoading(ILevelManager level, IProjectile projectile)
		{
			return TestProjectileInstance.GetInstanceForLoading(level, projectile, this);
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

		public override void Initialize(XElement extensionElement, GamePack package) {
			var speedElement = XmlHelpers.GetChild(extensionElement, "speed");
			Speed = XmlHelpers.GetFloat(speedElement);
		}

	}

	public class TestProjectileInstance : ProjectileInstancePlugin {
		static readonly Random seedRng = new Random();

		BallisticProjectile flier;
		TestProjectileType myType;

		const float baseTimeToSplit = 0.5f;
		float timeToSplit = baseTimeToSplit;

		Random rng;

		int splits = 10;

		bool despawning;
		float timeToDespawn = 6;

		public static TestProjectileInstance CreateNew(ILevelManager level, IProjectile projectile, TestProjectileType type)
		{
			var instance = new TestProjectileInstance(level, projectile, type);
			instance.flier = BallisticProjectile.CreateNew(instance, level);

			return instance;
		}

		public static TestProjectileInstance GetInstanceForLoading(ILevelManager level, IProjectile projectile, TestProjectileType type)
		{
			return new TestProjectileInstance(level, projectile, type);
		}

		protected TestProjectileInstance(ILevelManager level, IProjectile projectile, TestProjectileType type)
			:base(level, projectile)
		{
			this.myType = type;
			this.rng = new Random(seedRng.Next());
		}

		public override void OnUpdate(float timeStep) {

			if (despawning) {
				timeToDespawn -= timeStep;

				if (timeToDespawn < 0) {
					Projectile.RemoveFromLevel();
					return;
				}
			}

			timeToSplit -= timeStep;
			if (timeToSplit > 0) return;

			timeToSplit = baseTimeToSplit;

			for (int i = 0; i < splits; i++) {
				var movement = flier.Movement;

				movement = new Quaternion((float)rng.NextDouble() * 5, (float)rng.NextDouble() * 5, (float)rng.NextDouble() * 5) * movement;

				var newProjectile = Level.SpawnProjectile(Projectile.ProjectileType, Projectile.Position, Quaternion.Identity, Projectile.Player,  movement);
				((TestProjectileInstance) newProjectile.ProjectilePlugin).splits = 0;
				
			}

			if (splits != 0) {
				splits = 0;
				Projectile.RemoveFromLevel();
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

		public override void LoadState(PluginDataWrapper pluginData) {
			this.flier = Projectile.GetDefaultComponent<BallisticProjectile>();

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
			Projectile.TriggerCollisions = true;
		}

		public override bool ShootProjectile(IRangeTarget target) {
			
			if (BallisticProjectile.GetTimesAndVectorsForStaticTarget(target.CurrentPosition,
																	Projectile.Position,
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

		public override void OnEntityHit(IEntity hitEntity)
		{
			Projectile.RemoveFromLevel();
		}

		public override void OnTerrainHit()
		{
			despawning = true;
			Projectile.TriggerCollisions = false;
		}

		public override void Dispose()
		{

		}
	}
}
