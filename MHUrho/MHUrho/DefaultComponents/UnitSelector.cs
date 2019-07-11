using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.DefaultComponents
{
	public delegate void UnitSelectedDelegate(UnitSelector unitSelector);
	public delegate void UnitDeselectedDelegate(UnitSelector unitSelector);

	public class UnitSelector : Selector {

		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => UnitSelector;

			public UnitSelector UnitSelector { get; private set; }

			readonly LevelManager level;
			readonly InstancePlugin plugin;
			readonly StDefaultComponent storedData;

			public Loader() {

			}

			protected Loader(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData)
			{
				this.level = level;
				this.plugin = plugin;
				this.storedData = storedData;
			}

			public static StDefaultComponent SaveState(UnitSelector unitSelector)
			{
				var storedUnitSelector = new StUnitSelector
										{
											Enabled = unitSelector.Enabled
										};
				return new StDefaultComponent {UnitSelector = storedUnitSelector};
			}

			public override void StartLoading() {

				var user = plugin as IUser;
				if (user == null)
				{
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(IUser)} interface", nameof(plugin));
				}

				if (storedData.ComponentCase != StDefaultComponent.ComponentOneofCase.UnitSelector) {
					throw new ArgumentException("Invalid component type data passed to loader", nameof(storedData));
				}

				var storedUnitSelector = storedData.UnitSelector;

				UnitSelector = new UnitSelector(user, level)
								{
									Enabled = storedUnitSelector.Enabled
								};

			}

			public override void ConnectReferences() {

			}

			public override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData)
			{
				return new Loader(level, plugin, storedData);
			}
		}

		public interface IUser {
			/// <summary>
			/// Executes given <paramref name="order"/>, returns true if executed successfully, false otherwise.
			/// </summary>
			/// <param name="order">Order to execute.</param>
			/// <returns>True if executed successfully, false otherwise</returns>
			bool ExecuteOrder(Order order);
		}

		public IUnit Unit => (IUnit)Entity;

		/// <summary>
		/// Invoked on unit selection
		/// </summary>
		public event UnitSelectedDelegate UnitSelected;

		/// <summary>
		/// Invoked on unit deselection
		/// </summary>
		public event UnitDeselectedDelegate UnitDeselected;

		IUser user;

		protected UnitSelector(IUser user, ILevelManager level) 
			:base(level)
		{
			this.user = user;
		}

		public static UnitSelector CreateNew<T>(T plugin, ILevelManager level)
			where T: UnitInstancePlugin, IUser
		{
			if (plugin == null) {
				throw new ArgumentNullException(nameof(plugin));
			}

			var newInstance = new UnitSelector(plugin, level);
			plugin.Entity.AddComponent(newInstance);
			return newInstance;
		}

		/// <summary>
		/// Issues an <paramref name="order"/> to the unit.
		/// If the order was executed, returns true, otherwise false.
		/// </summary>
		/// <param name="order">Order to execute.</param>
		/// <returns>True if unit executed given order, False if there is nothing the unit can do</returns>
		public override bool Order(Order order) {
			try {
				return user.ExecuteOrder(order);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Error,
								$"There was an unexpected exception in {nameof(user.ExecuteOrder)}: {e.Message}");
				return false;
			}
		}


		public override void Select() {
			Selected = true;
			try {
				UnitSelected?.Invoke(this);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(UnitSelected)}: {e.Message}");
			}
		}

		public override void Deselect() {
			Selected = false;
			try
			{
				UnitDeselected?.Invoke(this);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(UnitDeselected)}: {e.Message}");
			}
		}


		public override StDefaultComponent SaveState()
		{
			return Loader.SaveState(this);
		}



		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.AddedToEntity(entityDefaultComponents);

			if (Entity == null || !(Entity is IUnit)) {
				throw new
					InvalidOperationException($"Cannot attach {nameof(UnitSelector)} to a node that does not have {nameof(Logic.IUnit)} component");
			}


			AddedToEntity(typeof(UnitSelector), entityDefaultComponents);
			Entity.OnRemoval += (_) => Deselect();
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(UnitSelector), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}


	}
}
