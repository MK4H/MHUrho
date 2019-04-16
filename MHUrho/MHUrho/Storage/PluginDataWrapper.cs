using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Urho;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.WorldMap;

namespace MHUrho.Storage
{
	public class PluginDataWrapper {
		
		protected class TypeArgumentException : ArgumentException
		{
			public TypeArgumentException(string wantedType, Data.ContentsOneofCase actualType)
			{
				
			}
		}

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
			if (PluginData.DataStorageTypesCase == PluginData.DataStorageTypesOneofCase.None) {
				PluginData.Sequential = new SequentialPluginData();
			}
			else if (PluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Sequential) {
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
			if (PluginData.DataStorageTypesCase == PluginData.DataStorageTypesOneofCase.None) {
				PluginData.Indexed = new IndexedPluginData();
			}
			else if (PluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Indexed) {
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
			if (PluginData.DataStorageTypesCase == PluginData.DataStorageTypesOneofCase.None) {
				PluginData.Named = new NamedPluginData();
			}
			else if (PluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Named) {
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
				{ typeof(float),(data,level) => data.ContentsCase == Data.ContentsOneofCase.Float ? data.Float : throw new TypeArgumentException("float", data.ContentsCase) },
				{ typeof(double), (data, level) => data.ContentsCase == Data.ContentsOneofCase.Double ? data.Double : throw new TypeArgumentException("double", data.ContentsCase)},
				{ typeof(int), (data, level) => data.ContentsCase == Data.ContentsOneofCase.Int ? data.Int : throw new TypeArgumentException("int", data.ContentsCase)},
				{ typeof(long), (data, level) => data.ContentsCase == Data.ContentsOneofCase.Long ? data.Long : throw new TypeArgumentException("long", data.ContentsCase)},
				{ typeof(uint), (data, level) => data.ContentsCase == Data.ContentsOneofCase.Uint ? data.Uint : throw new TypeArgumentException("uint", data.ContentsCase)},
				{ typeof(ulong), (data, level) => data.ContentsCase == Data.ContentsOneofCase.Ulong ? data.Ulong : throw new TypeArgumentException("ulong", data.ContentsCase)},
				{ typeof(bool), (data, level) => data.ContentsCase == Data.ContentsOneofCase.Bool ? data.Bool : throw new TypeArgumentException("bool", data.ContentsCase)},
				{ typeof(string), (data, level) => data.ContentsCase == Data.ContentsOneofCase.String ? data.String : throw new TypeArgumentException("string", data.ContentsCase)},
				{ typeof(Google.Protobuf.ByteString), (data, level) => data.ContentsCase == Data.ContentsOneofCase.ByteArray ? data.ByteArray : throw new TypeArgumentException("Google.Protobuf.ByteString", data.ContentsCase)},
				{ typeof(IntVector2), (data, level) => data.ContentsCase == Data.ContentsOneofCase.IntVector2 ? data.IntVector2.ToIntVector2() : throw new TypeArgumentException("IntVector2", data.ContentsCase)},
				{ typeof(IntVector3), (data, level) => data.ContentsCase == Data.ContentsOneofCase.IntVector3 ? data.IntVector3.ToIntVector3() : throw new TypeArgumentException("IntVector3", data.ContentsCase)},
				{ typeof(Vector2), (data, level) => data.ContentsCase == Data.ContentsOneofCase.Vector2 ? data.Vector2.ToVector2() : throw new TypeArgumentException("Vector2", data.ContentsCase)},
				{ typeof(Vector3), (data, level) => data.ContentsCase == Data.ContentsOneofCase.Vector3 ? data.Vector3.ToVector3() : throw new TypeArgumentException("Vector3", data.ContentsCase)},
				{ typeof(IEnumerable<float>), (data, level) => data.ContentsCase == Data.ContentsOneofCase.FloatList ? data.FloatList.Value : throw new TypeArgumentException("IEnumerable<float>", data.ContentsCase)},
				{ typeof(IEnumerable<double>), (data, level) => data.ContentsCase == Data.ContentsOneofCase.DoubleList ? data.DoubleList.Value : throw new TypeArgumentException("IEnumerable<double>", data.ContentsCase)},
				{ typeof(IEnumerable<int>), (data, level) => data.ContentsCase == Data.ContentsOneofCase.IntList ? data.IntList.Value : throw new TypeArgumentException("IEnumerable<int>", data.ContentsCase)},
				{ typeof(IEnumerable<long>), (data, level) => data.ContentsCase == Data.ContentsOneofCase.LongList ? data.LongList.Value : throw new TypeArgumentException("IEnumerable<long>", data.ContentsCase)},
				{ typeof(IEnumerable<bool>), (data, level) => data.ContentsCase == Data.ContentsOneofCase.BoolList ? data.BoolList.Value : throw new TypeArgumentException("IEnumerable<bool>", data.ContentsCase)},
				{ typeof(IEnumerable<string>), (data, level) => data.ContentsCase == Data.ContentsOneofCase.StringList ? data.StringList.Value : throw new TypeArgumentException("IEnumerable<string>", data.ContentsCase)},
				{ typeof(IEnumerable<Google.Protobuf.ByteString>), (data, level) => data.ContentsCase == Data.ContentsOneofCase.ByteArrayList ? data.ByteArrayList.Value : throw new TypeArgumentException("IEnumerable<Google.Protobuf.ByteString>", data.ContentsCase)},
				{ typeof(IEnumerable<IntVector2>), (data, level) => data.ContentsCase == Data.ContentsOneofCase.IntVector2List? data.IntVector2List.Value : throw new TypeArgumentException("IEnumerable<IntVector2>", data.ContentsCase)},
				{ typeof(IEnumerable<IntVector3>), (data, level) => data.ContentsCase == Data.ContentsOneofCase.IntVector3List ? data.IntVector3List.Value : throw new TypeArgumentException("IEnumerable<IntVector3>", data.ContentsCase)},
				{ typeof(IEnumerable<Vector2>), (data, level) => data.ContentsCase == Data.ContentsOneofCase.Vector2List ? data.Vector2List.Value : throw new TypeArgumentException("IEnumerable<Vector2>", data.ContentsCase)},
				{ typeof(IEnumerable<Vector3>), (data, level) => data.ContentsCase == Data.ContentsOneofCase.Vector3List ? data.Vector3List.Value : throw new TypeArgumentException("IEnumerable<Vector3>", data.ContentsCase)},
				{ typeof(Path), (data, level) => data.ContentsCase == Data.ContentsOneofCase.Path ? Path.Load(data.Path, level) : throw new TypeArgumentException("Path", data.ContentsCase) }
				
			};

		//This does not need typecheck, because the type is checked by the C# generics system
		protected static Dictionary<Type, Func<object, ILevelManager, Data>> ToDataConvertors 
			= new Dictionary<Type, Func<object, ILevelManager, Data>> 
			{
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
				{ typeof(IEnumerable<float>), (o, level) => {
												var val = new FloatList();
												val.Value.AddRange((IEnumerable<float>) o);
												return new Data {FloatList = val};
											}  },
				{ typeof(IEnumerable<double>), (o, level) => {
												var val = new DoubleList();
												val.Value.AddRange((IEnumerable<double>) o);
												return new Data {DoubleList = val};
											}  },
				{ typeof(IEnumerable<int>), (o, level) => {
												var val = new IntList();
												val.Value.AddRange((IEnumerable<int>) o);
												return new Data {IntList = val};
											}  },
				{ typeof(IEnumerable<long>), (o, level) => {
												var val = new LongList();
												val.Value.AddRange((IEnumerable<long>) o);
												return new Data {LongList = val};
											}  },
				{ typeof(IEnumerable<bool>), (o, level) => {
												var val = new BoolList();
												val.Value.AddRange((IEnumerable<bool>) o);
												return new Data {BoolList = val};
											}  },
				{ typeof(IEnumerable<string>), (o, level) => {
												var val = new StringList();
												val.Value.AddRange((IEnumerable<string>) o);
												return new Data {StringList = val};
											}  },
				{ typeof(IEnumerable<Google.Protobuf.ByteString>), (o, level) => {
																		var val = new ByteArrayList();
																		val.Value.AddRange((IEnumerable<Google.Protobuf.ByteString>) o);
																		return new Data {ByteArrayList = val};
																	}  },
				{ typeof(IEnumerable<IntVector2>), (o, level) => {
													var val = new IntVector2List();
													val.Value.AddRange(from vect in (IEnumerable<IntVector2>) o select vect.ToStIntVector2());
													return new Data {IntVector2List = val};
												}  },
				{ typeof(IEnumerable<IntVector3>), (o, level) => {
														var val = new IntVector3List();
														val.Value.AddRange(from vect in (IEnumerable<IntVector3>) o select vect.ToStIntVector3());
														return new Data {IntVector3List = val};
													}  },
				{ typeof(IEnumerable<Vector2>), (o, level) => {
														var val = new Vector2List();
														val.Value.AddRange(from vect in (IEnumerable<Vector2>) o select vect.ToStVector2());
														return new Data {Vector2List = val};
													}  },
				{ typeof(IEnumerable<Vector3>), (o, level) => {
														var val = new Vector3List();
														val.Value.AddRange(from vect in (IEnumerable<Vector3>) o select vect.ToStVector3());
														return new Data {Vector3List = val};
													}  },
				{ typeof(Path), (o, level) => new Data{ Path = ((Path)o).Save() } }
				
			};


	}
}
