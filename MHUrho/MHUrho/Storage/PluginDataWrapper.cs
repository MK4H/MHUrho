using System;
using System.Collections.Generic;
using System.Text;

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
        /// Creates a reader for wrapped data in Stream format,
        /// written by <see cref="StreamedPluginDataWriter"/>
        /// </summary>
        /// <returns>Rreader for data in stream format</returns>
        public StreamedPluginDataReader GetReaderForWrappedStreamedData() {
            if (PluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Streamed) {
                throw new
                    InvalidOperationException("Cannot get StreamedReader for data that are not stored in streamed format");
            }
            return new StreamedPluginDataReader(PluginData);
        }
        /// <summary>
        /// Creates a writer that stores data in Stream format,
        /// the written data then can be read by <see cref="StreamedPluginDataReader"/>
        /// </summary>
        /// <returns>Writer that writes data in Stream format</returns>
        public StreamedPluginDataWriter GetWriterForWrappedStreamedData() {
            if (PluginData.DataStorageTypesCase == PluginData.DataStorageTypesOneofCase.None) {
                PluginData.Streamed = new StreamPluginData();
            } else if (PluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Streamed) {
                throw new InvalidOperationException("Cannot get streamedWriter for data that are not streamed");
            }
            return new StreamedPluginDataWriter(PluginData);
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

        protected static Dictionary<Type, Func<Data, object>> FromDataConvertors;

        protected static Dictionary<Type, Func<object, Data>> ToDataConvertors;
    }
}
