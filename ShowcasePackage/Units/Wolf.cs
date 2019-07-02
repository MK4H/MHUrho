using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MHUrho.Control;
using MHUrho.DefaultComponents;
using MHUrho.EntityInfo;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MoreLinq;
using Urho;

namespace ShowcasePackage.Units
{
	public class WolfType : UnitTypePlugin {
		public override string Name => "Wolf";
		public override int ID => 3;


		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			
		}

		public override UnitInstancePlugin CreateNewInstance(ILevelManager level, IUnit unit)
		{
			return Wolf.CreateNew(level, unit);
		}

		public override UnitInstancePlugin GetInstanceForLoading(ILevelManager level, IUnit unit)
		{
			return Wolf.CreateForLoading(level, unit);
		}

		public override bool CanSpawnAt(ITile centerTile)
		{
			//Can only spawn on a tile with no buildings and no other units
			return centerTile.Building == null && centerTile.Units.Count == 0;
		}
	}

	public class Wolf : UnitInstancePlugin,
						WorldWalker.IUser,
						MovingRangeTarget.IUser,
						UnitSelector.IUser,
						MovingMeeleAttacker.IUser
	{
		static readonly Vector3 targetOffset = new Vector3(0, 0.5f, 0);

		AnimationController animationController;
		WorldWalker walker;
		MovingMeeleAttacker attacker;

		ClimbingDistCalc distCalc = new ClimbingDistCalc(0.5f, 0.2f);

		float hp;
		HealthBar healthbar;

		Wolf(ILevelManager level, IUnit unit)
			:base(level, unit)
		{

		}

		public static Wolf CreateNew(ILevelManager level, IUnit unit)
		{
			Wolf wolf = new Wolf(level, unit);
			wolf.animationController = unit.CreateComponent<AnimationController>();
			wolf.walker = WorldWalker.CreateNew(wolf, level);
			wolf.attacker = MovingMeeleAttacker.CreateNew(wolf,
													level,
													true,
													new IntVector2(20, 20),
													1,
													5,
													0.5f);
			UnitSelector.CreateNew(wolf, level);
			MovingRangeTarget.CreateNew(wolf, level, targetOffset);
			unit.AlwaysVertical = false;
			wolf.hp = 100;
			wolf.healthbar = new HealthBar(level, unit, new Vector3(0, 15, 0), new Vector2(0.5f, 0.1f), wolf.hp);

			return wolf;
		}

		public static Wolf CreateForLoading(ILevelManager level, IUnit unit)
		{
			return new Wolf(level, unit);
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var sequentialData = pluginData.GetWriterForWrappedSequentialData();
			sequentialData.StoreNext(hp);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			animationController = Unit.CreateComponent<AnimationController>();
			walker = Unit.GetDefaultComponent<WorldWalker>();
			attacker = Unit.GetDefaultComponent<MovingMeeleAttacker>();

			RegisterEvents(walker);
			var sequentialData = pluginData.GetReaderForWrappedSequentialData();
			hp = sequentialData.GetNext<float>();
			healthbar = new HealthBar(Level, Unit, new Vector3(0, 15, 0), new Vector2(0.5f, 0.1f), hp);
		}

		public override void Dispose()
		{
			healthbar.Dispose();
		}

		public override void OnHit(IEntity other, object userData)
		{
			if (other.Player == Unit.Player)
			{
				return;
			}

			hp -= 5;
			if (hp < 0)
			{
				healthbar.SetHealth(0);
				walker.Enabled = false;
				attacker.Enabled = false;
				Unit.RemoveFromLevel();
			}
			else
			{
				healthbar.SetHealth((int)hp);
			}
		}

		INodeDistCalculator WorldWalker.IUser.GetNodeDistCalculator()
		{
			return distCalc;
		}

		IEnumerable<Waypoint> MovingRangeTarget.IUser.GetFutureWaypoints(MovingRangeTarget target)
		{
			return walker.GetRestOfThePath(targetOffset);
		}

		bool UnitSelector.IUser.ExecuteOrder(Order order)
		{
			order.Executed = false;
			if (order.PlatformOrder)
			{
				switch (order)
				{
					case MoveOrder moveOrder:
						order.Executed = walker.GoTo(moveOrder.Target);
						break;
					case AttackOrder attackOrder:
						if (Unit.Player.IsEnemy(attackOrder.Target.Player))
						{
							attacker.Attack(attackOrder.Target);
							order.Executed = true;
						}

						if (order.Executed)
						{
							attacker.SearchForTarget = false;
						}
						break;
				}
			}

			return order.Executed;
		}

		bool MeeleAttacker.IBaseUser.IsInRange(MeeleAttacker attacker, IEntity target)
		{
			return Vector3.Distance(target.Position, Unit.Position) < 1;
		}

		IUnit MeeleAttacker.IBaseUser.PickTarget(ICollection<IUnit> possibleTargets)
		{
			return possibleTargets.MinBy((target) => Vector3.Distance(target.Position, Unit.Position)).FirstOrDefault();
		}

		void MovingMeeleAttacker.IUser.MoveTo(Vector3 position)
		{
			walker.GoTo(Level.Map.PathFinding.GetClosestNode(position));
		}

		void OnMovementStarted(WorldWalker walker)
		{
			animationController.PlayExclusive("Units/Dog/Walk.ani", 0, true);
			animationController.SetSpeed("Units/Dog/Walk.ani", 2);
		}

		void OnMovementFinished(WorldWalker walker)
		{
			animationController.Stop("Units/Dog/Walk.ani");
		}

		void OnMovementFailed(WorldWalker walker)
		{
			animationController.Stop("Units/Dog/Walk.ani");
		}

		void OnMovementCanceled(WorldWalker walker)
		{
			animationController.Stop("Units/Dog/Walk.ani");
		}

		void RegisterEvents(WorldWalker walker)
		{
			walker.MovementStarted += OnMovementStarted;
			walker.MovementFinished += OnMovementFinished;
			walker.MovementFailed += OnMovementFailed;
			walker.MovementCanceled += OnMovementCanceled;

		}
	}
}
