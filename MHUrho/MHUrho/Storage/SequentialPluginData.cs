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

		public void GetCurrent<T>(out T value)
		{
			value = GetCurrent<T>();
		}

		/// <summary>
		/// Moves the enumerator to next value and sets <paramref name="value"/> to it's value and returns true,
		/// if there is no next value, returns false and sets Finished to true
		/// </summary>
		/// <param name="value">The loaded value (true) or default(<typeparamref name="T"/>) (false).</param>
		/// <typeparam name="T">Type of the stored value.</typeparam>
		/// <returns>True if the <paramref name="value"/> is valid, false if there is nothing more to read.</returns>
		public bool GetNext<T>(out T value)
		{
			Finished = !MoveNext();
			if (Finished) {
				value = default(T);
				return false;
			}
			value = GetCurrent<T>();
			return true;
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
