using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Logic {


	public interface IBuilding : IEntity {
		BuildingType BuildingType { get; }

		Vector3 Center { get; }

		IntVector2 Location { get; }

		BuildingInstancePlugin Plugin { get; }

		IntRect Rectangle { get; }

		IntVector2 Size { get; }

		void Kill();

		StBuilding Save();
	}
}