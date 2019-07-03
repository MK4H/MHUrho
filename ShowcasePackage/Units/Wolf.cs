﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MHUrho.CameraMovement;
using MHUrho.Control;
using MHUrho.DefaultComponents;
using MHUrho.EntityInfo;
using MHUrho.Input.MandK;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UserInterface.MandK;
using MoreLinq;
using ShowcasePackage.Misc;
using Urho;

namespace ShowcasePackage.Units
{
	public class WolfType : SpawnableUnitTypePlugin {

		public static string TypeName = "Wolf";
		public static int TypeID = 5;

		public override string Name => TypeName;
		public override int ID => TypeID;

		public override Cost Cost => cost;
		public override UnitType UnitType => myType;

		const string CostElement = "cost";

		Cost cost;
		UnitType myType;

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			XElement costElem = extensionElement.Element(package.PackageManager.GetQualifiedXName(CostElement));
			cost = Cost.FromXml(costElem, package);


			myType = package.GetUnitType(ID);
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

		public override Spawner GetSpawner(GameController input, GameUI ui, CameraMover camera)
		{
			return new WolfSpawner(input, ui, camera, myType, this);
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
			sequentialData.GetNext(out hp);
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
			animationController.PlayExclusive("Units/Wolf/Wolf_Walk_cycle_.ani", 0, true);
			animationController.SetSpeed("Units/Wolf/Wolf_Walk_cycle_.ani", 2);
		}

		void OnMovementFinished(WorldWalker walker)
		{
			animationController.Stop("Units/Wolf/Wolf_Walk_cycle_.ani");
		}

		void OnMovementFailed(WorldWalker walker)
		{
			animationController.Stop("Units/Wolf/Wolf_Walk_cycle_.ani");
		}

		void OnMovementCanceled(WorldWalker walker)
		{
			animationController.Stop("Units/Wolf/Wolf_Walk_cycle_.ani");
		}

		void RegisterEvents(WorldWalker walker)
		{
			walker.MovementStarted += OnMovementStarted;
			walker.MovementFinished += OnMovementFinished;
			walker.MovementFailed += OnMovementFailed;
			walker.MovementCanceled += OnMovementCanceled;

		}
	}

	class WolfSpawner : PointSpawner {

		public override Cost Cost => myType.Cost;

		readonly WolfType myType;

		public WolfSpawner(GameController input, GameUI ui, CameraMover camera, UnitType type, WolfType myType)
			: base(input, ui, camera, type)
		{
			this.myType = myType;
		}

		

		public override IUnit SpawnAt(ITile tile, IPlayer player)
		{
			if (myType.CanSpawnAt(tile) && (Level.EditorMode || myType.Cost.HasResources(player))) {
				return Level.SpawnUnit(UnitType, tile, Quaternion.Identity, player);
			}

			return null;
		}
	}
}
