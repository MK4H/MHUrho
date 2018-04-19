using System;
using System.Collections.Generic;
using System.Text;


namespace MHUrho.Storage
{
	public abstract class SequentialPluginDataWrapper : PluginDataWrapper {

		public int Count => PluginData.Sequential.Data.Count;

		protected SequentialPluginDataWrapper(PluginData pluginData) : base(pluginData) {
			if (pluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Sequential) {
				throw new ArgumentException("pluginData was not Sequential");
			}
		}
	}

	public class SequentialPluginDataWriter : SequentialPluginDataWrapper {

		public void StoreNext<T>(T value) {
			PluginData.Sequential.Data.Add(ToDataConvertors[typeof(T)](value));
		}

		public SequentialPluginDataWriter() : base(new PluginData { Sequential = new SequentialPluginData() }) {

		}

		public SequentialPluginDataWriter(PluginData pluginData) : base(pluginData) {

		}

	}

	public class SequentialPluginDataReader : SequentialPluginDataWrapper {

		private IEnumerator<Data> dataEnumerator;

		public bool MoveNext() {
			return dataEnumerator.MoveNext();
		}

		public T GetCurrent<T>() {
			return(T) FromDataConvertors[typeof(T)](dataEnumerator.Current);
		}

		public void Reset() {
			this.dataEnumerator = PluginData.Sequential.Data.GetEnumerator();
		}

		public SequentialPluginDataReader(PluginData pluginData) : base(pluginData) {
			this.dataEnumerator = pluginData.Sequential.Data.GetEnumerator();
		}

	}
}
