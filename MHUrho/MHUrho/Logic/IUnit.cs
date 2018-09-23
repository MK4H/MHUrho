using System;
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



		Node CenterNode { get; }


		Node LegNode { get; }

		UnitInstancePlugin UnitPlugin { get; }

		

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

		void FaceTowards(Vector3 lookPosition, bool rotateAroundY = false);

		void MoveBy(Vector2 moveBy);

		void MoveBy(Vector3 moveBy);

		void MoveTo(Vector2 newLocation);

		void MoveTo(Vector3 newPosition);

		void RotateAroundCenter(float pitch, float yaw, float roll);

		void RotateAroundFeet(float pitch, float yaw, float roll);

		StUnit Save();

		void SetHeight(float newHeight);
	}
}