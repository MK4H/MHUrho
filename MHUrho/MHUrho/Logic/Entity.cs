using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.UnitComponents;
using Urho;
using MHUrho.WorldMap;

namespace MHUrho.Logic
{
	public abstract class Entity : Component
	{

		/// <summary>
		/// ID of this entity
		/// Hides component member ID, but having two IDs would be more confusing
		/// 
		/// If you need component ID, just cast this to component and access ID
		/// </summary>
		public new int ID { get; protected set; }

		public IPlayer Player { get; protected set; }

		public ILevelManager Level { get; protected set; }

		public Map Map => Level.Map;

		public abstract Vector3 Position { get; protected set; }

		protected Dictionary<Type, IList<DefaultComponent>> defaultComponents;

		protected Entity(int ID, ILevelManager level) {
			this.ID = ID;
			this.Level = level;
			this.defaultComponents = new Dictionary<Type, IList<DefaultComponent>>();
		}

		public T GetDefaultComponent<T>()
			where T: DefaultComponent
		{
			return defaultComponents.TryGetValue(typeof(T), out IList<DefaultComponent> presentComponents) ? (T)presentComponents[0] : null;
		}

		public bool HasDefaultComponent<T>()
			where T : DefaultComponent 
		{
			return defaultComponents.ContainsKey(typeof(T));
		}

		public IEnumerable<T> GetDefaultComponents<T>()
			where T : DefaultComponent 
		{
			return defaultComponents.TryGetValue(typeof(T), out IList<DefaultComponent> presentComponents) ? (IEnumerable<T>)presentComponents : null;
		}

		public void AddComponent(DefaultComponent defaultComponent) {
			Node.AddComponent(defaultComponent);
			((IByTypeQueryable) defaultComponent).AddedToEntity(defaultComponents);
		}

		public bool RemoveComponent(DefaultComponent defaultComponent) {
			Node.RemoveComponent(defaultComponent);
			return ((IByTypeQueryable) defaultComponent).RemovedFromEntity(defaultComponents);
		}
	}
}
