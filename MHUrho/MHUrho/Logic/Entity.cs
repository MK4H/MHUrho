using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Input;
using MHUrho.Plugins;
using MHUrho.DefaultComponents;
using Urho;
using MHUrho.WorldMap;

namespace MHUrho.Logic
{
	/// <summary>
	/// Base class for entities in the game
	/// </summary>
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

		/// <inheritdoc/>
		public ILevelManager Level { get; protected set; }

		/// <inheritdoc/>
		public abstract Vector3 Position { get; protected set; }

		/// <inheritdoc/>
		public Vector2 XZPosition {
			get => Position.XZ2();
			set => Position = new Vector3(value.X, Position.Y, value.Y);
		}

		/// <summary>
		/// Type of this entity, loaded from package.
		/// </summary>
		public new abstract IEntityType Type { get; }
		
		/// <inheritdoc/>
		public abstract Vector3 Forward { get; }

		/// <inheritdoc/>
		public abstract Vector3 Backward { get; }

		/// <inheritdoc/>
		public abstract Vector3 Right { get; }

		/// <inheritdoc/>
		public abstract Vector3 Left { get; }

		/// <inheritdoc/>
		public abstract Vector3 Up { get; }

		/// <inheritdoc/>
		public abstract Vector3 Down { get; }

		/// <inheritdoc/>
		public abstract InstancePlugin Plugin { get; }

		/// <inheritdoc/>
		public bool IsRemovedFromLevel { get; protected set; }

		/// <inheritdoc/>
		public event Action<IEntity> PositionChanged;

		/// <inheritdoc/>
		public event Action<IEntity> RotationChanged;

		/// <inheritdoc/>
		public event Action<IEntity> OnRemoval;

		/// <summary>
		/// Default components present on this entity, split up by their types for faster search. 
		/// </summary>
		protected Dictionary<Type, IList<DefaultComponent>> defaultComponents;

		/// <summary>
		/// Creates new entity in the game.
		/// </summary>
		/// <param name="ID">Identifier of the entity.</param>
		/// <param name="level">Level in which the entity is created.</param>
		protected Entity(int ID, ILevelManager level)
		{
			this.ReceiveSceneUpdates = true;
			this.ID = ID;
			this.Level = level;
			this.defaultComponents = new Dictionary<Type, IList<DefaultComponent>>();
		}

		/// <inheritdoc/>
		public abstract void Accept(IEntityVisitor visitor);

		/// <inheritdoc/>
		public abstract T Accept<T>(IEntityVisitor<T> visitor);

		/// <inheritdoc/>
		public T CreateComponent<T>()
			where T : Component, new()
		{
			return Node.CreateComponent<T>();
		}

		/// <inheritdoc/>
		T IEntity.GetComponent<T>()
		{
			return GetComponent<T>();
		}

		/// <inheritdoc/>
		public IEnumerable<T> GetComponents<T>()
			where T : Component
		{
			return Node.Components.OfType<T>();
		}

		/// <inheritdoc/>
		public T GetDefaultComponent<T>()
			where T: DefaultComponent
		{
			return defaultComponents.TryGetValue(typeof(T), out IList<DefaultComponent> presentComponents) ? (T)presentComponents[0] : null;
		}

		/// <inheritdoc/>
		public bool HasDefaultComponent<T>()
			where T : DefaultComponent 
		{
			return defaultComponents.ContainsKey(typeof(T));
		}

		/// <inheritdoc/>
		public IEnumerable<T> GetDefaultComponents<T>()
			where T : DefaultComponent 
		{
			return defaultComponents.TryGetValue(typeof(T), out IList<DefaultComponent> presentComponents) ? (IEnumerable<T>)presentComponents : null;
		}

		/// <inheritdoc/>
		public void AddComponent(DefaultComponent defaultComponent) {
			((IByTypeQueryable) defaultComponent).AddedToEntity(this, defaultComponents);
		}

		/// <inheritdoc/>
		public bool RemoveComponent(DefaultComponent defaultComponent) {
			return ((IByTypeQueryable) defaultComponent).RemovedFromEntity(defaultComponents);
		}

		/// <inheritdoc/>
		public virtual void RemoveFromLevel()
		{
			IsRemovedFromLevel = true;
			try {
				OnRemoval?.Invoke(this);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(OnRemoval)}: {e.Message}");
			}
			OnRemoval = null;
		}

		/// <inheritdoc/>
		public abstract void HitBy(IEntity other, object additionalData);

		/// <summary>
		/// Invokes the <see cref="PositionChanged"/> event.
		/// </summary>
		protected void SignalPositionChanged()
		{
			try {
				PositionChanged?.Invoke(this);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(PositionChanged)}: {e.Message}");
			}
			
		}

		/// <summary>
		/// Invokes the <see cref="RotationChanged"/> event.
		/// </summary>
		protected void SignalRotationChanged()
		{
			try {
				RotationChanged?.Invoke(this);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(RotationChanged)}: {e.Message}");
			}
		}
	}
}
