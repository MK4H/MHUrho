using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Storage
{
    public class PluginDataStorage {
        Google.Protobuf.Collections.MapField<string, Data> map;

        public void Store<T>(string key, T value) {
            var data = new Data();
            switch (Type.GetTypeCode(typeof(T))) {
                case TypeCode.Boolean:
                    data.Bool = (bool)(object)value;
                    break;
                case TypeCode.Byte:
                    throw new ArgumentException("Unsupported type");
                case TypeCode.Char:
                    throw new ArgumentException("Unsupported type");
                case TypeCode.DateTime:
                    throw new ArgumentException("Unsupported type");
                case TypeCode.DBNull:
                    throw new ArgumentException("Unsupported type");
                case TypeCode.Decimal:
                    throw new ArgumentException("Unsupported type");
                case TypeCode.Double:
                    data.Double = (double)(object)value;
                    break;
                case TypeCode.Empty:
                    throw new ArgumentException("Unsupported type");
                case TypeCode.Int16:
                    throw new ArgumentException("Unsupported type");
                case TypeCode.Int32:
                    data.Int = (int)(object)value;
                    break;
                case TypeCode.Int64:
                    data.Long = (long)(object)value;
                    break;
                case TypeCode.Object:
                    //TODO: other objects
                    throw new NotImplementedException();
                case TypeCode.SByte:
                    throw new ArgumentException("Unsupported type");
                case TypeCode.Single:
                    data.Float = (float)(object)value;
                    break;
                case TypeCode.String:
                    data.String = (string)(object)value;
                    break;
                case TypeCode.UInt16:
                    throw new ArgumentException("Unsupported type");
                case TypeCode.UInt32:
                    data.Uint = (uint)(object)value;
                    break;
                case TypeCode.UInt64:
                    data.Ulong = (ulong)(object)value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            map.Add(key, data);
        }

        public T Get<T>(string key) {
            Data value = map[key];

            switch (value.ContentsCase) {
                case Data.ContentsOneofCase.None:
                    break;
                case Data.ContentsOneofCase.Float:
                    if (typeof(T) == typeof(float)) {
                        return (T)(object)value.Float;
                    }
                    break;
                case Data.ContentsOneofCase.Double:
                    if (typeof(T) == typeof(double)) {
                        return (T)(object)value.Double;
                    }
                    break;
                case Data.ContentsOneofCase.Int:
                    if (typeof(T) == typeof(int)) {
                        return (T)(object)value.Int;
                    }
                    break;
                case Data.ContentsOneofCase.Long:
                    if (typeof(T) == typeof(long)) {
                        return (T)(object)value.Long;
                    }
                    break;
                case Data.ContentsOneofCase.Uint:
                    if (typeof(T) == typeof(uint)) {
                        return (T)(object)value.Uint;
                    }
                    break;
                case Data.ContentsOneofCase.Ulong:
                    if (typeof(T) == typeof(ulong)) {
                        return (T)(object)value.Ulong;
                    }
                    break;
                case Data.ContentsOneofCase.Bool:
                    if (typeof(T) == typeof(bool)) {
                        return (T)(object)value.Bool;
                    }
                    break;
                case Data.ContentsOneofCase.String:
                    if (typeof(T) == typeof(string)) {
                        return (T)(object)value.String;
                    }
                    break;
                case Data.ContentsOneofCase.ByteArray:
                    if (typeof(T) == typeof(byte[])) {
                        return (T)(object)value.ByteArray.ToByteArray();
                    }
                    break;
                case Data.ContentsOneofCase.IntVector2:
                    if (typeof(T) == typeof(IntVector2)) {
                        StIntVector2 storedValue = value.IntVector2;
                        return (T)(object)new IntVector2(storedValue.X, storedValue.Y);
                    }
                    break;
                case Data.ContentsOneofCase.IntVector3:
                    if (typeof(T) == typeof(IntVector3)) {
                        StIntVector3 storedValue = value.IntVector3;
                        IntVector3 typedValue = new IntVector3();
                        typedValue.X = storedValue.X;
                        typedValue.Y = storedValue.Y;
                        typedValue.Z = storedValue.Z;
                        return (T)(object)typedValue;
                    }
                    break;
                case Data.ContentsOneofCase.Vector2:
                    if (typeof(T) == typeof(Vector2)) {
                        StVector2 storedValue = value.Vector2;
                        return (T)(object)new Vector2(storedValue.X, storedValue.Y);
                    }
                    break;
                case Data.ContentsOneofCase.Vector3:
                    if (typeof(T) == typeof(Vector3)) {
                        StVector3 storedValue = value.Vector3;
                        return (T)(object)new Vector3(storedValue.X, storedValue.Y, storedValue.Z);
                    }
                    break;
                case Data.ContentsOneofCase.FloatList:
                    if (typeof(T) == typeof(IEnumerable<float>)) {
                        return (T)(object)value.FloatList.Value;
                    }
                    break;
                case Data.ContentsOneofCase.DoubleList:
                    if (typeof(T) == typeof(IEnumerable<double>)) {
                        return (T)(object)value.DoubleList.Value;
                    }
                    break;
                case Data.ContentsOneofCase.IntList:
                    if (typeof(T) == typeof(IEnumerable<int>)) {
                        return (T)(object)value.IntList.Value;
                    }
                    break;
                case Data.ContentsOneofCase.LongList:
                    if (typeof(T) == typeof(IEnumerable<long>)) {
                        return (T)(object)value.LongList.Value;
                    }
                    break;
                case Data.ContentsOneofCase.BoolList:
                    if (typeof(T) == typeof(IEnumerable<bool>)) {
                        return (T)(object)value.BoolList.Value;
                    }
                    break;
                case Data.ContentsOneofCase.StringList:
                    if (typeof(T) == typeof(IEnumerable<string>)) {
                        return (T)(object)value.StringList.Value;
                    }
                    break;
                case Data.ContentsOneofCase.ByteArrayList:
                    if (typeof(T) == typeof(IEnumerable<byte[]>)) {
                        List<byte[]> typedValue = new List<byte[]>();
                        foreach (var item in value.ByteArrayList.Value) {
                            typedValue.Add(item.ToByteArray());
                        }
                        return (T)(object)typedValue;
                    }
                    break;
                case Data.ContentsOneofCase.IntVector2List:
                    if (typeof(T) == typeof(IEnumerable<IntVector2>)) {
                        List<IntVector2> typedValue = new List<IntVector2>();
                        foreach (var item in value.IntVector2List.Value) {
                            typedValue.Add(new IntVector2(item.X, item.Y));
                        }
                        return (T)(object)typedValue;
                    }
                    break;
                case Data.ContentsOneofCase.IntVector3List:
                    if (typeof(T) == typeof(IEnumerable<IntVector3>)) {
                        List<IntVector3> typedValue = new List<IntVector3>();
                        foreach (var item in value.IntVector3List.Value) {
                            IntVector3 intVector3 = new IntVector3() {
                                                                         X = item.X,
                                                                         Y = item.Y,
                                                                         Z = item.Z
                                                                     };
                            typedValue.Add(intVector3);
                        }
                        return (T)(object)typedValue;
                    }
                    break;
                case Data.ContentsOneofCase.Vector2List:
                    if (typeof(T) == typeof(IEnumerable<Vector2>)) {
                        List<Vector2> typedValue = new List<Vector2>();
                        foreach (var item in value.Vector2List.Value) {
                            typedValue.Add(new Vector2(item.X, item.Y));
                        }
                        return (T)(object)typedValue;
                    }
                    break;
                case Data.ContentsOneofCase.Vector3List:
                    if (typeof(T) == typeof(IEnumerable<Vector3>)) {
                        List<Vector3> typedValue = new List<Vector3>();
                        foreach (var item in value.Vector3List.Value) {
                            typedValue.Add(new Vector3(item.X, item.Y, item.Z));
                        }
                        return (T)(object)typedValue;
                    }
                    break;
                default:
                    throw new InvalidOperationException("Loaded data are invalid");
            }

            throw new ArgumentException("Given type does not equal the stored type", nameof(T));
        }

        public PluginDataStorage(Google.Protobuf.Collections.MapField<string, Data> map) {
            this.map = map;
        }
    }
}
