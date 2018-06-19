using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		UnitSelector,
		WorkQueue,
		WorldWalker,
		UnpoweredFlier,
		StaticRangeTarget,
		MovingRangeTarget,
		Clicker,
		MovingMeele,
		StaticMeele

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

			/*
			 * If called from OnAttachedToNode, entity will already be set
			 * So i need to just add it to the correct spots in the dictionary
			 *
			 * Else it was called from Entity.AddComponent and i need to add the component to
			 * Engines node hierarchy here
			 * that will cause the OnAttachedToNode override to be called, in which i set the Entity and
			 * call Entity.AddComponent second time, now with the Entity set, which will go through the if here
			 *	
			 */
			if (Entity != null) {
				Debug.Assert(Entity == entity);
				AddedToEntity(entityDefaultComponents);
			}
			else {
				entity.Node.AddComponent(this);
			}

		}

		bool IByTypeQueryable.RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			Entity = null;
			return RemovedFromEntity(entityDefaultComponents);
		}

		public override void OnAttachedToNode(Node node) {
			base.OnAttachedToNode(node);

			/*
			 * Called from Node.AddComponent, need to call Entity.AddComponent
			 * Set Entity to distinguish the different paths, so on this.AddedToEntity call from Entity
			 *  i dont add the component to the node second time
			 */
			if (Entity == null) {
				Entity = Node.GetComponent<Entity>();
				Entity.AddComponent(this);
			}
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
