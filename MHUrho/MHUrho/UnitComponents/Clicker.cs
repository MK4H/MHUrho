using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
    public class Clicker : DefaultComponent
    {
		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => Clicker;

			public Clicker Clicker { get; private set; }

			public Loader() {

			}

			public static PluginData SaveState(Clicker clicker) {
				var storageData = new SequentialPluginDataWriter();
				return storageData.PluginData;
			}

			public override void StartLoading(LevelManager level, InstancePluginBase plugin, PluginData storedData) {

				var notificationReceiver = plugin as INotificationReceiver;
				if (notificationReceiver == null) {
					throw new
						ArgumentException($"provided plugin does not implement the {nameof(INotificationReceiver)} interface", nameof(plugin));
				}

				Clicker = new Clicker(notificationReceiver);
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
			void Clicked(Clicker clicker, int button, int qualifiers);
		}

		public static string ComponentName = nameof(Clicker);
		public static DefaultComponents ComponentID = DefaultComponents.Clicker;

		public override string ComponentTypeName => ComponentName;
		public override DefaultComponents ComponentTypeID => ComponentID;

		private INotificationReceiver notificationReceiver;

		protected Clicker(INotificationReceiver notificationReceiver) {
			this.notificationReceiver = notificationReceiver;
		}

		public static Clicker CreateNew<T>(T instancePlugin, ILevelManager level) 
			where T: InstancePluginBase, INotificationReceiver
		{
			if (instancePlugin == null) {
				throw new ArgumentNullException(nameof(instancePlugin));
			}

			return new Clicker(instancePlugin);
		}


		public override PluginData SaveState()
		{
			return Loader.SaveState(this);
		}

		public void Click(int button, int qualifiers) {
			notificationReceiver.Clicked(this, button, qualifiers);
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			AddedToEntity(typeof(Clicker), entityDefaultComponents);

		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			return RemovedFromEntity(typeof(Clicker), entityDefaultComponents);
		}

	}
}
