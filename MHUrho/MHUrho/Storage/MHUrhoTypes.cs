// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: MHUrhoTypes.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace MHUrho.Storage {

  /// <summary>Holder for reflection information generated from MHUrhoTypes.proto</summary>
  public static partial class MHUrhoTypesReflection {

    #region Descriptor
    /// <summary>File descriptor for MHUrhoTypes.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static MHUrhoTypesReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChFNSFVyaG9UeXBlcy5wcm90bxIOTUhVcmhvLlN0b3JhZ2UaD1VyaG9UeXBl",
            "cy5wcm90byJQCgZTdFBhdGgSMAoKcGF0aFBvaW50cxgBIAMoCzIcLk1IVXJo",
            "by5TdG9yYWdlLlN0SW50VmVjdG9yMhIUCgxjdXJyZW50SW5kZXgYAiABKAVi",
            "BnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::MHUrho.Storage.UrhoTypesReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::MHUrho.Storage.StPath), global::MHUrho.Storage.StPath.Parser, new[]{ "PathPoints", "CurrentIndex" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class StPath : pb::IMessage<StPath> {
    private static readonly pb::MessageParser<StPath> _parser = new pb::MessageParser<StPath>(() => new StPath());
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<StPath> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::MHUrho.Storage.MHUrhoTypesReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StPath() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StPath(StPath other) : this() {
      pathPoints_ = other.pathPoints_.Clone();
      currentIndex_ = other.currentIndex_;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StPath Clone() {
      return new StPath(this);
    }

    /// <summary>Field number for the "pathPoints" field.</summary>
    public const int PathPointsFieldNumber = 1;
    private static readonly pb::FieldCodec<global::MHUrho.Storage.StIntVector2> _repeated_pathPoints_codec
        = pb::FieldCodec.ForMessage(10, global::MHUrho.Storage.StIntVector2.Parser);
    private readonly pbc::RepeatedField<global::MHUrho.Storage.StIntVector2> pathPoints_ = new pbc::RepeatedField<global::MHUrho.Storage.StIntVector2>();
    /// <summary>
    ///TODO: Probably just store target and recompute the path when loading
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::RepeatedField<global::MHUrho.Storage.StIntVector2> PathPoints {
      get { return pathPoints_; }
    }

    /// <summary>Field number for the "currentIndex" field.</summary>
    public const int CurrentIndexFieldNumber = 2;
    private int currentIndex_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CurrentIndex {
      get { return currentIndex_; }
      set {
        currentIndex_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as StPath);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(StPath other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if(!pathPoints_.Equals(other.pathPoints_)) return false;
      if (CurrentIndex != other.CurrentIndex) return false;
      return true;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      hash ^= pathPoints_.GetHashCode();
      if (CurrentIndex != 0) hash ^= CurrentIndex.GetHashCode();
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      pathPoints_.WriteTo(output, _repeated_pathPoints_codec);
      if (CurrentIndex != 0) {
        output.WriteRawTag(16);
        output.WriteInt32(CurrentIndex);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      size += pathPoints_.CalculateSize(_repeated_pathPoints_codec);
      if (CurrentIndex != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(CurrentIndex);
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(StPath other) {
      if (other == null) {
        return;
      }
      pathPoints_.Add(other.pathPoints_);
      if (other.CurrentIndex != 0) {
        CurrentIndex = other.CurrentIndex;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            input.SkipLastField();
            break;
          case 10: {
            pathPoints_.AddEntriesFrom(input, _repeated_pathPoints_codec);
            break;
          }
          case 16: {
            CurrentIndex = input.ReadInt32();
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
