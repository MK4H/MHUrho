using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Input;
using MHUrho.Plugins;
using MHUrho.UnitComponents;
using Urho;
using MHUrho.WorldMap;

namespace MHUrho.Logic
{
	public abstract class Entity : Component, IEntity {

		/// <inheritdoc />
		/// <summary>
		/// ID of this entity
		/// Hides component member ID, but having two IDs would be more confusing.
		/// If you need component ID, just cast this to component and access ID
		/// </summary>
		public new int ID { get; protected set; }

		/// <inheritdoc />
		/// <summary>
		/// Player owning this entity
		/// </summary>
		public IPlayer Player { get; protected set; }

		public ILevelManager Level { get; protected set; }

		public Map Map => Level.Map;

		public abstract Vector3 Position { get; protected set; }

		public Vector2 XZPosition {
			get => Position.XZ2();
			set => Position = new Vector3(value.X, Position.Y, value.Y);
		}

		public abstract Vector3 Forward { get; }

		public abstract Vector3 Backward { get; }

		public abstract Vector3 Right { get; }

		public abstract Vector3 Left { get; }

		public abstract Vector3 Up { get; }

		public abstract Vector3 Down { get; }

		public abstract InstancePlugin Plugin { get; }
	

		public bool RemovedFromLevel { get; protected set; }

		public event Action<IEntity> PositionChanged;

		public event Action<IEntity> RotationChanged;

		public event Action OnRemoval;

		protected Dictionary<Type, IList<DefaultComponent>> defaultComponents;

		protected Entity(int ID, ILevelManager level) {
			this.ID = ID;
			this.Level = level;
			this.defaultComponents = new Dictionary<Type, IList<DefaultComponent>>();
		}

		public abstract void Accept(IEntityVisitor visitor);

		public abstract T Accept<T>(IEntityVisitor<T> visitor);

		public T CreateComponent<T>()
			where T : Component, new()
		{
			return Node.CreateComponent<T>();
		}


		T IEntity.GetComponent<T>()
		{
			return GetComponent<T>();
		}

		public IEnumerable<T> GetComponents<T>()
			where T : Component
		{
			return Node.Components.OfType<T>();
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
			((IByTypeQueryable) defaultComponent).AddedToEntity(this, defaultComponents);
		}

		public bool RemoveComponent(DefaultComponent defaultComponent) {
			return ((IByTypeQueryable) defaultComponent).RemovedFromEntity(defaultComponents);
		}

		public virtual void RemoveFromLevel()
		{
			RemovedFromLevel = true;
			OnRemoval?.Invoke();
		}

		public abstract void HitBy(IEntity other, object additionalData);

		protected void SignalPositionChanged()
		{
			PositionChanged?.Invoke(this);
		}

		protected void SignalRotationChanged()
		{
			RotationChanged?.Invoke(this);
		}
	}
}
