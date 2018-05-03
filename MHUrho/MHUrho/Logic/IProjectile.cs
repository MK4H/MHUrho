using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic {


	public interface IProjectile : IEntity {
		bool FaceInTheDirectionOfMovement { get; set; }

		ProjectileInstancePlugin Plugin { get; }

		ProjectileType ProjectileType { get; }

		bool TriggerCollisions { get; set; }

		void Despawn();

		bool Move(Vector3 movement);

		void ReInitialize(int newID, ILevelManager level, IPlayer player, Vector3 position);

		StProjectile Save();
	}
}