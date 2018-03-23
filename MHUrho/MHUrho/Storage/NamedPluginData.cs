using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Storage
{


    public abstract class NamedPluginDataWrapper : PluginDataWrapper {

        public int Count => PluginData.Named.DataMap.Count;

        public bool ContainsKey(string key) {
            return PluginData.Named.DataMap.ContainsKey(key);
        }



        protected NamedPluginDataWrapper(PluginData pluginData) : base(pluginData) {
            if (pluginData.DataStorageTypesCase != PluginData.DataStorageTypesOneofCase.Named) {
                throw new ArgumentException("pluginData was not Named");
            }
        }
    }

    public class NamedPluginDataWriter : PluginDataWrapper {

        public void Store<T>(string key, T value) {
            PluginData.Named.DataMap.Add(key, ToDataConvertors[typeof(T)](value));
        }

        public NamedPluginDataWriter() : base(new PluginData {Named = new NamedPluginData()}) {

        }

        public NamedPluginDataWriter(PluginData pluginData) : base(pluginData) {

        }
    }
    public class NamedPluginDataReader : PluginDataWrapper {

        public T Get<T>(string key) {
            return (T)FromDataConvertors[typeof(T)](PluginData.Named.DataMap[key]);
        }

        public bool TryGetValue<T>(string key, out T value) {
            value = default(T);
            if (PluginData.Named.DataMap.TryGetValue(key, out Data valueData)) {
                value = (T)FromDataConvertors[typeof(T)](valueData);
                return true;
            }

            return false;
        }

        public NamedPluginDataReader(PluginData pluginData) : base(pluginData) {

        }
    }
}
