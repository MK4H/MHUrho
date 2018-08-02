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
	public delegate bool IsInRange(MeeleAttacker attacker, IEntity target);

	public delegate void TargetInRange(MeeleAttacker attacker, IEntity target);

	public delegate void Attacked(MeeleAttacker attacker, IEntity target);

	public delegate void TargetFound(MeeleAttacker attacker, IEntity target);

	public delegate IUnit PickTarget(List<IUnit> possibleTargets);

	public abstract class MeeleAttacker : DefaultComponent
	{

		public bool SearchForTarget { get; set; }

		public float TimeBetweenSearches { get; set; }

		public float TimeBetweenAttacks { get; set; }

		public float AttacksPerSecond {
			get => 1.0f / TimeBetweenAttacks;
			set => TimeBetweenAttacks = 1.0f / value;
		}

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

				if (pTarget != null) {
					pTarget.OnRemoval -= OnTargetDeath;
				}

				if (value != null) {
					value.OnRemoval += OnTargetDeath;
				}

				pTarget = value;
			} }

		public event Attacked Attacked;
		public event TargetInRange TargetInRange;
		public event TargetFound TargetFound;


		protected IsInRange IsInRange;
		protected PickTarget PickTarget;

		protected float TimeToNextSearch;
		protected float TimeToNextAttack;

		IntVector2 searchRectangleSize;

		protected MeeleAttacker(ILevelManager level,
								bool searchForTarget,
								IntVector2 searchRectangleSize,
								float timeBetweenSearches,
								float timeBetweenAttacks,
								IsInRange isInRange, 
								PickTarget pickTarget)
			:base(level)
		{
			this.SearchForTarget = searchForTarget;
			this.SearchRectangleSize = searchRectangleSize;
			this.TimeBetweenSearches = timeBetweenSearches;
			this.TimeBetweenAttacks = timeBetweenAttacks;

			this.TimeToNextSearch = timeBetweenSearches;
			this.TimeToNextAttack = TimeBetweenAttacks;

			this.IsInRange = isInRange;
			this.PickTarget = pickTarget;
		}

		/// <summary>
		/// Constructor for loading, needs the <paramref name="timeToNextAttack"/>
		/// </summary>
		/// <param name="level"></param>
		/// <param name="searchForTarget"></param>
		/// <param name="searchRectangleSize"></param>
		/// <param name="timeBetweenSearches"></param>
		/// <param name="timeBetweenAttacks"></param>
		/// <param name="timeToNextSearch"></param>
		/// <param name="timeToNextAttack"></param>
		/// <param name="isInRange"></param>
		/// <param name="pickTarget"></param>
		protected MeeleAttacker(ILevelManager level,
								bool searchForTarget,
								IntVector2 searchRectangleSize,
								float timeBetweenSearches,
								float timeBetweenAttacks,
								float timeToNextSearch,
								float timeToNextAttack,
								IsInRange isInRange,
								PickTarget pickTarget)
			: this(level, searchForTarget, searchRectangleSize, timeBetweenSearches, timeBetweenAttacks,  isInRange, pickTarget)
		{
			this.TimeToNextSearch = timeToNextSearch;
			this.TimeToNextAttack = timeToNextAttack;
		}

		public void Attack(IEntity entity)
		{
			Target = entity;
		}

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
		/// <param name="timeStep">timestep of the update from which it was called</param>
		/// <returns>true if <see cref="Target"/> is in range, false if it is not</returns>
		protected bool TryAttack(float timeStep)
		{
			if (IsInRange(this, Target)) {
				TargetInRange?.Invoke(this, Target);
				TimeToNextAttack -= timeStep;
				if (TimeToNextAttack < 0) {
					TimeToNextAttack = TimeBetweenAttacks;
					Attacked?.Invoke(this, Target);
				}


				return true;
			}

			return false;
		}

		/// <summary>
		/// If <see cref="SearchForTarget"/> is true,searches for targets in rectangle of size <see cref="SearchRectangleSize"/> around the owning entitys position
		/// Otherwise does nothing
		/// </summary>
		/// <param name="timeStep"></param>
		protected void SearchForTargetInRange(float timeStep)
		{
			if (!SearchForTarget) return;

			TimeToNextSearch -= timeStep;
			if (TimeToNextSearch > 0) return;

			TimeToNextSearch = TimeBetweenSearches;
			ITile centerTile = Map.GetContainingTile(Entity.Position);
			IntVector2 topLeft = centerTile.MapLocation - (searchRectangleSize / 2);
			List<IUnit> unitsInRange = new List<IUnit>();
			Map.ForEachInRectangle(topLeft, topLeft + searchRectangleSize,
									(tile) => {
										unitsInRange.AddRange(from unit in tile.Units
															where unit.Player != Entity.Player
															select unit);
									});

			Target = PickTarget(unitsInRange);
			if (Target != null) {
				Target.OnRemoval += OnTargetDeath;
				TargetFound?.Invoke(this, Target);
			}
			
		}

		void OnTargetDeath()
		{
			Target = null;
		}
	}
}
