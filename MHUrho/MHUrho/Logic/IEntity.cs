using System.Collections.Generic;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Logic {


	public interface IEntity {
		int ID { get; }

		ILevelManager Level { get; }

		Map Map { get; }

		IPlayer Player { get; }

		Vector3 Position { get; }

		bool RemovedFromLevel { get; }

		void AddComponent(DefaultComponent defaultComponent);

		T CreateComponent<T>() where T : Component, new();

		T GetDefaultComponent<T>() where T : DefaultComponent;

		IEnumerable<T> GetDefaultComponents<T>() where T : DefaultComponent;

		bool HasDefaultComponent<T>() where T : DefaultComponent;

		bool RemoveComponent(DefaultComponent defaultComponent);

		void Accept(IEntityVisitor visitor);
	}
}