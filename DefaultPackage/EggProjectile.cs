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
using MHUrho.WorldMap;

namespace DefaultPackage
{
	public class EggProjectileType : ProjectileTypePlugin {
		public float Speed { get; private set; }

		public override bool IsMyType(string typeName) {
			return typeName == "EggProjectile";
		}

		public override void Initialize(XElement extensionElement, PackageManager packageManager) {
			var speedElement = XmlHelpers.GetChild(extensionElement, "speed");
			Speed = XmlHelpers.GetFloat(speedElement);
		}

		public override ProjectileInstancePlugin CreateNewInstance(ILevelManager level, IProjectile projectile) {
			return new EggProjectileInstance(level, projectile, this);
		}

		public override ProjectileInstancePlugin GetInstanceForLoading()
		{
			return new EggProjectileInstance(this);
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
	}

	public class EggProjectileInstance : ProjectileInstancePlugin, BallisticProjectile.INotificationReceiver {

		BallisticProjectile flier;
		readonly EggProjectileType myType;

		public EggProjectileInstance(EggProjectileType myType)
		{
			this.myType = myType;
		}

		public EggProjectileInstance(ILevelManager level, IProjectile projectile, EggProjectileType myType)
			:base(level, projectile)
		{
			this.myType = myType;
			this.flier = BallisticProjectile.GetInstanceFor(this, level);
			projectile.AddComponent(flier);
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			
		}

		public override void LoadState(ILevelManager level, IProjectile projectile, PluginDataWrapper pluginData) {
			this.Level = level;
			this.projectile = projectile;
			this.flier = projectile.GetDefaultComponent<BallisticProjectile>();
		}

		public override void ReInitialize(ILevelManager level) {
			
		}

		public override bool ShootProjectile(IRangeTarget target) {
			if (target.Moving) {
				return ShootMovingTarget(target);
			}

			return ShootStaticTarget(target);
		}

		public override bool ShootProjectile(Vector3 movement) {
			return false;
		}

		public override void OnEntityHit(IEntity hitEntity)
		{
			if (hitEntity.Player != projectile.Player) {
				projectile.Despawn();
			}	
		}

		public override void OnTerrainHit()
		{
			projectile.Despawn();
		}

		bool ShootMovingTarget(IRangeTarget target)
		{
			int numSolutions = BallisticProjectile.GetVectorsForMovingTarget(target,
																			projectile.Position,
																			myType.Speed,
																			out Vector3 lowVector,
																			out Vector3 highVector);

			if (numSolutions >= 1) {
				flier.StartFlight(lowVector);
				return true;
			}

			return false;
		}

		bool ShootStaticTarget(IRangeTarget target) {
			if (BallisticProjectile.GetTimesAndVectorsForStaticTarget(
									target.CurrentPosition,
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
	}

}
