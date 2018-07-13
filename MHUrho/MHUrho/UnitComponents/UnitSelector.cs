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
	public delegate void UnitSelectedDelegate(UnitSelector unitSelector);
	public delegate void UnitDeselectedDelegate(UnitSelector unitSelector);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="selector"></param>
	/// <param name="order">Contains an Executed flag, which is true if some method before consumed the command, and false if it did not
	/// Should be set to true if you were able to execute the command, and leave the previous value if not</param>
	public delegate void UnitOrderedDelegate(UnitSelector unitSelector, Order order);



	public class UnitSelector : Selector {

		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => UnitSelector;

			public UnitSelector UnitSelector { get; private set; }

			public Loader() {

			}

			public static PluginData SaveState(UnitSelector unitSelector) {
				var sequentialData = new SequentialPluginDataWriter(unitSelector.Level);
				sequentialData.StoreNext<bool>(unitSelector.Enabled);
				return sequentialData.PluginData;
			}

			public override void StartLoading(LevelManager level, InstancePlugin plugin, PluginData storedData) {
				
				var dataReader = new SequentialPluginDataReader(storedData, level);

				UnitSelector = new UnitSelector(level)
								{
									Enabled = dataReader.GetNext<bool>()
								};

			}

			public override void ConnectReferences(LevelManager level) {

			}

			public override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone() {
				return new Loader();
			}
		}

		public static string ComponentName = nameof(UnitSelector);
		public static DefaultComponents ComponentID = DefaultComponents.UnitSelector;

		public override string ComponentTypeName => ComponentName;
		public override DefaultComponents ComponentTypeID => ComponentID;

		public IUnit Unit => (IUnit)Entity;

		public event UnitSelectedDelegate UnitSelected;
		public event UnitDeselectedDelegate UnitDeselected;
		
		
		public event UnitOrderedDelegate Ordered;


		protected UnitSelector(ILevelManager level) 
			:base(level)
		{

		}

		public static UnitSelector CreateNew(ILevelManager level)
		{
			return new UnitSelector(level);
		}

		/// <summary>
		/// Orders this selected unit with target <paramref name="targetTile"/>
		/// 
		/// if the unit can do anything, returns true,the order is given and the unit will procede
		/// if the unit cant do anything, returns false
		/// </summary>
		/// <param name="targetTile">target tile</param>
		/// <returns>True if unit was given order, False if there is nothing the unit can do</returns>
		public override bool Order(Order order) {
			//TODO: EventArgs to get if the event was handled
			Ordered?.Invoke(this, order);
			return order.Executed;
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
