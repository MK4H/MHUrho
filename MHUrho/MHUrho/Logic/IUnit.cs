using MHUrho.EntityInfo;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic {

	public interface IUnit : IEntity {
		/// <summary>
		/// Get or Set value indicating if units Up vector will always be the same as World up vector (Vector3.UnitY) if true
		///		or if unit can be freely rotated
		/// </summary>
		bool AlwaysVertical { get; set; }

		/// <summary>
		/// Get vector in world coordinates indicating the backwards direction in reference to current unit orientation
		/// </summary>
		Vector3 Backward { get; }

		Node CenterNode { get; }

		/// <summary>
		/// Get vector in world coordinates indicating the downwards direction in reference to current unit orientation
		/// </summary>
		Vector3 Down { get; }

		Vector3 Forward { get; }

		Vector3 Left { get; }

		Node LegNode { get; }

		UnitInstancePlugin UnitPlugin { get; }

		Vector3 Right { get; }

		/// <summary>
		/// Get tile currently containinng the units Node, value is equal to IMap.GetContainingTile(unit.Position)
		/// <see cref="WorldMap.IMap.GetContainingTile(Vector3)"/>
		/// </summary>
		ITile Tile { get; }

		/// <summary>
		/// Get unitType instance representing current type of the unit
		/// <see cref="Logic.UnitType"/>
		/// </summary>
		UnitType UnitType { get; }

		Vector3 Up { get; }

		//TODO: Temp
		HealthBar HealthBar{ get;}

		bool CanGoFromTo(ITile fromTile, ITile toTile);

		void ChangeType(UnitType newType);

		void FaceTowards(Vector3 lookPosition, bool rotateAroundY = false);

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