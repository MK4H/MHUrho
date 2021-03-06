﻿using System;
using System.Collections.Generic;
using MHUrho.Input;
using MHUrho.Plugins;
using MHUrho.DefaultComponents;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Logic {

	/// <summary>
	/// Common ancestor of all entities in game, namely Units, Buildings and Projectiles
	/// </summary>
	public interface IEntity : IDisposable {
		/// <summary>
		/// Entity ID, used for getting reference to entity from Level by
		/// <see cref="ILevelManager.GetEntity(int)"/>.
		/// </summary>
		int ID { get; }

		/// <summary>
		/// UrhoSharp <see cref="Node"/> the entity is represented by.
		/// </summary>
		Node Node { get; }

		/// <summary>
		/// Level containing the entity.
		/// </summary>
		ILevelManager Level { get; }

		/// <summary>
		/// Type of the entity.
		/// </summary>
		IEntityType Type { get; }

		/// <summary>
		/// Get player that owns this entity
		/// </summary>
		IPlayer Player { get; }

		/// <summary>
		/// Get current position of the center of the entity
		/// </summary>
		Vector3 Position { get; }

		/// <summary>
		/// Entity position projected into XZ plane.
		///	Is a shortcut for the X and Z members of 
		/// <see cref="IEntity.Position"/>.
		/// </summary>
		Vector2 XZPosition { get; }

		/// <summary>
		/// Vector in world coordinates indicating the upwards direction in reference to current entity orientation.
		/// </summary>
		Vector3 Up { get; }

		/// <summary>
		/// Vector in world coordinates indicating the downwards direction in reference to current entity orientation.
		/// </summary>
		Vector3 Down { get; }

		/// <summary>
		/// Vector in world coordinates indicating the forward direction in reference to current entity orientation.
		/// </summary>
		Vector3 Forward { get; }

		/// <summary>
		/// Get vector in world coordinates indicating the backwards direction in reference to current entity orientation.
		/// </summary>
		Vector3 Backward { get; }

		/// <summary>
		/// Vector in world coordinates indicating the left direction in reference to current entity orientation.
		/// </summary>
		Vector3 Left { get; }

		/// <summary>
		/// Vector in world coordinates indicating the right direction in reference to current entity orientation.
		/// </summary>
		Vector3 Right { get; }

		/// <summary>
		/// User plugin of this entity.
		/// </summary>
		InstancePlugin Plugin { get; }

		/// <summary>
		/// If the entity was removed from level and should not be used.
		/// </summary>
		bool IsRemovedFromLevel { get; }

		/// <summary>
		/// Cleanup actions, called on entity removal.
		/// </summary>
		event Action<IEntity> OnRemoval;

		/// <summary>
		/// Triggered on every change in <see cref="Position"/>.
		/// </summary>
		event Action<IEntity> PositionChanged;

		/// <summary>
		/// Triggered on every change in <see cref="Rotation"/>.
		/// </summary>
		event Action<IEntity> RotationChanged;

		/// <summary>
		/// Adds one of the classes derived from <see cref="DefaultComponent"/>. See <see cref="MHUrho.DefaultComponents"/>
		/// </summary>
		/// <param name="defaultComponent">the component to be added, should not be null</param>
		void AddComponent(DefaultComponent defaultComponent);

		/// <summary>
		/// Creates any <see cref="Component"/> that can be created with <see cref="Node.CreateComponent{T}(CreateMode, uint)"/> and its overloads.
		/// Mainly for creating components provided by the engine itself, contained in <see cref="Urho"/> namespace.
		/// </summary>
		/// <typeparam name="T">Component from <see cref="Urho"/> namespace.</typeparam>
		/// <returns>Reference to the newly created component.</returns>
		T CreateComponent<T>() where T : Component, new();

		/// <summary>
		/// Gets the first component of type <typeparamref name="T"/> or derived from it present on this entity.
		/// This method is provided for getting the Urho3D basic components from entity.
		/// For getting <see cref="DefaultComponent"/> and derived, use <see cref="GetDefaultComponent{T}"/>.
		/// O(n) time complexity.
		/// </summary>
		/// <typeparam name="T">Any class derived from <see cref="Component"/>.</typeparam>
		/// <returns>First component of type <typeparamref name="T"/> or derived from it that are present on this entity.</returns>
		T GetComponent<T>() where T : Component;

		/// <summary>
		/// Gets every component of type <typeparamref name="T"/> or derived from it present on this entity.
		/// This method is provided for getting the Urho3D basic components from entity.
		/// For getting <see cref="DefaultComponent"/> and derived, use <see cref="GetDefaultComponents{T}"/>.
		/// O(n) time complexity.
		/// </summary>
		/// <typeparam name="T">Any class derived from <see cref="Component"/>.</typeparam>
		/// <returns>All components of type <typeparamref name="T"/> or derived from it that are present on this entity.</returns>
		IEnumerable<T> GetComponents<T>() where T : Component;

		/// <summary>
		/// Get the first component of type <typeparamref name="T"/> or derived from it present on this entity
		/// O(1) time complexity compared to <see cref="Node.GetComponent{T}(bool)"/> and <see cref="Component.GetComponent{T}"/>
		/// which are O(n) time complexity where n is the number of components.
		/// </summary>
		/// <typeparam name="T">One of the components defined in namespace <see cref="MHUrho.DefaultComponents"/>.</typeparam>
		/// <returns>First component of type <typeparamref name="T"/> or derived from it present on this entity, or null if not present.</returns>
		T GetDefaultComponent<T>() where T : DefaultComponent;

		/// <summary>
		/// Get an enumerable of all components of type <typeparamref name="T"/> or derived from it present on this entity.
		/// O(1) time complexity compared to <see cref="Node.GetComponent{T}(bool)"/> and <see cref="Component.GetComponent{T}"/> which are O(n) in the total number of components.
		/// </summary>
		/// <typeparam name="T">One of the components defined in namespace <see cref="MHUrho.DefaultComponents"/>.</typeparam>
		/// <returns>Enumerable with all components of type <typeparamref name="T"/> or derived from it.</returns>
		IEnumerable<T> GetDefaultComponents<T>() where T : DefaultComponent;

		/// <summary>
		/// Checks if there is a components of type <typeparamref name="T"/> or derived from it present on this entity.
		/// O(1) time complexity.
		/// </summary>
		/// <typeparam name="T">One of the components defined in namespace <see cref="MHUrho.DefaultComponents"/>.</typeparam>
		/// <returns>True if there is a component of type <typeparamref name="T"/> or derived from it, false otherwise.</returns>
		bool HasDefaultComponent<T>() where T : DefaultComponent;

		/// <summary>
		/// Removes the provided instance of <see cref="DefaultComponent"/> from this entity if this instance is present on the entity.
		/// </summary>
		/// <param name="defaultComponent">The component instance to remove.</param>
		/// <returns>True if the <paramref name="defaultComponent"/> was present on this entity and was removed, false if not.</returns>
		bool RemoveComponent(DefaultComponent defaultComponent);

		/// <summary>
		/// Removes the entity from the level.
		/// </summary>
		void RemoveFromLevel();

		/// <summary>
		/// Accept method for visitor pattern.
		/// </summary>
		/// <param name="visitor">Visitor to visit.</param>
		void Accept(IEntityVisitor visitor);

		/// <summary>
		/// Accept method for generic visitor pattern.
		/// </summary>
		/// <typeparam name="T">The type that will be returned by visitor.</typeparam>
		/// <param name="visitor">Visitor to visit.</param>
		/// <returns></returns>
		T Accept<T>(IEntityVisitor<T> visitor);

		/// <summary>
		/// Signals to this entity that it was hit by the <paramref name="other"/> entity.
		///
		/// If <paramref name="other"/> is a Projectile, it was a range hit, if it is other, it was meele hit.
		/// </summary>
		/// <param name="other">The entity that hit this entity.</param>
		/// <param name="userData">User defined data to be sent to the the plugin.</param>
		void HitBy(IEntity other, object userData = null);
	}
}