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
	public abstract class MeeleAttacker : DefaultComponent
	{
		public interface IBaseNotificationReceiver {
			//TODO: Add MeeleTarget component, with attack range
			bool IsInRange(MeeleAttacker attacker, IEntity target);

			void Attacked(MeeleAttacker attacker, IEntity target);

			IEntity PickTarget(List<IEntity> possibleTargets);

		}

		public bool AttackIfInRange { get; set; }

		public float TimeBetweenAttacks { get; set; }

		public float AttacksPerSecond {
			get => 1.0f / TimeBetweenAttacks;
			set => TimeBetweenAttacks = 1.0f / value;
		}

		public IntVector2 TargetSearchRectangleSize {
			get => targetSearchRectangleSize;
			set {
				if (value.X > 0 && value.Y > 0) {
					targetSearchRectangleSize = value;
				}
				else {
					throw new ArgumentOutOfRangeException(nameof(value), "SearchRectangle has to be at least [1,1] in size");
				}
			}
		}

		public IEntity Target { get; protected set; }

		protected abstract IBaseNotificationReceiver BaseNotificationReceiver { get; }

		protected float timeToNextAttack;

		IntVector2 targetSearchRectangleSize;

		protected MeeleAttacker(ILevelManager level)
			:base(level)
		{

		}

		public void Attack(IEntity entity)
		{
			Target = entity;
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
			if (BaseNotificationReceiver.IsInRange(this, Target)) {
				timeToNextAttack -= timeStep;
				if (timeToNextAttack < 0) {
					timeToNextAttack = TimeBetweenAttacks;
					BaseNotificationReceiver.Attacked(this, Target);
				}


				return true;
			}

			return false;
		}

		/// <summary>
		/// If <see cref="AttackIfInRange"/> is true,searches for targets in rectangle of size <see cref="TargetSearchRectangleSize"/> around the owning entitys position
		/// Otherwise does nothing
		/// </summary>
		protected void SearchForTargetInRange()
		{
			if (AttackIfInRange) {
				ITile centerTile = Map.GetContainingTile(Entity.Position);
				IntVector2 topLeft = centerTile.MapLocation - (targetSearchRectangleSize / 2);
				List<IUnit> unitsInRange = new List<IUnit>();
				Map.ForEachInRectangle(topLeft, topLeft + targetSearchRectangleSize,
										(tile) => {
											unitsInRange.AddRange(from unit in tile.Units
																where unit.Player != Entity.Player
																select unit);
										});
			}
		}

		
	}
}
