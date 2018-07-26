using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.UnitComponents
{
    public class StaticMeeleAttacker : MeeleAttacker
    {
		internal class Loader : DefaultComponentLoader {
			public override DefaultComponent Component => StaticMeele;

			public StaticMeeleAttacker StaticMeele { get; private set; }

			int targetID;

			public Loader()
			{

			}

			public static PluginData SaveState(StaticMeeleAttacker staticMeele)
			{
				var writer = new SequentialPluginDataWriter(staticMeele.Level);
				writer.StoreNext(staticMeele.SearchForTarget);
				writer.StoreNext(staticMeele.SearchRectangleSize);
				writer.StoreNext(staticMeele.TimeBetweenSearches);
				writer.StoreNext(staticMeele.TimeBetweenAttacks);
				
				writer.StoreNext(staticMeele.Enabled);
				writer.StoreNext(staticMeele.Target.ID);
				writer.StoreNext(staticMeele.TimeToNextSearch);
				writer.StoreNext(staticMeele.TimeToNextAttack);
				return writer.PluginData;
			}

			public override void StartLoading(LevelManager level, InstancePlugin plugin, PluginData storedData)
			{
				var user = plugin as IUser;
				if (user == null) {
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(MovingMeeleAttacker.IUser)} interface", nameof(plugin));
				}

				var reader = new SequentialPluginDataReader(storedData, level);
				var searchForTarget = reader.GetNext<bool>();
				var searchRectangleSize = reader.GetNext<IntVector2>();
				var timeBetweenSearches = reader.GetNext<float>();
				var timeBetweenAttacks = reader.GetNext<float>();
				
				var enabled = reader.GetNext<bool>();
				targetID = reader.GetNext<int>();
				var timeToNextSearch = reader.GetNext<float>();
				var timeToNextAttack = reader.GetNext<float>();

				user.GetMandatoryDelegates(out IsInRange isInRange, out PickTarget pickTarget);

				StaticMeele = new StaticMeeleAttacker(level,
													searchForTarget,
													searchRectangleSize,
													timeBetweenSearches,
													timeBetweenAttacks,
													enabled,
													timeToNextSearch,
													timeToNextAttack,
													isInRange,
													pickTarget);
			}

			public override void ConnectReferences(LevelManager level)
			{
				StaticMeele.Target = targetID == 0 ? null : level.GetEntity(targetID);
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
			void GetMandatoryDelegates(out IsInRange isInRange, out PickTarget pickTarget);
		}

		public static string ComponentName = nameof(StaticMeeleAttacker);
		public static DefaultComponents ComponentID = DefaultComponents.StaticMeele;

		public override string ComponentTypeName => ComponentName;

		public override DefaultComponents ComponentTypeID => ComponentID;


		protected StaticMeeleAttacker(ILevelManager level,
									bool searchForTarget,
									IntVector2 searchRectangleSize,
									float timeBetweenSearches,
									float timeBetweenAttacks,
									IsInRange isInRange, 
									PickTarget pickTarget)
			:base(level, searchForTarget, searchRectangleSize, timeBetweenSearches, timeBetweenAttacks,  isInRange, pickTarget)	
		{
			this.ReceiveSceneUpdates = true;
		}

		protected StaticMeeleAttacker(ILevelManager level,
									bool searchForTarget,
									IntVector2 searchRectangleSize,
									float timeBetweenSearches,
									float timeBetweenAttacks,
									bool enabled,
									float timeToNextSearch,
									float timeToNextAttack,
									IsInRange isInRange,
									PickTarget pickTarget)
			:base(level, searchForTarget, searchRectangleSize, timeBetweenSearches, timeBetweenAttacks, timeToNextSearch, timeToNextAttack,  isInRange, pickTarget)
		{
			this.Enabled = enabled;

			this.ReceiveSceneUpdates = true;
		}

		public static StaticMeeleAttacker CreateNew<T>(T instancePlugin,
														ILevelManager level,
														bool searchForTarget, 
														IntVector2 searchRectangleSize,
														float timeBetweenSearches,
														float timeBetweenAttacks
														)
			where T : InstancePlugin, IUser
		{
			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			((IUser)instancePlugin).GetMandatoryDelegates(out IsInRange isInRange, out PickTarget pickTarget);

			return new StaticMeeleAttacker(level, searchForTarget, searchRectangleSize, timeBetweenSearches, timeBetweenAttacks, isInRange, pickTarget);
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

			if (Target != null) {
				TryAttack(timeStep);
			}
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents)
		{
			base.AddedToEntity(entityDefaultComponents);

			AddedToEntity(typeof(StaticMeeleAttacker), entityDefaultComponents);
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents)
		{
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(StaticMeeleAttacker), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}
	}
}
