using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Helpers;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.WorldMap;

namespace MHUrho.Storage
{
	public class PluginDataWrapper {
		/// <summary>
		/// This field is internal because i dont want people writing plugins to go around
		/// the wrappers and change things inside the pluginData itself
		/// </summary>
		internal PluginData PluginData { get; private set; }

		protected ILevelManager Level;

		public PluginDataWrapper(PluginData pluginData, ILevelManager level) {
			this.PluginData = pluginData;
			this.Level = level;
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
			return new SequentialPluginDataReader(PluginData, Level);
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
			return new SequentialPluginDataWriter(PluginData, Level);
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
			return new IndexedPluginDataReader(PluginData, Level);
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
			return new IndexedPluginDataWriter(PluginData, Level);
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
			return new NamedPluginDataReader(PluginData, Level);
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
			return new NamedPluginDataWriter(PluginData, Level);
		}

		protected static Dictionary<Type, Func<Data, ILevelManager, object>> FromDataConvertors 
			= new Dictionary<Type, Func<Data, ILevelManager, object>> 
			{
				//TODO: Type checks
				{ typeof(float),(data,level) => data.Float },
				{ typeof(double),(data, level) => data.Double },
				{ typeof(int), (data, level) => data.Int },
				{ typeof(long), (data, level) => data.Long },
				{ typeof(uint), (data, level) => data.Uint },
				{ typeof(ulong), (data, level) => data.Ulong },
				{ typeof(bool), (data, level) => data.Bool },
				{ typeof(string), (data, level) => data.String },
				{ typeof(Google.Protobuf.ByteString), (data, level) => data.ByteArray },
				{ typeof(IntVector2), (data, level) => data.IntVector2.ToIntVector2() },
				{ typeof(IntVector3), (data, level) => data.IntVector3.ToIntVector3() },
				{ typeof(Vector2), (data, level) => data.Vector2.ToVector2() },
				{ typeof(Vector3), (data, level) => data.Vector3.ToVector3() },
				//TODO: Lists
				{ typeof(Path), (data, level) => Path.Load(data.Path, level) }
				
			};

		protected static Dictionary<Type, Func<object, ILevelManager, Data>> ToDataConvertors 
			= new Dictionary<Type, Func<object, ILevelManager, Data>> 
			{
				//TODO: Type checks
				{ typeof(float),(o, level) => new Data{Float = (float)o} },
				{ typeof(double),(o, level) => new Data{Double = (double)o } },
				{ typeof(int), (o, level) => new Data{Int = (int)o } },
				{ typeof(long), (o, level) => new Data{Long = (long)o } },
				{ typeof(uint), (o, level) => new Data{Uint = (uint)o } },
				{ typeof(ulong), (o, level) => new Data{Ulong = (ulong)o } },
				{ typeof(bool), (o, level) => new Data{Bool = (bool)o } },
				{ typeof(string), (o, level) => new Data{String = (string)o } },
				{ typeof(Google.Protobuf.ByteString), (o, level) => new Data{ByteArray = (Google.Protobuf.ByteString)o } },
				{ typeof(IntVector2), (o, level) => new Data{ IntVector2 = ((IntVector2)o).ToStIntVector2()} },
				{ typeof(IntVector3), (o, level) => new Data{ IntVector3 = ((IntVector3)o).ToStIntVector3() } },
				{ typeof(Vector2), (o, level) => new Data{ Vector2 = ((Vector2)o).ToStVector2()} },
				{ typeof(Vector3), (o, level) => new Data{ Vector3 = ((Vector3)o).ToStVector3()} },
				//TODO: Lists
				{ typeof(Path), (o, level) => new Data{ Path = ((Path)o).Save() } }
				
			};
	}
}
