using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic {

	public interface IUnit : IEntity {
		bool AlwaysVertical { get; set; }

		Vector3 Backward { get; }

		Node CenterNode { get; }

		Vector3 Down { get; }

		Vector3 Forward { get; }

		Vector3 Left { get; }

		Node LegNode { get; }

		UnitInstancePlugin Plugin { get; }

		Vector3 Right { get; }

		ITile Tile { get; }

		UnitType UnitType { get; }

		Vector3 Up { get; }

		Vector2 XZPosition { get; }

		bool CanGoFromTo(ITile fromTile, ITile toTile);

		void ChangeType(UnitType newType);

		void FaceTowards(Vector3 lookPosition, bool rotateAroundY = false);

		void Kill();

		bool MoveBy(Vector2 moveBy);

		bool MoveBy(Vector3 moveBy);

		float MovementSpeed(ITile tile);

		bool MoveTo(Vector2 newLocation);

		bool MoveTo(Vector3 newPosition);

		void RotateAroundCenter(float pitch, float yaw, float roll);

		void RotateAroundFeet(float pitch, float yaw, float roll);

		StUnit Save();

		void SetHeight(float newHeight);
	}
}