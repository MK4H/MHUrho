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

namespace MHUrho.UnitComponents
{
	internal delegate void UnitSelectedDelegate(UnitSelector unitSelector);
	internal delegate void UnitDeselectedDelegate(UnitSelector unitSelector);
	internal delegate void UnitOrderedToTileDelegate(UnitSelector unitSelector, ITile targetTile, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs);
	internal delegate void UnitOrderedToUnitDelegate(UnitSelector unitSelector, IUnit targetUnit, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs);
	internal delegate void UnitOrderedToBuildingDelegate(UnitSelector unitSelector, IBuilding targetBuilding, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs);


	public class UnitSelector : Selector {

		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => UnitSelector;

			public UnitSelector UnitSelector { get; private set; }

			public Loader() {

			}

			public static PluginData SaveState(UnitSelector unitSelector) {
				var sequentialData = new SequentialPluginDataWriter();
				return sequentialData.PluginData;
			}

			public override void StartLoading(LevelManager level, InstancePlugin plugin, PluginData storedData) {
				var notificationReceiver = plugin as INotificationReceiver;
				if (notificationReceiver == null) {
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(INotificationReceiver)} interface", nameof(plugin));
				}

				UnitSelector = new UnitSelector(notificationReceiver, level);

			}

			public override void ConnectReferences(LevelManager level) {

			}

			public override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone() {
				return new Loader();
			}
		}

		public interface INotificationReceiver {
			void OnUnitSelected(UnitSelector selector);

			void OnUnitDeselected(UnitSelector selector);

			/// <summary>
			/// 
			/// </summary>
			/// <param name="selector"></param>
			/// <param name="targetTile"></param>
			/// <param name="orderArgs">Contains an Executed flag, which is true if some method before consumed the command, and false if it did not
			/// Should be set to true if you were able to execute the command, and leave the previous value if not</param>
			void OnUnitOrderedToTile(UnitSelector selector, ITile targetTile, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs);

			void OnUnitOrderedToUnit(UnitSelector selector, IUnit targetUnit, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs);

			void OnUnitOrderedToBuilding(UnitSelector selector, IBuilding targetBuilding, MouseButton button, MouseButton buttons, int qualifiers, OrderArgs orderArgs);
		}

		public static string ComponentName = nameof(UnitSelector);
		public static DefaultComponents ComponentID = DefaultComponents.UnitSelector;

		public override string ComponentTypeName => ComponentName;
		public override DefaultComponents ComponentTypeID => ComponentID;

		public IUnit Unit => (IUnit)Entity;

		internal event UnitSelectedDelegate UnitSelected;
		internal event UnitDeselectedDelegate UnitDeselected;
		internal event UnitOrderedToTileDelegate OrderedToTile;
		internal event UnitOrderedToUnitDelegate OrderedToUnit;
		internal event UnitOrderedToBuildingDelegate OrderedToBuilding;


		readonly INotificationReceiver notificationReceiver;


		protected UnitSelector(INotificationReceiver notificationReceiver,ILevelManager level) 
			:base(level)
		{
			this.notificationReceiver = notificationReceiver;

			UnitSelected += notificationReceiver.OnUnitSelected;
			UnitDeselected += notificationReceiver.OnUnitDeselected;
			OrderedToTile += notificationReceiver.OnUnitOrderedToTile;
			OrderedToUnit += notificationReceiver.OnUnitOrderedToUnit;
			OrderedToBuilding += notificationReceiver.OnUnitOrderedToBuilding;
		}

		public static UnitSelector CreateNew<T>(T instancePlugin, ILevelManager level)
			where T: UnitInstancePlugin, INotificationReceiver {

			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			return new UnitSelector(instancePlugin, level);
		}

		/// <summary>
		/// Orders this selected unit with target <paramref name="targetTile"/>
		/// 
		/// if the unit can do anything, returns true,the order is given and the unit will procede
		/// if the unit cant do anything, returns false
		/// </summary>
		/// <param name="targetTile">target tile</param>
		/// <returns>True if unit was given order, False if there is nothing the unit can do</returns>
		public override bool Order(ITile targetTile, MouseButton button, MouseButton buttons, int qualifiers) {
			//TODO: EventArgs to get if the event was handled
			var orderArgs = new OrderArgs();
			OrderedToTile?.Invoke(this, targetTile, button, buttons, qualifiers, orderArgs);
			return orderArgs.Executed;
		}

		public override bool Order(IUnit targetUnit, MouseButton button, MouseButton buttons, int qualifiers) {
			var orderArgs = new OrderArgs();
			OrderedToUnit?.Invoke(this, targetUnit, button, buttons, qualifiers, orderArgs);
			return orderArgs.Executed;
		}

		public override bool Order(IBuilding targetBuilding, MouseButton button, MouseButton buttons, int qualifiers) {
			var orderArgs = new OrderArgs();
			OrderedToBuilding?.Invoke(this, targetBuilding, button, buttons, qualifiers, orderArgs);
			return orderArgs.Executed;
		}

		public override void Select() {
			Selected = true;
			UnitSelected?.Invoke(this);
		}

		public override void Deselect() {
			Selected = false;
			UnitDeselected?.Invoke(this);
		}


		public override PluginData SaveState()
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
			Entity.OnRemoval += Deselect;
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(UnitSelector), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}


	}
}
