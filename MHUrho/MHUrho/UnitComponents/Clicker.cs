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

		internal static Clicker Load(ILevelManager level, InstancePluginBase plugin, PluginData data) {
			var notificationReceiver = plugin as INotificationReceiver;
			if (notificationReceiver == null) {
				throw new
					ArgumentException($"provided plugin does not implement the {nameof(INotificationReceiver)} interface", nameof(plugin));
			}

			return new Clicker(notificationReceiver);
		}

		internal override void ConnectReferences(ILevelManager level) {
			//NOTHING
		}

		public override PluginData SaveState() {
			var storageData = new SequentialPluginDataWriter();
			return storageData.PluginData;
		}

		public void Click(int button, int qualifiers) {
			notificationReceiver.Clicked(this, button, qualifiers);
		}

		
	}
}
