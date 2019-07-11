using System.Collections.Generic;
using MHUrho.Control;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic {


	public interface IBuilding : IEntity {

		/// <summary>
		/// Type of the building.
		/// </summary>
		BuildingType BuildingType { get; }

		/// <summary>
		/// The instance plugin of the building.
		/// </summary>
		BuildingInstancePlugin BuildingPlugin { get; }

		/// <summary>
		/// The area taken up by the building.
		/// </summary>
		IntRect Rectangle { get; }

		/// <summary>
		/// Top left (min x, min z) corner of the area taken up by the building.
		/// </summary>
		IntVector2 TopLeft { get; }

		/// <summary>
		/// Top right (max x, min z) corner of the area taken up by the building.
		/// </summary>
		IntVector2 TopRight { get; }

		/// <summary>
		/// Bottom left (min x, max z) corner of the area taken up by the building.
		/// </summary>
		IntVector2 BottomLeft { get; }

		/// <summary>
		/// Bottom right (max x, max z) corner of the area taken up by the building.
		/// </summary>
		IntVector2 BottomRight { get; }

		/// <summary>
		/// Center of the area taken up by the building.
		/// </summary>
		Vector3 Center { get; }

		/// <summary>
		/// Size of the area taken up by the building.
		/// </summary>
		IntVector2 Size { get; }

		/// <summary>
		/// List of tiles taken up by the building.
		/// </summary>
		IReadOnlyList<ITile> Tiles { get; }

		/// <summary>
		/// Serializes the current state of the building into an instance of <see cref="StBuilding"/>.
		/// </summary>
		/// <returns>Serialized current state of the building.</returns>
		StBuilding Save();

		/// <summary>
		/// Returns height at the [<paramref name="x"/>, <paramref name="y"/>] position or null if it is the same as terrain height.
		/// </summary>
		/// <param name="x">The x coord of the point.</param>
		/// <param name="y">The z coord of the point.</param>
		/// <returns>The height at the [<paramref name="x"/>, <paramref name="y"/>] point or null if it is the same as terrain height.</returns>
		float? GetHeightAt(float x, float y);

		/// <summary>
		/// Checks if it is possible to change the height of the tile with a corner at the [<paramref name="x"/>, <paramref name="y"/>] coords.
		/// </summary>
		/// <param name="x">The x coord of the corner to be changed.</param>
		/// <param name="y">The z coord of the corner to be changed.</param>
		/// <returns>True if it is possible to change the height, false otherwise.</returns>
		bool CanChangeTileHeight(int x, int y);

		/// <summary>
		/// Notifies the building that the height of the tile it occupies has changed.
		/// </summary>
		/// <param name="tile">The tile with the changed height.</param>
		void TileHeightChanged(ITile tile);

		/// <summary>
		/// Changes the height of the building.
		/// </summary>
		/// <param name="newHeight">The new height of the building.</param>
		void ChangeHeight(float newHeight);

		/// <summary>
		/// Gets the formation controller to direct the positioning of the units around the <paramref name="centerPosition"/>.
		/// </summary>
		/// <param name="centerPosition">The central position around which the units should be ordered.</param>
		/// <returns>The formation controller to direct the positioning of the units.</returns>
		IFormationController GetFormationController(Vector3 centerPosition);
	}
}