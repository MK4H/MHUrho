using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.DefaultComponents;
using Urho;

namespace MHUrho.Logic {


	public interface IProjectile : IEntity {
		bool FaceInTheDirectionOfMovement { get; set; }

		ProjectileInstancePlugin ProjectilePlugin { get; }

		ProjectileType ProjectileType { get; }

		bool TriggerCollisions { get; set; }

		bool Shoot(IRangeTarget target);

		bool Shoot(Vector3 movement);

		bool Move(Vector3 movement);

		void ReInitialize(int newID, ILevelManager level, IPlayer player, Vector3 position);

		StProjectile Save();
	}
}