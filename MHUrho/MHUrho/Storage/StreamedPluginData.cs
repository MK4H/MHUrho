using System;
using System.Collections.Generic;
using System.Text;


namespace MHUrho.Storage
{
    public abstract class StreamedPluginDataWrapper : PluginDataWrapper {

        public int Count => PluginData.Streamed.Data.Count;

        protected StreamedPluginDataWrapper(PluginData pluginData) : base(pluginData) {
            if (pluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Streamed) {
                throw new ArgumentException("pluginData was not Streamed");
            }
        }
    }

    public class StreamedPluginDataWriter : StreamedPluginDataWrapper {

        public void StoreNext<T>(T value) {
            PluginData.Streamed.Data.Add(ToDataConvertors[typeof(T)](value));
        }

        public StreamedPluginDataWriter() : base(new PluginData { Streamed = new StreamPluginData() }) {

        }

        public StreamedPluginDataWriter(PluginData pluginData) : base(pluginData) {

        }

    }

    public class StreamedPluginDataReader : StreamedPluginDataWrapper {

        private IEnumerator<Data> dataEnumerator;

        public bool MoveNext() {
            return dataEnumerator.MoveNext();
        }

        public T GetCurrent<T>() {
            return(T) FromDataConvertors[typeof(T)](dataEnumerator.Current);
        }

        public void Reset() {
            this.dataEnumerator = PluginData.Streamed.Data.GetEnumerator();
        }

        public StreamedPluginDataReader(PluginData pluginData) : base(pluginData) {
            this.dataEnumerator = pluginData.Streamed.Data.GetEnumerator();
        }

    }
}
