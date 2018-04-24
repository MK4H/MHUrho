using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using Urho;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;

namespace DefaultPackage
{
	public class EggProjectileType : ProjectileTypePluginBase {
		public float Speed { get; private set; }

		public override bool IsMyType(string typeName) {
			return typeName == "EggProjectile";
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {
			var speedElement = XmlHelpers.GetChild(extensionElement, "speed");
			Speed = XmlHelpers.GetFloat(speedElement);
		}

		public override ProjectileInstancePluginBase CreateNewInstance(ILevelManager level, Projectile projectile) {
			return new EggProjectileInstance(level, projectile, this);
		}

		public override ProjectileInstancePluginBase GetInstanceForLoading() {
			throw new NotImplementedException();
		}

		public override bool IsInRange(Vector3 source, IRangeTarget target) {
			return UnpoweredFlier.GetUnpoweredProjectileTimesAndAngles(target.CurrentPosition,
																		source, 
																		Speed,
																		out var loweTime,
																		out var lowVector,
																		out var highTime,
																		out var highVector);
		}
	}

	public class EggProjectileInstance : ProjectileInstancePluginBase, UnpoweredFlier.INotificationReceiver {

		private UnpoweredFlier flier;
		private EggProjectileType myType;

		public EggProjectileInstance(ILevelManager level, Projectile projectile, EggProjectileType myType)
			:base(level, projectile)
		{
			this.myType = myType;
			this.flier = UnpoweredFlier.GetInstanceFor(this, level);
			projectile.AddComponent(flier);
		}

		public override void SaveState(PluginDataWrapper pluginData) {
			throw new NotImplementedException();
		}

		public override void LoadState(ILevelManager level, Projectile projectile, PluginDataWrapper pluginData) {
			throw new NotImplementedException();
		}

		public override void ReInitialize(ILevelManager level) {
			
		}

		public override bool ShootProjectile(IRangeTarget target) {
			if (UnpoweredFlier.GetUnpoweredProjectileTimesAndAngles(target.CurrentPosition,
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
			return false;
		}

		public void OnMovementStarted(UnpoweredFlier flier) {
			
		}

		public void OnGroundHit(UnpoweredFlier flier) {
			projectile.Despawn();
		}
	}

}
