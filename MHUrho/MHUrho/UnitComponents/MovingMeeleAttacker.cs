using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.UnitComponents
{
	public delegate void MoveTo(Vector3 position);

	/// <summary>
	/// A component that can check for targets in a rectangle of size <see cref="MeeleAttacker.SearchRectangleSize"/>,
	/// if it finds a target or is given target explicitly by <see cref="MeeleAttacker.Attack(IEntity)"/>,
	/// pathfinds to the target and if in range, raises <see cref="MeeleAttacker.Attacked"/> event every <see cref="MeeleAttacker.TimeBetweenAttacks"/> seconds
	/// </summary>
    public class MovingMeeleAttacker : MeeleAttacker
    {
		internal class Loader : DefaultComponentLoader{
			public override DefaultComponent Component => MovingMeele;

			public MovingMeeleAttacker MovingMeele { get; private set; }

			int targetID;

			public Loader()
			{

			}

			public static PluginData SaveState(MovingMeeleAttacker movingMeele)
			{
				var writer = new SequentialPluginDataWriter(movingMeele.Level);
				writer.StoreNext(movingMeele.SearchForTarget);
				writer.StoreNext(movingMeele.SearchRectangleSize);
				writer.StoreNext(movingMeele.TimeBetweenSearches);
				writer.StoreNext(movingMeele.timeBetweenPositionChecks);
				writer.StoreNext(movingMeele.TimeBetweenAttacks);
				writer.StoreNext(movingMeele.Enabled);
				writer.StoreNext(movingMeele.Target?.ID ?? 0);

				writer.StoreNext(movingMeele.TimeToNextSearch);
				writer.StoreNext(movingMeele.timeToNextPositionCheck);
				writer.StoreNext(movingMeele.TimeToNextAttack);
				return writer.PluginData;
			}

			public override void StartLoading(LevelManager level, InstancePlugin plugin, PluginData storedData)
			{
				var user = plugin as IUser;
				if (user == null) {
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(IUser)} interface", nameof(plugin));
				}

				var reader = new SequentialPluginDataReader(storedData, level);
				var searchForTarget = reader.GetNext<bool>();
				var targetSearchRectangleSize = reader.GetNext<IntVector2>();
				var timeBetweenSearches = reader.GetNext<float>();
				var timeBetweenPositionChecks = reader.GetNext<float>();
				var timeBetweenAttacks = reader.GetNext<float>();
				
				var enabled = reader.GetNext<bool>();
				targetID = reader.GetNext<int>();

				var timeToNextSearch = reader.GetNext<float>();
				var timeToNextPositionCheck = reader.GetNext<float>();
				var timeToNextAttack = reader.GetNext<float>();

				user.GetMandatoryDelegates(out MoveTo moveTo, out IsInRange isInRange, out PickTarget pickTarget);

				MovingMeele = new MovingMeeleAttacker(level,
													 searchForTarget,
													 targetSearchRectangleSize,
													 timeBetweenSearches,
													 timeBetweenPositionChecks,
													 timeBetweenAttacks,
													 enabled,
													 timeToNextSearch,
													 timeToNextPositionCheck,
													 timeToNextAttack,
													 moveTo,
													 isInRange,
													 pickTarget
													 );

			}

			public override void ConnectReferences(LevelManager level)
			{
				MovingMeele.Target = targetID == 0 ? null : level.GetEntity(targetID);
			}

			public override void FinishLoading()
			{
				
			}

			public override DefaultComponentLoader Clone()
			{
				return new Loader();
			}
		}

		public interface IUser {
			void GetMandatoryDelegates(out MoveTo moveTo, out IsInRange isInRange, out PickTarget pickTarget);
		}

		public static string ComponentName = nameof(MovingMeeleAttacker);
		public static DefaultComponents ComponentID = DefaultComponents.MovingMeele;

		public override string ComponentTypeName => ComponentName;

		public override DefaultComponents ComponentTypeID => ComponentID;

		Vector3 previousTargetPosition;

		MoveTo moveTo;

		float timeBetweenPositionChecks;
		float timeToNextPositionCheck;



		protected MovingMeeleAttacker(ILevelManager level,
									bool searchForTarget,
									IntVector2 searchRectangleSize,
									float timeBetweenSearches,
									float timeBetweenPositionChecks,
									float timeBetweenAttacks,
									bool enabled,
									MoveTo moveTo,
									IsInRange isInRange,
									PickTarget pickTarget
									)
			:base(level,searchForTarget, searchRectangleSize, timeBetweenSearches, timeBetweenAttacks, isInRange, pickTarget)
		{
			this.Enabled = enabled;
			this.moveTo = moveTo;
			this.timeBetweenPositionChecks = timeBetweenPositionChecks;
			this.timeToNextPositionCheck = timeBetweenPositionChecks;

			this.ReceiveSceneUpdates = true;
		}

		protected MovingMeeleAttacker(ILevelManager level,
									bool searchForTarget,
									IntVector2 searchRectangleSize,
									float timeBetweenSearches,
									float timeBetweenPositionChecks,
									float timeBetweenAttacks,
									bool enabled,
									float timeToNextSearch,
									float timeToNextPositionCheck,
									float timeToNextAttack,
									MoveTo moveTo,
									IsInRange isInRange,
									PickTarget pickTarget
		)
			: base(level, 
					searchForTarget,
					searchRectangleSize,
					timeBetweenSearches, 
					timeBetweenAttacks,
					timeToNextSearch,
					timeToNextAttack,
					isInRange,
					pickTarget)
		{
			this.Enabled = enabled;
			this.moveTo = moveTo;
			this.timeBetweenPositionChecks = timeBetweenPositionChecks;
			this.timeToNextPositionCheck = timeToNextPositionCheck;

			this.ReceiveSceneUpdates = true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="instancePlugin"></param>
		/// <param name="level"></param>
		/// <param name="searchForTarget"></param>
		/// <param name="searchRectangleSize">The size of the rectangle with the Entity in the center that will be checked for possible targets</param>
		/// <param name="timeBetweenSearches">Time between searching the rectangle of size <paramref name="searchRectangleSize"/> if <paramref name="searchForTarget"/> is true</param>
		/// <param name="timeBetweenPositionChecks">Time between the attacker checks if the targets position changed and recalculates its path</param>
		/// <param name="timeBetweenAttacks">Time between each attack</param>
		/// <returns></returns>
		public static MovingMeeleAttacker CreateNew<T>(T instancePlugin, 
														ILevelManager level, 
														bool searchForTarget,
														IntVector2 searchRectangleSize,
														float timeBetweenSearches,
														float timeBetweenPositionChecks,
														float timeBetweenAttacks
														)
			where T : InstancePlugin, IUser
		{
			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			((IUser) instancePlugin).GetMandatoryDelegates(out MoveTo moveTo,
															out IsInRange isInRange,
															out PickTarget pickTarget);

			return new MovingMeeleAttacker(level,
											searchForTarget,
											searchRectangleSize,
											timeBetweenSearches,
											timeBetweenPositionChecks,
											timeBetweenAttacks,
											true,
											moveTo,
											isInRange,
											pickTarget);
		}

		public override PluginData SaveState()
		{
			return Loader.SaveState(this);
		}

		protected override void OnUpdateChecked(float timeStep)
		{
			if (Target == null) {
				SearchForTargetInRange(timeStep);
			}

			//If attacker has a target, and the target is out of range
			if (Target != null && !TryAttack(timeStep)) {

				CheckTargetPosition(timeStep);

			}

			
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents)
		{
			base.AddedToEntity(entityDefaultComponents);

			AddedToEntity(typeof(MovingMeeleAttacker), entityDefaultComponents);
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents)
		{
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(MovingMeeleAttacker), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}

		void CheckTargetPosition(float timeStep)
		{
			timeToNextPositionCheck -= timeStep;
			if (timeToNextPositionCheck > 0) return;

			timeToNextPositionCheck = timeBetweenPositionChecks;

			//TODO: Intersect
			if (Target.Position != previousTargetPosition) {
				moveTo(Target.Position);
				previousTargetPosition = Target.Position;
			}
		}
	}
}
