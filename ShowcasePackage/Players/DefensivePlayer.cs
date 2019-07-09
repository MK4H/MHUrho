﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MHUrho.DefaultComponents;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.WorldMap;
using ShowcasePackage.Buildings;
using ShowcasePackage.Units;
using Urho;

namespace ShowcasePackage.Players
{

	/// <summary>
	/// Represents the <see cref="DefensivePlayer"/> player type.
	/// </summary>
	public class DefensivePlayerType : PlayerAITypePlugin
	{

		public override int ID => 3;

		public override string Name => "Defensive";

		public UnitType Chicken { get; private set; }
		public UnitType Wolf { get; private set; }


		public BuildingType Gate { get; private set; }
		public BuildingType Tower { get; private set; }
		public BuildingType Keep { get; private set; }
		public BuildingType Wall { get; private set; }
		public BuildingType TreeCutter { get; private set; }

		public override PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player)
		{
			return DefensivePlayer.CreateNew(level, player, this);
		}

		public override PlayerAIInstancePlugin GetInstanceForLoading(ILevelManager level, IPlayer player)
		{
			return DefensivePlayer.GetInstanceForLoading(level, player, this);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			Chicken = package.GetUnitType(ChickenType.TypeID);
			Wolf = package.GetUnitType(WolfType.TypeID);
			Gate = package.GetBuildingType(GateType.TypeID);
			Tower = package.GetBuildingType(TowerType.TypeID);
			Keep = package.GetBuildingType(KeepType.TypeID);
			Wall = package.GetBuildingType(WallType.TypeID);
			TreeCutter = package.GetBuildingType(TreeCutterType.TypeID);
		}
	}

	/// <summary>
	/// Player that builds a castle, makes a few units and mainly defends.
	/// </summary>
	public class DefensivePlayer : PlayerAIInstancePlugin
	{

		class State {

		}

		class BuildCutters : State {

		}

		class BuildCastle : State {

		}

		class AttackEnemy : State {

		}

		class ChickenWrapper
		{
			public const float DefaultWaitingTime = 4;

			public Chicken Chicken { get; private set; }
			public IRangeTarget NextTarget { get; set; }

			public float TimeToNextTargetCheck { get; set; }

			public void TrySetTarget(IRangeTarget newTarget, IMap map)
			{
				if (Chicken.Shooter.Target != null)
				{
					return;
				}

				NextTarget = newTarget;
				if (!Chicken.Shooter.ShootAt(NextTarget))
				{
					Chicken.Walker.GoTo(map.PathFinding.GetClosestNode(newTarget.CurrentPosition));
				}
				else
				{
					Chicken.Walker.Stop();
				}
			}

			public ChickenWrapper(Chicken chicken)
			{
				this.Chicken = chicken;
				NextTarget = null;
				TimeToNextTargetCheck = DefaultWaitingTime;
			}
		}

		readonly DefensivePlayerType type;

		IBuilding keep;

		List<ChickenWrapper> chickens;
		IntVector2 spawnPoint;

		int state = 0;
		float logicTimeout;

		public static DefensivePlayer CreateNew(ILevelManager level, IPlayer player, DefensivePlayerType type)
		{
			var instance = new DefensivePlayer(level, player, type) { spawnPoint = (level.Map.TopLeft + level.Map.BottomRight) / 2 };

			return instance;
		}

		public static DefensivePlayer GetInstanceForLoading(ILevelManager level, IPlayer player, DefensivePlayerType type)
		{
			return new DefensivePlayer(level, player, type);
		}

		protected DefensivePlayer(ILevelManager level, IPlayer player, DefensivePlayerType type)
			: base(level, player)
		{
			this.type = type;
			chickens = new List<ChickenWrapper>();

		}

		public override void OnUpdate(float timeStep)
		{
			logicTimeout -= timeStep;
			if (logicTimeout > 0) return;
			logicTimeout = 2;
			switch (state)
			{
				case 0:
					state = 1;
					var spiralPoint = new Spiral(spawnPoint).GetEnumerator();
					spiralPoint.MoveNext();
					for (int i = 0; i < 1; i++, spiralPoint.MoveNext())
					{
						IUnit newChicken = Level.SpawnUnit(type.Chicken, Level.Map.GetTileByMapLocation(spiralPoint.Current), Quaternion.Identity, Player);
						chickens.Add(new ChickenWrapper((Chicken)newChicken.UnitPlugin));
					}

					var target = FindTargets(spawnPoint).FirstOrDefault();
					if (target == null)
					{
						return;
					}

					foreach (var chicken in chickens)
					{
						if (!chicken.Chicken.Shooter.ShootAt(target))
						{
							chicken.Chicken.Walker.GoTo(Level.Map.PathFinding.GetClosestNode(target.CurrentPosition));
							chicken.NextTarget = target;
						}
					}
					break;
				case 1:
					var newTarget = FindTargets(spawnPoint).FirstOrDefault();
					if (newTarget == null) return;

					foreach (var chicken in chickens)
					{


					}
					break;
				default:
					throw new InvalidOperationException("Invalid state of logic");
			}
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var indexedData = pluginData.GetWriterForWrappedIndexedData();
			indexedData.Store(1, state);
			indexedData.Store(2, spawnPoint);
			indexedData.Store(3, from chicken in chickens select chicken.Chicken.Unit.ID);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			var indexedData = pluginData.GetReaderForWrappedIndexedData();

			state = indexedData.Get<int>(1);
			spawnPoint = indexedData.Get<IntVector2>(2);
			chickens = new List<ChickenWrapper>(from unit in indexedData.Get<IEnumerable<int>>(3)
												select new ChickenWrapper((Chicken)Level.GetUnit(unit).Plugin));

			keep = GetKeep();
		}

		public override void Dispose()
		{

		}

		public override void UnitAdded(IUnit unit)
		{

		}

		public override void UnitKilled(IUnit unit)
		{
			if (Level.IsEnding)
			{
				//Do nothing
				return;
			}


		}

		/// <summary>
		/// Called when the player is being loaded into freshly started level that had not been played before.
		/// This level therefore has no state specific to this player saved, so it must be initialized.
		/// </summary>
		/// <param name="level">The level being loaded</param>
		public override void Init(ILevelManager level)
		{
			keep = GetKeep();
		}

		public override void BuildingAdded(IBuilding building)
		{

		}

		public override void BuildingDestroyed(IBuilding building)
		{
			if (building == keep)
			{
				//#error DESTROY EVERYTHING, SIGNAL DEATH
			}
		}

		public override double ResourceAmountChanged(ResourceType resourceType, double currentAmount, double requestedNewAmount)
		{
			return requestedNewAmount;
		}
		IEnumerable<IRangeTarget> FindTargets(IntVector2 sourcePoint)
		{

			return from enemyPlayer in Player.GetEnemyPlayers()
				   from unit in enemyPlayer.GetAllUnits()
				   where unit.HasDefaultComponent<RangeTargetComponent>()
				   orderby (unit.Position.XZ2() - sourcePoint.ToVector2()).Length
				   select unit.GetDefaultComponent<RangeTargetComponent>();

		}

		/// <summary>
		/// Gets players keep and checks there is really only one.
		/// </summary>
		/// <returns>The players keep.</returns>
		/// <exception cref="LevelLoadingException">Thrown when there is invalid number of keeps.</exception>
		IBuilding GetKeep()
		{
			IReadOnlyList<IBuilding> keeps = Player.GetBuildingsOfType(type.Keep);
			if (keeps.Count != 1)
			{
				throw new LevelLoadingException("Player is missing a keep.");
			}
			return keeps[0];
		}
	}
}