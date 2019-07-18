using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MHUrho.Control;
using MHUrho.DefaultComponents;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.PathFinding;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.WorldMap;
using MoreLinq;
using ShowcasePackage.Buildings;
using ShowcasePackage.Misc;
using ShowcasePackage.Units;
using Urho;


namespace ShowcasePackage.Players
{
	/// <summary>
	/// Represents the <see cref="AggressivePlayer"/> player type.
	/// </summary>
	public class AggressivePlayerType : PlayerAITypePlugin
	{

		public override int ID => 2;

		public override string Name => "Aggressive";

		public ChickenType Chicken { get; private set; }
		public WolfType Wolf { get; private set; }


		public GateType Gate { get; private set; }
		public TowerType Tower { get; private set; }
		public KeepType Keep { get; private set; }
		public WallType Wall { get; private set; }
		public TreeCutterType TreeCutter { get; private set; }

		public override PlayerAIInstancePlugin CreateNewInstance(ILevelManager level, IPlayer player)
		{
			return AggressivePlayer.CreateNew(level, player, this);
		}

		public override PlayerAIInstancePlugin GetInstanceForLoading(ILevelManager level, IPlayer player)
		{
			return AggressivePlayer.GetInstanceForLoading(level, player, this);
		}

		protected override void Initialize(XElement extensionElement, GamePack package)
		{
			Chicken = (ChickenType)package.GetUnitType(ChickenType.TypeID).Plugin;
			Wolf = (WolfType)package.GetUnitType(WolfType.TypeID).Plugin;
			Gate = (GateType)package.GetBuildingType(GateType.TypeID).Plugin;
			Tower = (TowerType)package.GetBuildingType(TowerType.TypeID).Plugin;
			Keep = (KeepType)package.GetBuildingType(KeepType.TypeID).Plugin;
			Wall = (WallType)package.GetBuildingType(WallType.TypeID).Plugin;
			TreeCutter = (TreeCutterType)package.GetBuildingType(TreeCutterType.TypeID).Plugin;
		}
	}

	/// <summary>
	/// Player that builds a few production buildings and then just spawns lots of units and attacks.
	/// </summary>
	public class AggressivePlayer : PlayerWithKeep
	{

		class CutterWrapper {

			public IBuilding Building => cutter.Building;

			/// <summary>
			/// Possible placement of the cutters relative to the keep center tile.
			/// +X coord is relative forward direction of the keep, +Y is relative right of the keep.
			/// </summary>
			static readonly IntVector2[] PossiblePositions = {
														new IntVector2(-8, 2),
														new IntVector2(-8, 0),
														new IntVector2(-8, -2),
														new IntVector2(8, 2),
														new IntVector2(8, 0),
														new IntVector2(8, -2),
														new IntVector2(2, -8),
														new IntVector2(0, -8),
														new IntVector2(-2, -8),
														new IntVector2(2, 8),
														new IntVector2(0, 8),
														new IntVector2(-2, 8),
													};

			

			readonly TreeCutter cutter;
			readonly AggressivePlayer player;
			readonly IntVector2 position;

			public static void SaveCutters(AggressivePlayer player, SequentialPluginDataWriter writer, IEnumerable<CutterWrapper> cutters)
			{
				writer.StoreNext(player.takenPositions.Count);
				foreach (var cutter in cutters) {
					cutter.Save(writer);
				}
			}

			public static Dictionary<IBuilding, CutterWrapper> LoadCutters(SequentialPluginDataReader reader, AggressivePlayer player, ILevelManager level)
			{
				Dictionary<IBuilding, CutterWrapper> cutters = new Dictionary<IBuilding, CutterWrapper>();

				reader.GetNext(out int numberOfCutters);
				for (int i = 0; i < numberOfCutters; i++) {
					CutterWrapper newWrapper = new CutterWrapper(reader, player, level);
					cutters.Add(newWrapper.cutter.Building, newWrapper);
					player.takenPositions.Add(newWrapper.position);
				}
				return cutters;
			}

