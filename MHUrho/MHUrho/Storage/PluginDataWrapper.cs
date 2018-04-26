using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.WorldMap;

namespace MHUrho.Storage
{
	public class PluginDataWrapper {
		/// <summary>
		/// This field is internal because i dont want people writing plugins to go around
		/// the wrappers and change things inside the pluginData itself
		/// </summary>
		internal PluginData PluginData { get; private set; }

		public PluginDataWrapper(PluginData pluginData) {
			this.PluginData = pluginData;
		}

		public bool CanStoreAndLoad(Type t) {
			return FromDataConvertors.ContainsKey(t) && ToDataConvertors.ContainsKey(t);
		}

		public bool CanStoreAndLoad<T>() {
			return FromDataConvertors.ContainsKey(typeof(T)) && ToDataConvertors.ContainsKey(typeof(T));
		}

		public PluginData.DataStorageTypesOneofCase GetWrappedDataType() {
			return PluginData.DataStorageTypesCase;
		}

		/// <summary>
		/// Creates a reader for wrapped data in Sequential format,
		/// written by <see cref="SequentialPluginDataWriter"/>
		/// </summary>
		/// <returns>Rreader for data in sequential format</returns>
		public SequentialPluginDataReader GetReaderForWrappedSequentialData() {
			if (PluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Sequential) {
				throw new
					InvalidOperationException("Cannot get SequentialReader for data that are not stored in sequential format");
			}
			return new SequentialPluginDataReader(PluginData);
		}
		/// <summary>
		/// Creates a writer that stores data in sequential format,
		/// the written data then can be read by <see cref="SequentialPluginDataReader"/>
		/// </summary>
		/// <returns>Writer that writes data in sequential format</returns>
		public SequentialPluginDataWriter GetWriterForWrappedSequentialData() {
			if (PluginData.DataStorageTypesCase == PluginData.DataStorageTypesOneofCase.None) {
				PluginData.Sequential = new SequentialPluginData();
			} else if (PluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Sequential) {
				throw new InvalidOperationException("Cannot get sequentialWriter for data that are not sequential");
			}
			return new SequentialPluginDataWriter(PluginData);
		}
		/// <summary>
		/// Creates a reader for wrapped data in Indexed format
		/// written by <see cref="IndexedPluginDataWriter"/>
		/// </summary>
		/// <returns></returns>
		public IndexedPluginDataReader GetReaderForWrappedIndexedData() {
			if (PluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Indexed) {
				throw new
					InvalidOperationException("Cannot get IndexedReader for data that are not stored in indexed format");
			}
			return new IndexedPluginDataReader(PluginData);
		}
		/// <summary>
		/// Creates a writer that writes data in Indexed format,
		/// the written data can then be read by <see cref="IndexedPluginDataReader"/>
		/// </summary>
		/// <returns></returns>
		public IndexedPluginDataWriter GetWriterForWrappedIndexedData() {
			if (PluginData.DataStorageTypesCase == PluginData.DataStorageTypesOneofCase.None) {
				PluginData.Indexed = new IndexedPluginData();
			}
			else if (PluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Indexed) {
				throw new InvalidOperationException("Cannot get IndexedWriter for data that are not Indexed");
			}
			return new IndexedPluginDataWriter(PluginData);
		}
		/// <summary>
		/// Creates a reader for wrapped data in Named format
		/// written by <see cref="NamedPluginDataWriter"/>
		/// </summary>
		/// <returns></returns>
		public NamedPluginDataReader GetReaderForWrappedNamedData() {
			if (PluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Named) {
				throw new
					InvalidOperationException("Cannot get NamedReader for data that are not stored in named format");
			}
			return new NamedPluginDataReader(PluginData);
		}
		/// <summary>
		/// Creates a writer that writes data in Named format,
		/// the written data can then be read by <see cref="NamedPluginDataReader"/>
		/// </summary>
		/// <returns></returns>
		public NamedPluginDataWriter GetWriterForWrappedNamedData() {
			if (PluginData.DataStorageTypesCase == PluginData.DataStorageTypesOneofCase.None) {
				PluginData.Named = new NamedPluginData();
			}
			else if (PluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Named) {
				throw new InvalidOperationException("Cannot get NamedWriter for data that are not Named");
			}
			return new NamedPluginDataWriter(PluginData);
		}

		protected static Dictionary<Type, Func<Data, object>> FromDataConvertors 
			= new Dictionary<Type, Func<Data, object>> 
			{
				//TODO: Type checks
				{ typeof(float),(data) => data.Float },
				{ typeof(double),(data) => data.Double },
				{ typeof(int), (data) => data.Int },
				{ typeof(long), (data) => data.Long },
				{ typeof(uint), (data) => data.Uint },
				{ typeof(ulong), (data) => data.Ulong },
				{ typeof(bool), (data) => data.Bool },
				{ typeof(string), (data) => data.String },
				{ typeof(Google.Protobuf.ByteString), (data) => data.ByteArray },
				{ typeof(IntVector2), (data) => data.IntVector2.ToIntVector2() },
				{ typeof(IntVector3), (data) => data.IntVector3.ToIntVector3() },
				{ typeof(Vector2), (data) => data.Vector2.ToVector2() },
				{ typeof(Vector3), (data) => data.Vector3.ToVector3() },
				//TODO: Lists
				{ typeof(Path), (data) => Path.Load(data.Path) }
				
			};

		protected static Dictionary<Type, Func<object, Data>> ToDataConvertors 
			= new Dictionary<Type, Func<object, Data>> 
			{
				//TODO: Type checks
				{ typeof(float),(o) => new Data{Float = (float)o} },
				{ typeof(double),(o) => new Data{Double = (double)o } },
				{ typeof(int), (o) => new Data{Int = (int)o } },
				{ typeof(long), (o) => new Data{Long = (long)o } },
				{ typeof(uint), (o) => new Data{Uint = (uint)o } },
				{ typeof(ulong), (o) => new Data{Ulong = (ulong)o } },
				{ typeof(bool), (o) => new Data{Bool = (bool)o } },
				{ typeof(string), (o) => new Data{String = (string)o } },
				{ typeof(Google.Protobuf.ByteString), (o) => new Data{ByteArray = (Google.Protobuf.ByteString)o } },
				{ typeof(IntVector2), (o) => new Data{ IntVector2 = ((IntVector2)o).ToStIntVector2()} },
				{ typeof(IntVector3), (o) => new Data{ IntVector3 = ((IntVector3)o).ToStIntVector3() } },
				{ typeof(Vector2), (o) => new Data{ Vector2 = ((Vector2)o).ToStVector2()} },
				{ typeof(Vector3), (o) => new Data{ Vector3 = ((Vector3)o).ToStVector3()} },
				//TODO: Lists
				{ typeof(Path), (o) => new Data{ Path = ((Path)o).Save() } }
				
			};
	}
}
