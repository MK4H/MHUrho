using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;

namespace MHUrho.Storage
{
	public abstract class IndexedPluginDataWrapper : PluginDataWrapper {
		public int Count => PluginData.Indexed.DataMap.Count;

		public bool ContainsIndex(int index) {
			return PluginData.Indexed.DataMap.ContainsKey(index);
		}

		protected IndexedPluginDataWrapper(PluginData pluginData, ILevelManager level) : base(pluginData, level) {
			if (pluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Indexed) {
				throw new ArgumentException("pluginData was not Indexed");
			}
		}
	}

	public class IndexedPluginDataWriter : IndexedPluginDataWrapper {

		

		public void Store<T>(int index, T value) {
			PluginData.Indexed.DataMap.Add(index, ToDataConvertors[typeof(T)](value, Level));
		}

		public IndexedPluginDataWriter(PluginData pluginData, ILevelManager level) : base(pluginData, level) {
			if (pluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Indexed) {
				throw new ArgumentException("pluginData was not Indexed");
			}
		}

		public IndexedPluginDataWriter(ILevelManager level) : base(new PluginData{ Indexed = new IndexedPluginData()}, level) {

		}
	}

	public class IndexedPluginDataReader : IndexedPluginDataWrapper {

		public T Get<T>(int index) {
			return (T) FromDataConvertors[typeof(T)](PluginData.Indexed.DataMap[index], Level);
		}

		public bool TryGetValue<T>(int index, out T value) {
			value = default(T);
			if (PluginData.Indexed.DataMap.TryGetValue(index, out Data dataValue)) {
				value = (T)FromDataConvertors[typeof(T)](dataValue, Level);
				return true;
			}
			return false;
		}

		public IndexedPluginDataReader(PluginData pluginData, ILevelManager level) : base(pluginData, level) {
			if (pluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Indexed) {
				throw new ArgumentException("pluginData was not Indexed");
			}
		}
	}
}