			public static CutterWrapper CreateNewCutter(ILevelManager level, AggressivePlayer player)
			{
				foreach (var position in PossiblePositions) {

					ITile tile = GetTileRelativeToKeepCenter(position, player);
					if (player.takenPositions.Contains(position) ||
						!player.type.TreeCutter.CanBuild(tile.MapLocation, player.Player, level)) {
						continue;
					}

					IBuilding newBuilding = level.BuildBuilding(player.type.TreeCutter.MyTypeInstance,
																tile.MapLocation,
																Quaternion.Identity,
																player.Player);
					if (newBuilding != null) {
						player.takenPositions.Add(position);
						return new CutterWrapper(newBuilding, player, position);
					}
				}
				return null;
			}

			public void Destroyed()
			{
				player.takenPositions.Remove(position);
			}

			CutterWrapper(IBuilding building, AggressivePlayer player, IntVector2 position)
			{
				this.cutter = (TreeCutter) building.BuildingPlugin;
				this.player = player;
				this.position = position;
			}

			CutterWrapper(SequentialPluginDataReader reader, AggressivePlayer player, ILevelManager level)
			{
				reader.GetNext(out int id);
				this.cutter = (TreeCutter)level.GetBuilding(id).Plugin;
				this.player = player;
				reader.GetNext(out position);
			}

			static ITile GetTileRelativeToKeepCenter(IntVector2 relativeDiff, AggressivePlayer player)
			{
				Vector3 worldPosition = player.Keep.Building.Center +
										player.Keep.Building.Forward * relativeDiff.X +
										player.Keep.Building.Right * relativeDiff.Y;

				return player.Player.Level.Map.GetContainingTile(worldPosition);
			}

			void Save(SequentialPluginDataWriter writer)
			{
				writer.StoreNext(cutter.Building.ID);
				writer.StoreNext(position);
			}
		
		}

		enum States { BuildCutters, BuildUnits, AttackEnemy };

		abstract class State : IDisposable {

			public abstract States Ident { get; }

			protected readonly AggressivePlayer Player;
			protected ILevelManager Level => Player.Player.Level;

			protected State(AggressivePlayer player)
			{
				this.Player = player;
			}

