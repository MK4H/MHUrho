using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.UnitComponents
{

	public delegate void TargetInRangeDelegate(MeeleAttacker attacker, IEntity target);

	public delegate void AttackedDelegate(MeeleAttacker attacker, IEntity target);

	public delegate void TargetFoundDelegate(MeeleAttacker attacker, IEntity target);

	public delegate void TargetLost(MeeleAttacker attacker);

	/// <summary>
	/// Component that attacks a target in selectable intervals.
	/// Can automatically search for target unit and automatically attack it.
	/// Can be manually ordered to attack unit or building.
	/// </summary>
	public abstract class MeeleAttacker : DefaultComponent
	{
		/// <summary>
		/// Base interface to be expanded in classes derived from MeeleAttacker
		/// </summary>
		public interface IBaseUser {
			/// <summary>
			/// Checks if the unit is in meele range of the attacker and should be attacked.
			/// </summary>
			/// <param name="attacker">The current attacker.</param>
			/// <param name="target">Attacked target.</param>
			/// <returns>True if the unit is in range, false otherwise.</returns>
			bool IsInRange(MeeleAttacker attacker, IEntity target);

			/// <summary>
			/// Pick the target unit from all automatically found units in <paramref name="possibleTargets"/>.
			/// </summary>
			/// <param name="possibleTargets">Automatically found units that are in search distance</param>
			/// <returns>The chosen unit to attack or null if none should be attacked.</returns>
			IUnit PickTarget(ICollection<IUnit> possibleTargets);
		}

		/// <summary>
		/// If meeleAttacker should automatically search for a target
		/// Automatically will only attack units, not buildings.
		/// </summary>
		public bool SearchForTarget { get; set; }

		/// <summary>
		/// Timeout between searching for target
		/// </summary>
		public float TimeBetweenSearches { get; set; }

		public float TimeBetweenAttacks { get; set; }

		public float AttacksPerSecond {
			get => 1.0f / TimeBetweenAttacks;
			set => TimeBetweenAttacks = 1.0f / value;
		}

		/// <summary>
		/// Determines the size of the searched rectangle when searching for target.
		/// </summary>
		public IntVector2 SearchRectangleSize {
			get => searchRectangleSize;
			set {
				if (value.X > 0 && value.Y > 0) {
					searchRectangleSize = value;
				}
				else {
					throw new ArgumentOutOfRangeException(nameof(value), "SearchRectangle has to be at least [1,1] in size");
				}
			}
		}


		IEntity pTarget;
		public IEntity Target {
			get => pTarget;
			protected set {
				if (value == pTarget) {
					return;
				}

				//Unregister from current target
				if (pTarget != null) {
					pTarget.OnRemoval -= OnTargetDeath;
				}

				//Register to the new target
				if (value != null) {
					value.OnRemoval += OnTargetDeath;
				}

				pTarget = value;
			}
		}

		/// <summary>
		/// Invoked when meeleAtacker attacks the current target.
		/// </summary>
		public event AttackedDelegate Attacked;

		/// <summary>
		/// Invoked each game tick the current target is in range.
		/// </summary>
		public event TargetInRangeDelegate TargetInRange;

		/// <summary>
		/// Invoked when target is acquired automatically
		/// </summary>
		public event TargetFoundDelegate TargetFound;

		public event TargetLost TargetLost;


		protected float TimeToNextSearch;
		protected float TimeToNextAttack;

		readonly IBaseUser user;

		IntVector2 searchRectangleSize;

		protected MeeleAttacker(ILevelManager level,
								bool searchForTarget,
								IntVector2 searchRectangleSize,
								float timeBetweenSearches,
								float timeBetweenAttacks,
								IBaseUser user)
			:base(level)
		{
			this.SearchForTarget = searchForTarget;
			this.SearchRectangleSize = searchRectangleSize;
			this.TimeBetweenSearches = timeBetweenSearches;
			this.TimeBetweenAttacks = timeBetweenAttacks;

			this.TimeToNextSearch = timeBetweenSearches;
			this.TimeToNextAttack = TimeBetweenAttacks;

			this.user = user;
		}

		/// <summary>
		/// Constructor for loading, needs the <paramref name="timeToNextAttack"/>
		/// </summary>
		/// <param name="level">Current level.</param>
		/// <param name="searchForTarget">If the new instance should automatically search for target.</param>
		/// <param name="searchRectangleSize">Size of the searched area</param>
		/// <param name="timeBetweenSearches"></param>
		/// <param name="timeBetweenAttacks"></param>
		/// <param name="timeToNextSearch"></param>
		/// <param name="timeToNextAttack"></param>
		/// <param name="user">Plugin providing the implementation of methods needed for our implementation.</param>
		protected MeeleAttacker(ILevelManager level,
								bool searchForTarget,
								IntVector2 searchRectangleSize,
								float timeBetweenSearches,
								float timeBetweenAttacks,
								float timeToNextSearch,
								float timeToNextAttack,
								IBaseUser user)
			: this(level, searchForTarget, searchRectangleSize, timeBetweenSearches, timeBetweenAttacks,  user)
		{
			this.TimeToNextSearch = timeToNextSearch;
			this.TimeToNextAttack = timeToNextAttack;
		}

		/// <summary>
		/// Sets <paramref name="newTarget"/> as the current target.
		/// </summary>
		/// <param name="newTarget">The new target to attack.</param>
		public void Attack(IEntity newTarget)
		{
			Target = newTarget;
		}

		/// <summary>
		/// Stops attacking any current target.
		/// </summary>
		public void StopAttacking()
		{
			Target = null;
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents)
		{
			base.AddedToEntity(entityDefaultComponents);

			AddedToEntity(typeof(MeeleAttacker), entityDefaultComponents);
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents)
		{
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(MeeleAttacker), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}

		/// <summary>
		/// Progresses the attack, stepping the time between attacks if <see cref="Target"/> is in range
		/// </summary>
		/// <param name="timeStep">Time elapsed since the last update</param>
		/// <returns>true if <see cref="Target"/> is in range, false if it is not</returns>
		protected bool TryAttack(float timeStep)
		{
			if (IsInRange(Target)) {
				InvokeTargetInRange(Target);
				TimeToNextAttack -= timeStep;
				if (TimeToNextAttack < 0) {
					TimeToNextAttack = TimeBetweenAttacks;
					InvokeOnAttacked(Target);
				}


				return true;
			}

			return false;
		}

		/// <summary>
		/// If <see cref="SearchForTarget"/> is true,searches for targets in rectangle of size <see cref="SearchRectangleSize"/> around the owning entitys position
		/// Otherwise does nothing.
		/// Searches only for units, buildings can only be targeted by a call to <see cref="Attack(IEntity)"/>.
		/// </summary>
		/// <param name="timeStep"></param>
		protected void SearchForTargetInRange(float timeStep)
		{
			if (!SearchForTarget) return;

			TimeToNextSearch -= timeStep;
			if (TimeToNextSearch > 0) return;

			TimeToNextSearch = TimeBetweenSearches;
			ITile centerTile = Level.Map.GetContainingTile(Entity.Position);
			IntVector2 topLeft = centerTile.MapLocation - (searchRectangleSize / 2);
			List<IUnit> unitsInRange = new List<IUnit>();
			Level.Map.ForEachInRectangle(topLeft, topLeft + searchRectangleSize,
									(tile) => {
										unitsInRange.AddRange(from unit in tile.Units
															where unit.Player != Entity.Player
															select unit);
									});

			Target = PickTarget(unitsInRange);
			if (Target != null) {
				Target.OnRemoval += OnTargetDeath;
				InvokeTargetFound(Target);
			}
			
		}

		/// <summary>
		/// Removes the target on its death.
		/// </summary>
		void OnTargetDeath()
		{
			Target = null;
			InvokeTargetLost();
		}

		bool IsInRange(IEntity target)
		{
			try {
				return user.IsInRange(this, target);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Error,
								$"There was an unexpected exception in {nameof(user.IsInRange)}: {e.Message}");
				return false;
			}
		}

		IUnit PickTarget(ICollection<IUnit> possibleTargets)
		{
			try
			{
				return user.PickTarget(possibleTargets);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Error,
								$"There was an unexpected exception in {nameof(user.PickTarget)}: {e.Message}");
				return null;
			}
		}

		void InvokeOnAttacked(IEntity target)
		{
			try {
				Attacked?.Invoke(this, target);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(Attacked)}: {e.Message}");
			}
		}

		void InvokeTargetInRange(IEntity target)
		{
			try
			{
				TargetInRange?.Invoke(this, target);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(TargetInRange)}: {e.Message}");
			}
		}

		void InvokeTargetFound(IEntity target)
		{
			try
			{
				TargetFound?.Invoke(this, target);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(TargetFound)}: {e.Message}");
			}
		}

		void InvokeTargetLost()
		{
			try
			{
				TargetLost?.Invoke(this);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Debug,
								$"There was an unexpected exception during the invocation of {nameof(TargetLost)}: {e.Message}");
			}
		}
	}
}
