using MHUrho.Control;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic {


	public interface IBuilding : IEntity {
		BuildingType BuildingType { get; }

		BuildingInstancePlugin BuildingPlugin { get; }

		IntRect Rectangle { get; }

		IntVector2 TopLeft { get; }

		IntVector2 TopRight { get; }

		IntVector2 BottomLeft { get; }

		IntVector2 BottomRight { get; }

		Vector3 Center { get; }

		IntVector2 Size { get; }

		StBuilding Save();

		float? GetHeightAt(float x, float y);

		IFormationController GetFormationController(Vector3 centerPosition);
	}
}