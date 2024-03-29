﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace MHUrho.DefaultComponents
{
    public class StaticMeeleAttacker : MeeleAttacker
    {
		internal class Loader : DefaultComponentLoader {
			public override DefaultComponent Component => StaticMeele;

			public StaticMeeleAttacker StaticMeele { get; private set; }


			readonly LevelManager level;
			readonly InstancePlugin plugin;
			readonly StDefaultComponent storedData;

			int targetID;

			public Loader()
			{

			}

			protected Loader(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData)
			{
				this.level = level;
				this.plugin = plugin;
				this.storedData = storedData;
			}

			public static StDefaultComponent SaveState(StaticMeeleAttacker staticMeele)
			{
				var storedStaticMeeleAttacker = new StStaticMeeleAttacker
												{
													Enabled = staticMeele.Enabled,
													SearchForTarget = staticMeele.SearchForTarget,
													SearchRectangleSize = staticMeele.SearchRectangleSize.ToStIntVector2(),
													TimeBetweenAttacks = staticMeele.TimeBetweenAttacks,
													TimeBetweenSearches = staticMeele.TimeBetweenSearches,
													TimeToNextAttack = staticMeele.TimeToNextAttack,
													TimeToNextSearch = staticMeele.TimeToNextSearch,
													TargetID = staticMeele.Target?.ID ?? 0
												};

				return new StDefaultComponent {StaticMeeleAttacker = storedStaticMeeleAttacker};
			}

			public override void StartLoading()
			{
				var user = plugin as IUser;
				if (user == null) {
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(MovingMeeleAttacker.IUser)} interface", nameof(plugin));
				}

				if (storedData.ComponentCase != StDefaultComponent.ComponentOneofCase.StaticMeeleAttacker) {
					throw new ArgumentException("Invalid component type data passed to loader", nameof(storedData));
				}

				var storedStaticMeeleAttacker = storedData.StaticMeeleAttacker;

				StaticMeele = new StaticMeeleAttacker(level,
													storedStaticMeeleAttacker.SearchForTarget,
													storedStaticMeeleAttacker.SearchRectangleSize.ToIntVector2(),
													storedStaticMeeleAttacker.TimeBetweenSearches,
													storedStaticMeeleAttacker.TimeBetweenAttacks,
													storedStaticMeeleAttacker.Enabled,
													storedStaticMeeleAttacker.TimeToNextSearch,
													storedStaticMeeleAttacker.TimeToNextAttack,
													user);

				targetID = storedStaticMeeleAttacker.TargetID;
			}

			public override void ConnectReferences()
			{
				StaticMeele.Target = targetID == 0 ? null : level.GetEntity(targetID);
			}

			public override void FinishLoading()
			{
				
			}

			public override DefaultComponentLoader Clone(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData)
			{
				return new Loader(level, plugin, storedData);
			}
		}

		public interface IUser : IBaseUser {
			
		}

		protected StaticMeeleAttacker(ILevelManager level,
									bool searchForTarget,
									IntVector2 searchRectangleSize,
									float timeBetweenSearches,
									float timeBetweenAttacks,
									IUser user)
			:base(level, searchForTarget, searchRectangleSize, timeBetweenSearches, timeBetweenAttacks, user)	
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
									IUser user)
			:base(level, searchForTarget, searchRectangleSize, timeBetweenSearches, timeBetweenAttacks, timeToNextSearch, timeToNextAttack, user)
		{
			this.Enabled = enabled;

			this.ReceiveSceneUpdates = true;
		}

		public static StaticMeeleAttacker CreateNew<T>(T plugin,
														ILevelManager level,
														bool searchForTarget, 
														IntVector2 searchRectangleSize,
														float timeBetweenSearches,
														float timeBetweenAttacks
														)
			where T : EntityInstancePlugin, IUser
		{
			if (plugin == null) {
				throw new ArgumentNullException(nameof(plugin));
			}

			var newInstance =  new StaticMeeleAttacker(level,
															searchForTarget,
															searchRectangleSize,
															timeBetweenSearches,
															timeBetweenAttacks,
															plugin);

			plugin.Entity.AddComponent(newInstance);
			return newInstance;
		}

		public override StDefaultComponent SaveState()
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
