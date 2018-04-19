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

namespace DefaultPackage
{
	public class TestProjectileType : ProjectileTypePluginBase {
		public override bool IsMyType(string typeName) {
			return typeName == "TestProjectile";
		}

		public override ProjectileInstancePluginBase CreateNewInstance(ILevelManager level, Projectile projectile) {
			return new TestProjectileInstance(level, projectile);
		}

		public override ProjectileInstancePluginBase GetInstanceForLoading() {
			throw new NotImplementedException();
		}

		public override bool IsInRange(Vector3 source, RangeTargetComponent target) {
			return false;
		}

		public override bool IsInRange(Vector3 source, Vector3 target) {
			return UnpoweredFlier.GetUnpoweredProjectileTimesAndAngles(target,
																	   source,
																	   30,
																	   out var lowTime,
																	   out var lowVector,
																	   out var highTime,
																	   out var highVector);
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {
			
		}

	}

	public class TestProjectileInstance : ProjectileInstancePluginBase, UnpoweredFlier.INotificationReciever 
	{
		private UnpoweredFlier flier;

		private const float baseTimeToSplit = 0.5f;
		private float timeToSplit = baseTimeToSplit;

		private Random rng;

		private int splits = 10;

		public TestProjectileInstance(ILevelManager level, Projectile projectile)
			:base (level, projectile)
		{
			this.rng = new Random();
			flier = UnpoweredFlier.GetInstanceFor(this, level);
			projectile.Node.AddComponent(flier);
		}

		public override void OnUpdate(float timeStep) {

			timeToSplit -= timeStep;
			if (timeToSplit > 0) return;

			timeToSplit = baseTimeToSplit;

			for (int i = 0; i < splits; i++) {
				var movement = flier.Movement;

				movement = new Quaternion((float)rng.NextDouble() * 5, (float)rng.NextDouble() * 5, (float)rng.NextDouble() * 5) * movement;

				var newProjectile = Level.SpawnProjectile(projectile.ProjectileType, projectile.Node.Position, projectile.Player,  movement);
				((TestProjectileInstance) newProjectile.Plugin).splits = 0;
				
			}

			if (splits != 0) {
				splits = 0;
				projectile.Despawn();
			}
			
		}

		public override void SaveState(PluginDataWrapper pluginData) {
			throw new NotImplementedException();
		}

		public override void LoadState(ILevelManager level, Projectile projectile, PluginDataWrapper pluginData) {
			throw new NotImplementedException();
		}

		public override void ReInitialize(ILevelManager level) {
			timeToSplit = baseTimeToSplit;
			splits = 10;
		}

		public void OnMovementStarted(UnpoweredFlier flier) {
			
		}

		public void OnGroundHit(UnpoweredFlier flier) {
			
		}
	}
}
