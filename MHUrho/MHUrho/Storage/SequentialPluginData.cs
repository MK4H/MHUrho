using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;


namespace MHUrho.Storage
{
	public abstract class SequentialPluginDataWrapper : PluginDataWrapper {

		public int Count => PluginData.Sequential.Data.Count;

		protected SequentialPluginDataWrapper(PluginData pluginData, ILevelManager level) : base(pluginData, level) {
			if (pluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Sequential) {
				throw new ArgumentException("pluginData was not of type Sequential");
			}
		}
	}

	public class SequentialPluginDataWriter : SequentialPluginDataWrapper {

		public void StoreNext<T>(T value) {
			PluginData.Sequential.Data.Add(ToDataConvertors[typeof(T)](value, Level));
		}

		public SequentialPluginDataWriter(ILevelManager level) : base(new PluginData { Sequential = new SequentialPluginData() }, level) {

		}

		public SequentialPluginDataWriter(PluginData pluginData, ILevelManager level) : base(pluginData, level) {
			if (pluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Sequential) {
				throw new ArgumentException("pluginData was not of type Sequential");
			}
		}

	}

	public class SequentialPluginDataReader : SequentialPluginDataWrapper {

		public bool Finished { get; private set; }

		IEnumerator<Data> dataEnumerator;

		public bool MoveNext() {
			return dataEnumerator.MoveNext();
		}

		public T GetCurrent<T>() {
			return(T) FromDataConvertors[typeof(T)](dataEnumerator.Current, Level);
		}

		/// <summary>
		/// Moves the enumerator to next value and returns it,
		/// if there is no next value, returns default(T) and sets Finished to true
		/// </summary>
		/// <typeparam name="T">Type of the stored value</typeparam>
		/// <returns>Next stored value, or default(T) if <see cref="Finished"/> is true</returns>
		public T GetNext<T>()
		{
			Finished = !MoveNext();
			return Finished ? default(T) : GetCurrent<T>();
		}

		public void Reset() {
			this.dataEnumerator = PluginData.Sequential.Data.GetEnumerator();
		}

		public SequentialPluginDataReader(PluginData pluginData, ILevelManager level) : base(pluginData, level) {
			if (pluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Sequential) {
				throw new ArgumentException("pluginData was not of type Sequential");
			}

			this.dataEnumerator = pluginData.Sequential.Data.GetEnumerator();
		}

	}
}