			public static State Load(SequentialPluginDataReader reader, AggressivePlayer player)
			{
				reader.GetNext(out int intState);
				switch ((States) intState) {
					case States.BuildCutters:
						return new BuildCutters(reader, player);
					case States.BuildUnits:
						return new BuildUnits(reader, player);
					case States.AttackEnemy:
						return new AttackEnemy(reader, player);
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			public virtual void UnitAdded(IUnit unit) {

			}

			public virtual void UnitKilled(IUnit unit) {
				if (Player.wolfs.Remove(unit)) {
					return;
				}

				if (Player.chickens.Remove(unit)) {
					return;
				}
			}

			public virtual void BuildingAdded(IBuilding building) {

			}

			public virtual void BuildingDestroyed(IBuilding building)
			{
				if (Player.cutters.TryGetValue(building, out var cutter)) {
					cutter.Destroyed();
					Player.cutters.Remove(building);
					return;
				}
			}

			public virtual double ResourceAmountChanged(ResourceType resourceType,
														double currentAmount,
														double requestedNewAmount) {
				return requestedNewAmount;
			}

			public abstract void OnUpdate(float timeStep);

			public abstract void Dispose();

			public abstract void Save(SequentialPluginDataWriter writer);
		}

		class BuildCutters : State {

			public override States Ident => States.BuildCutters;

			readonly Timeout cutterBuildTimeout;

			const double BuildTimeout = 1;

			public BuildCutters(SequentialPluginDataReader reader, AggressivePlayer player)
				: base(player)
			{
				
			}

			public BuildCutters(State previousState, AggressivePlayer player)
				: base(player)
			{
				previousState?.Dispose();
				this.cutterBuildTimeout = new Timeout(BuildTimeout);
			}

			public override void OnUpdate(float timeStep)
			{
				if (!cutterBuildTimeout.Update(timeStep, true)) {
					return;
				}

				if (Player.cutters.Count == TargetNumberOfCutters) {
					Player.currentState = new BuildUnits(this, Player);
					return;
				}

				if (!Player.type.TreeCutter.Cost.HasResources(Player.Player)) {
					return;
				}

				CutterWrapper newCutter = CutterWrapper.CreateNewCutter(Level, Player);
				if (newCutter != null) {
					Player.type.TreeCutter.Cost.TakeFrom(Player.Player);
					Player.cutters.Add(newCutter.Building, newCutter);
				}
			}

			public override void Dispose()
			{
				
			}

			public override void Save(SequentialPluginDataWriter writer)
			{
				writer.StoreNext((int)Ident);
			}
		}

		abstract class Defend : State {

			protected readonly Timeout CutterCheckTimeout;
			protected readonly Timeout DefendCheckTimeout;

			protected Defend(double cutterCheckTimeout, double defendCheckTimeout, AggressivePlayer player)
				: base(player)
			{
				this.CutterCheckTimeout = new Timeout(cutterCheckTimeout);
				this.DefendCheckTimeout = new Timeout(defendCheckTimeout);
			}

			public override void OnUpdate(float timeStep)
			{
				if (CutterCheckTimeout.Update(timeStep, true)) {
					HoldCutters();
				}

				if (DefendCheckTimeout.Update(timeStep, true) && !DefendCastle()) {
					BackToCastle();
				}
			}

			protected void HoldCutters()
			{
				if (Player.cutters.Count >= TargetNumberOfCutters)
				{
					return;
				}

				if (!Player.type.TreeCutter.Cost.HasResources(Player.Player))
				{
					return;
				}

				CutterWrapper newCutter = CutterWrapper.CreateNewCutter(Level, Player);
				if (newCutter != null)
				{
					Player.type.TreeCutter.Cost.TakeFrom(Player.Player);
					Player.cutters.Add(newCutter.Building, newCutter);
				}
			}

			protected bool DefendCastle()
			{
				bool foundEnemy = false;

				ITile keepTile = Level.Map.GetContainingTile(Player.Keep.Building.Center);
				foreach (var tile in Level.Map.GetTilesInSpiral(keepTile, 50))
				{
					IUnit enemyUnit = tile.Units.FirstOrDefault(unit => unit.Player.IsEnemy(Player.Player));
					if (enemyUnit == null)
					{
						continue;
					}

					//Found enemy, attack the enemy unit
					foundEnemy = true;
					foreach (var unit in Player.AllUnits)
					{
						unit.Order(new AttackOrder(enemyUnit));
					}
					break;
				}

				return foundEnemy;
			}

			protected void BackToCastle()
			{
				//Move back to keep
				Player.Keep.GetFormationController(Player.Keep.Building.Center)
					.MoveToFormation(new UnitGroup(Player.AllUnits));
			}
		}

		class BuildUnits : Defend
		{
			public override States Ident => States.BuildUnits;

			const double CutterTimeout = 5;
			const double DefenseTimeout = 5;

			Timeout spawnTimeout = new Timeout(0.5);

			bool wolfPriority = true;

			public BuildUnits(SequentialPluginDataReader reader, AggressivePlayer player)
				: base(CutterTimeout, DefenseTimeout, player)
			{

			}

			public BuildUnits(State previousState, AggressivePlayer player)
				: base(CutterTimeout, DefenseTimeout, player)
			{
				previousState?.Dispose();
			}

			public override void OnUpdate(float timeStep)
			{
				base.OnUpdate(timeStep);

				if (spawnTimeout.Update(timeStep, true)) {
					//Switch if we first spawn chicken or wolf, so that we dont waste all resources always on one type
					if (wolfPriority) {
						SpawnWolf();
						SpawnChicken();
					}
					else {
						SpawnChicken();
						SpawnWolf();
					}
					wolfPriority = !wolfPriority;
				}

				if (Player.wolfs.Count >= TargetNumberOfWolfs && Player.chickens.Count >= TargetNumberOfChickens) {
					Player.currentState = new AttackEnemy(this, Player);
				}
			}

			public override void Dispose()
			{

			}

			public override void Save(SequentialPluginDataWriter writer)
			{
				writer.StoreNext((int)Ident);
			}

			void SpawnWolf()
			{
				if (Player.wolfs.Count >= TargetNumberOfWolfs) {
					return;
				}

				if (!Player.type.Wolf.Cost.HasResources(Player.Player)) {
					return;
				}

				var spiral = new Spiral(Level.Map.GetContainingTile(Player.Keep.Building.Center).TopLeft).GetSpiralEnumerator();
				while (spiral.MoveNext() && spiral.ContainingSquareSize < 20) {
					ITile tile = Level.Map.GetTileByTopLeftCorner(spiral.Current);
					if (tile.Units.Count != 0 || !Player.type.Wolf.CanSpawnAt(tile)) {
						continue;
					}

					IUnit newWolf = Level.SpawnUnit(Player.type.Wolf.UnitType, tile, Quaternion.Identity, Player.Player);
					if (newWolf != null) {
						Player.type.Wolf.Cost.TakeFrom(Player.Player);
						Player.wolfs.Add(newWolf, (Wolf) newWolf.Plugin);
						break;
					}
				}
			}

			void SpawnChicken()
			{
				if (Player.chickens.Count >= TargetNumberOfChickens)
				{
					return;
				}

				if (!Player.type.Chicken.Cost.HasResources(Player.Player))
				{
					return;
				}

				var spiral = new Spiral(Level.Map.GetContainingTile(Player.Keep.Building.Center).TopLeft).GetSpiralEnumerator();
				while (spiral.MoveNext() && spiral.ContainingSquareSize < 20)
				{
					ITile tile = Level.Map.GetTileByTopLeftCorner(spiral.Current);
					if (tile.Units.Count != 0 || !Player.type.Chicken.CanSpawnAt(tile))
					{
						continue;
					}

					IUnit newChicken = Level.SpawnUnit(Player.type.Chicken.UnitType, tile, Quaternion.Identity, Player.Player);
					if (newChicken != null)
					{
						Player.type.Chicken.Cost.TakeFrom(Player.Player);
						Player.chickens.Add(newChicken, (Chicken)newChicken.Plugin);
						break;
					}
				}
			}

		}

		class AttackEnemy : Defend
		{
			public override States Ident => States.AttackEnemy;

			PlayerWithKeep target;

			const double CutterTimeout = 5;
			const double DefenseTimeout = 10;

			public AttackEnemy(SequentialPluginDataReader reader, AggressivePlayer player)
				: base(CutterTimeout, DefenseTimeout, player)
			{

			}

			public AttackEnemy(State previousState, AggressivePlayer player)
				: base(CutterTimeout, DefenseTimeout, player)
			{
				previousState?.Dispose();

				var targetPlayer = player.Player
											.GetEnemyPlayers()
											.MinBy(enemy => {
														var keep = (enemy.Plugin as PlayerWithKeep)?.Keep;
														return keep == null
																	? double.MaxValue
																	: Vector3.Distance(player.Keep.Building.Center, keep.Building.Center);
													}).FirstOrDefault();

				target = targetPlayer?.Plugin as PlayerWithKeep;
				if (target != null) {
					target.Player.OnRemoval += TargetDied;
				}
			}

			

			public override void OnUpdate(float timeStep)
			{
				if (target == null || (Player.wolfs.Count < MinWolfs && Player.chickens.Count < MinChickens)) {
					Player.currentState = new BuildUnits(this, Player);
				}

				if (CutterCheckTimeout.Update(timeStep, true)) {
					HoldCutters();
				}

				if (DefendCheckTimeout.Update(timeStep, true) && !DefendCastle()) {
					Attack();
				}
			}

			public override void Dispose()
			{

			}

			public override void Save(SequentialPluginDataWriter writer)
			{
				writer.StoreNext((int)Ident);
			}

			void Attack()
			{
				WolfsAttack();

				ChickenAttack();

			}

			void WolfsAttack()
			{
				foreach (var wolf in Player.wolfs.Values)
				{
					var distCalc = new Wolf.WolfDistCalcThroughWalls(wolf);
					List<ITile> tileList = Level.Map.PathFinding.GetTileList(wolf.Unit.Position,
																			Level
																				.Map.PathFinding
																				.GetClosestNode(target.Keep.Building
																									.Center),
																			distCalc);

					if (tileList == null)
					{
						break;
					}

					if (distCalc.CanBreakThrough.Contains(tileList[0]))
					{
						wolf.ExecuteOrder(new AttackOrder(tileList[0].Building));
						continue;
					}

					if (tileList.Count > 1 && distCalc.CanBreakThrough.Contains(tileList[1]))
					{
						ITile sourceTile = tileList[0];
						ITile targetTile = tileList[1];

						IBuilding buildingStraight = targetTile.Building;

						IBuilding buildingDiag1 = Level.Map
														.GetTileByMapLocation(new IntVector2(sourceTile.MapLocation.X,
																							targetTile.MapLocation.Y))
														.Building;
						IBuilding buildingDiag2 = Level.Map
														.GetTileByMapLocation(new IntVector2(sourceTile.MapLocation.X,
																							targetTile.MapLocation.Y))
														.Building;

						if (buildingDiag1 == null || buildingDiag2 == null)
						{
							wolf.ExecuteOrder(new AttackOrder(buildingStraight));
							continue;
						}

						if (buildingDiag1 != null &&
							buildingDiag1.Player != Level.NeutralPlayer &&
							!buildingDiag1.Player.IsFriend(Player.Player))
						{
							wolf.ExecuteOrder(new AttackOrder(buildingDiag1));
							continue;
						}

						wolf.ExecuteOrder(new AttackOrder(buildingDiag2));
						continue;
					}

					//Get last tile that we dont have to break through anything
					ITile beforeBreakthrough = null;
					for (int i = 0; i < tileList.Count; i++) {
						if (!distCalc.CanBreakThrough.Contains(tileList[i])) {
							continue;
						}

						beforeBreakthrough = tileList[i - 1];
						break;
					}

					//If we can get to the end, get to the end
					if (beforeBreakthrough == null) {
						beforeBreakthrough = tileList[tileList.Count - 1];
					}

					//Try go to the last tile we dont have to break through anything to get to.
					wolf.ExecuteOrder(new MoveOrder(Level.Map.PathFinding.GetTileNode(beforeBreakthrough)));

				}
			}

			void ChickenAttack()
			{
				foreach (var chicken in Player.chickens.Values)
				{
					var distCalc = new Chicken.ChickenDistCalcThroughWalls(chicken);
					List<ITile> tileList = Level.Map.PathFinding.GetTileList(chicken.Unit.Position,
																			Level.Map
																				.PathFinding
																				.GetClosestNode(target.Keep
																									.Building
																									.Center),
																			distCalc);

					if (tileList == null)
					{
						break;
					}

	
					if (distCalc.CanBreakThrough.Contains(tileList[0]))
					{
						chicken.ExecuteOrder(new AttackOrder(tileList[0].Building));
						continue;
					}
					
					for (int i = 1; i < tileList.Count; i++)
					{
						if (!distCalc.CanBreakThrough.Contains(tileList[i])) {
							continue;
						}

						if (i < 7) {
							ITile sourceTile = tileList[i - 1];
							ITile targetTile = tileList[i];

							IBuilding buildingStraight = targetTile.Building;

							IBuilding buildingDiag1 = Level.Map
															.GetTileByMapLocation(new IntVector2(sourceTile.MapLocation.X,
																								targetTile.MapLocation.Y))
															.Building;
							IBuilding buildingDiag2 = Level.Map
															.GetTileByMapLocation(new IntVector2(sourceTile.MapLocation.X,
																								targetTile.MapLocation.Y))
															.Building;

							if (buildingDiag1 == null || buildingDiag2 == null)
							{
								chicken.ExecuteOrder(new AttackOrder(buildingStraight));
								break;
							}

							if (buildingDiag1 != null &&
								buildingDiag1.Player != Level.NeutralPlayer &&
								!buildingDiag1.Player.IsFriend(Player.Player))
							{
								chicken.ExecuteOrder(new AttackOrder(buildingDiag1));
								break;
							}

							chicken.ExecuteOrder(new AttackOrder(buildingDiag2));
							break;
						}

						chicken.ExecuteOrder(new MoveOrder(Level.Map.PathFinding.GetTileNode(tileList[i - 1])));
						break;
					}
				}
			}

			void TargetDied(IPlayer player)
			{
				target = null;
			}
		}

		const int TargetNumberOfCutters = 1;
		const int TargetNumberOfWolfs = 0;
		const int TargetNumberOfChickens = 1;
		const int MinWolfs = 1;
		const int MinChickens = 1;

		IEnumerable<UnitSelector> AllUnits =>
			chickens.Values
					.Select(chicken => chicken.Unit.GetDefaultComponent<UnitSelector>())
					.Concat(wolfs.Values.Select(wolf => wolf.Unit.GetDefaultComponent<UnitSelector>()));

		Dictionary<IBuilding, CutterWrapper> cutters;
		Dictionary<IUnit, Chicken> chickens;
		Dictionary<IUnit, Wolf> wolfs;

		readonly HashSet<IntVector2> takenPositions;

		readonly AggressivePlayerType type;


		State currentState;

		public static AggressivePlayer CreateNew(ILevelManager level, IPlayer player, AggressivePlayerType type)
		{
			var instance = new AggressivePlayer(level, player, type);

			return instance;
		}

		public static AggressivePlayer GetInstanceForLoading(ILevelManager level, IPlayer player, AggressivePlayerType type)
		{
			return new AggressivePlayer(level, player, type);
		}

		protected AggressivePlayer(ILevelManager level, IPlayer player, AggressivePlayerType type)
			: base(level, player, type.Keep)
		{
			this.type = type;
			this.takenPositions = new HashSet<IntVector2>();
			this.cutters = new Dictionary<IBuilding, CutterWrapper>();
			this.chickens = new Dictionary<IUnit, Chicken>();
			this.wolfs = new Dictionary<IUnit, Wolf>();

		}

		public override void OnUpdate(float timeStep)
		{
			currentState.OnUpdate(timeStep);
		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var writer = pluginData.GetWriterForWrappedSequentialData();
			CutterWrapper.SaveCutters(this, writer, cutters.Values);
			currentState.Save(writer);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			Keep = GetKeep();
			var reader = pluginData.GetReaderForWrappedSequentialData();
			cutters = CutterWrapper.LoadCutters(reader, this, Level);
			currentState = State.Load(reader, this);

			foreach (var wolf in Player.GetUnitsOfType(type.Wolf.UnitType)) {
				wolfs.Add(wolf, (Wolf)wolf.Plugin);
			}

			foreach (var chicken in Player.GetUnitsOfType(type.Chicken.UnitType)) {
				chickens.Add(chicken, (Chicken)chicken.Plugin);
			}

		}

		public override void Dispose()
		{

		}

		public override void UnitAdded(IUnit unit)
		{
			currentState.UnitAdded(unit);
		}

		public override void UnitKilled(IUnit unit)
		{
			if (Level.IsEnding)
			{
				//Do nothing
				return;
			}

			currentState.UnitKilled(unit);
		}

		/// <summary>
		/// Called when the player is being loaded into freshly started level that had not been played before.
		/// This level therefore has no state specific to this player saved, so it must be initialized.
		/// </summary>
		/// <param name="level">The level being loaded</param>
		public override void Init(ILevelManager level)
		{
			Keep = GetKeep();
			currentState = new BuildCutters((State)null, this);

			foreach (var wolf in Player.GetUnitsOfType(type.Wolf.UnitType)) {
				wolfs.Add(wolf, (Wolf)wolf.Plugin);
			}

			foreach (var chicken in Player.GetUnitsOfType(type.Chicken.UnitType)) {
				chickens.Add(chicken, (Chicken)chicken.Plugin);
			}
		}

		public override void BuildingAdded(IBuilding building)
		{
			currentState.BuildingAdded(building);
		}

		public override void BuildingDestroyed(IBuilding building)
		{
			if (Level.IsEnding) {
				return;
			}

			if (building == Keep.Building) {
				Player.RemoveFromLevel();
				return;
			}

			currentState.BuildingDestroyed(building);
		}

		public override double ResourceAmountChanged(ResourceType resourceType, double currentAmount, double requestedNewAmount)
		{
			return currentState.ResourceAmountChanged(resourceType, currentAmount, requestedNewAmount);
		}



	}
}
