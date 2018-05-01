using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;
using Urho;
using MHUrho.Storage;
using MHUrho.WorldMap;

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
		void AddedToEntity(Entity entity, IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents);

		bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents);
	}

	public abstract class DefaultComponent : Component, IByTypeQueryable {

		public abstract DefaultComponents ComponentTypeID{ get; }

		public abstract string ComponentTypeName { get; }

		public abstract PluginData SaveState();

		public Entity Entity { get; private set; }

		public IPlayer Player => Entity.Player;

		public ILevelManager Level { get; private set; }


		public Map Map => Level.Map;



		protected DefaultComponent(ILevelManager level)
		{
			this.Level = level;
		}

		void IByTypeQueryable.AddedToEntity(Entity entity, IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			Entity = entity;
			AddedToEntity(entityDefaultComponents);
		}

		bool IByTypeQueryable.RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			Entity = null;
			return RemovedFromEntity(entityDefaultComponents);
		}

		protected override void OnUpdate(float timeStep)
		{
			base.OnUpdate(timeStep);
			if (IsDeleted || !EnabledEffective) return;

			OnUpdateChecked(timeStep);
		}

		protected virtual void OnUpdateChecked(float timeStep)
		{
			//NOTHING
		}

		protected virtual void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents)
		{
			AddedToEntity(typeof(DefaultComponent), entityDefaultComponents);
		}

		protected virtual bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents)
		{
			return RemovedFromEntity(typeof(DefaultComponent), entityDefaultComponents);
		}


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
