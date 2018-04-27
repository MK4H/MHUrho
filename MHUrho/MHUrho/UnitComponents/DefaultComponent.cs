using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;
using Urho;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
	public enum DefaultComponents {
		Shooter,
		Meele,
		ResourceCarrier,
		UnitSelector,
		WallClimber,
		WorkQueue,
		WorldWalker,
		UnpoweredFlier,
		StaticRangeTarget,
		MovingRangeTarget,
		Clicker
	}

	//TODO: Rename this
	internal interface IByTypeQueryable {
		void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents);

		bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents);
	}

	public abstract class DefaultComponent : Component, IByTypeQueryable {

		public abstract DefaultComponents ComponentTypeID{ get; }

		public abstract string ComponentTypeName { get; }

		public abstract PluginData SaveState();

		void IByTypeQueryable.AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			AddedToEntity(entityDefaultComponents);
		}

		bool IByTypeQueryable.RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			return RemovedFromEntity(entityDefaultComponents);
		}

		protected abstract void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents);

		protected abstract bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents);

		protected void AddedToEntity(Type type, IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			if (entityDefaultComponents.TryGetValue(type, out var componentsOfType)) {
				componentsOfType.Add(this);
			}
			else {
				entityDefaultComponents.Add(type, new List<DefaultComponent> { this });
			}
		}

		protected bool RemovedFromEntity(Type type, IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			if (!entityDefaultComponents.TryGetValue(type, out var componentsOfType)) return false;

			bool removed = componentsOfType.Remove(this);
			if (componentsOfType.Count == 0) {
				entityDefaultComponents.Remove(type);
			}
			return removed;
		}
	}
}
