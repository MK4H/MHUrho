using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.DefaultComponents;
using Urho;

namespace MHUrho.Logic {

	/// <summary>
	/// Projectile in the current level.
	/// </summary>
	public interface IProjectile : IEntity {
		/// <summary>
		/// If the projectile should orient itself on every movement along the movement direction.
		/// </summary>
		bool FaceInTheDirectionOfMovement { get; set; }

		/// <summary>
		/// The instance plugin of this projectile.
		/// </summary>
		ProjectileInstancePlugin ProjectilePlugin { get; }

		/// <summary>
		/// The type of the projectile.
		/// </summary>
		ProjectileType ProjectileType { get; }

		/// <summary>
		/// If the projectile should trigger collisions.
		/// </summary>
		bool TriggerCollisions { get; set; }

		/// <summary>
		/// Shoot the projectile at the <paramref name="target"/>.
		/// </summary>
		/// <param name="target">Target to shoot the projectile at.</param>
		/// <returns>True if the projectile was shot at the target, false if the projectile cannot hit the target (out of range etc.).</returns>
		bool Shoot(IRangeTarget target);

		/// <summary>
		/// Shoot the target with the given initial <paramref name="movement"/> vector..
		/// </summary>
		/// <param name="movement">Initial movement vector of the projectile.</param>
		/// <returns>If the projectile can be shot with this movement vector, false if it cannot be shot.</returns>
		bool Shoot(Vector3 movement);

		/// <summary>
		/// Move the projectile by the value of <paramref name="movement"/>.
		/// </summary>
		/// <param name="movement">The change of position of the projectile.</param>
		/// <returns>True oif the projectile could be moved and did not hit anything, false if it hit something and cannot be moved further.</returns>
		bool Move(Vector3 movement);

		/// <summary>
		/// Resets the projectile into it's default state so that it can be shot again.
		/// Implements pooling of the projectiles to lower the number of allocations and deallocations.
		/// </summary>
		/// <param name="newID">New ID of the projectile.</param>
		/// <param name="level">Level in which the projectile is being reinitialized.</param>
		/// <param name="player">The owner of the projectile.</param>
		/// <param name="position">Position at which the projectile will be respawned.</param>
		void ReInitialize(int newID, ILevelManager level, IPlayer player, Vector3 position);

		/// <summary>
		/// Stores the projectile in an instance of <see cref="StProjectile"/> ready to be serialized.
		/// </summary>
		/// <returns>Stored projectile in an instance of <see cref="StProjectile"/>.</returns>
		StProjectile Save();
	}
}