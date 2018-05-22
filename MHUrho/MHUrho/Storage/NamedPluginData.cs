using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;

namespace MHUrho.Storage
{


	public abstract class NamedPluginDataWrapper : PluginDataWrapper {

		public int Count => PluginData.Named.DataMap.Count;

		public bool ContainsKey(string key) {
			return PluginData.Named.DataMap.ContainsKey(key);
		}



		protected NamedPluginDataWrapper(PluginData pluginData, ILevelManager level) : base(pluginData, level) {
			if (pluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Named) {
				throw new ArgumentException("pluginData was not Named");
			}
		}
	}

	public class NamedPluginDataWriter : NamedPluginDataWrapper {

		public void Store<T>(string key, T value) {
			PluginData.Named.DataMap.Add(key, ToDataConvertors[typeof(T)](value, Level));
		}

		public NamedPluginDataWriter(ILevelManager level) : base(new PluginData {Named = new NamedPluginData()}, level) {

		}

		public NamedPluginDataWriter(PluginData pluginData, ILevelManager level) : base(pluginData, level) {

		}
	}
	public class NamedPluginDataReader : NamedPluginDataWrapper {

		public T Get<T>(string key) {
			return (T)FromDataConvertors[typeof(T)](PluginData.Named.DataMap[key], Level);
		}

		public bool TryGetValue<T>(string key, out T value) {
			value = default(T);
			if (PluginData.Named.DataMap.TryGetValue(key, out Data valueData)) {
				value = (T)FromDataConvertors[typeof(T)](valueData, Level);
				return true;
			}

			return false;
		}

		public NamedPluginDataReader(PluginData pluginData, ILevelManager level) : base(pluginData, level) {

		}
	}
}
