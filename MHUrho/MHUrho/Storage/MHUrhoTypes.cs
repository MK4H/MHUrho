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
            "cy5wcm90byJdCgpTdFdheXBvaW50EisKCHBvc2l0aW9uGAEgASgLMhkuTUhV",
            "cmhvLlN0b3JhZ2UuU3RWZWN0b3IzEgwKBHRpbWUYAiABKAISFAoMbW92ZW1l",
            "bnRUeXBlGAMgASgFIkcKEFN0UGF0aEVudW1lcmF0b3ISJAoEcGF0aBgBIAEo",
            "CzIWLk1IVXJoby5TdG9yYWdlLlN0UGF0aBINCgVpbmRleBgCIAEoBSI3CgZT",
            "dFBhdGgSLQoJd2F5cG9pbnRzGAEgAygLMhouTUhVcmhvLlN0b3JhZ2UuU3RX",
            "YXlwb2ludGIGcHJvdG8z"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::MHUrho.Storage.UrhoTypesReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::MHUrho.Storage.StWaypoint), global::MHUrho.Storage.StWaypoint.Parser, new[]{ "Position", "Time", "MovementType" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::MHUrho.Storage.StPathEnumerator), global::MHUrho.Storage.StPathEnumerator.Parser, new[]{ "Path", "Index" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::MHUrho.Storage.StPath), global::MHUrho.Storage.StPath.Parser, new[]{ "Waypoints" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class StWaypoint : pb::IMessage<StWaypoint> {
    private static readonly pb::MessageParser<StWaypoint> _parser = new pb::MessageParser<StWaypoint>(() => new StWaypoint());
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<StWaypoint> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::MHUrho.Storage.MHUrhoTypesReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StWaypoint() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StWaypoint(StWaypoint other) : this() {
      Position = other.position_ != null ? other.Position.Clone() : null;
      time_ = other.time_;
      movementType_ = other.movementType_;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StWaypoint Clone() {
      return new StWaypoint(this);
    }

    /// <summary>Field number for the "position" field.</summary>
    public const int PositionFieldNumber = 1;
    private global::MHUrho.Storage.StVector3 position_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::MHUrho.Storage.StVector3 Position {
      get { return position_; }
      set {
        position_ = value;
      }
    }

    /// <summary>Field number for the "time" field.</summary>
    public const int TimeFieldNumber = 2;
    private float time_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public float Time {
      get { return time_; }
      set {
        time_ = value;
      }
    }

    /// <summary>Field number for the "movementType" field.</summary>
    public const int MovementTypeFieldNumber = 3;
    private int movementType_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int MovementType {
      get { return movementType_; }
      set {
        movementType_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as StWaypoint);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(StWaypoint other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(Position, other.Position)) return false;
      if (Time != other.Time) return false;
      if (MovementType != other.MovementType) return false;
      return true;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (position_ != null) hash ^= Position.GetHashCode();
      if (Time != 0F) hash ^= Time.GetHashCode();
      if (MovementType != 0) hash ^= MovementType.GetHashCode();
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (position_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(Position);
      }
      if (Time != 0F) {
        output.WriteRawTag(21);
        output.WriteFloat(Time);
      }
      if (MovementType != 0) {
        output.WriteRawTag(24);
        output.WriteInt32(MovementType);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (position_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Position);
      }
      if (Time != 0F) {
        size += 1 + 4;
      }
      if (MovementType != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(MovementType);
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(StWaypoint other) {
      if (other == null) {
        return;
      }
      if (other.position_ != null) {
        if (position_ == null) {
          position_ = new global::MHUrho.Storage.StVector3();
        }
        Position.MergeFrom(other.Position);
      }
      if (other.Time != 0F) {
        Time = other.Time;
      }
      if (other.MovementType != 0) {
        MovementType = other.MovementType;
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
            if (position_ == null) {
              position_ = new global::MHUrho.Storage.StVector3();
            }
            input.ReadMessage(position_);
            break;
          }
          case 21: {
            Time = input.ReadFloat();
            break;
          }
          case 24: {
            MovementType = input.ReadInt32();
            break;
          }
        }
      }
    }

  }

  public sealed partial class StPathEnumerator : pb::IMessage<StPathEnumerator> {
    private static readonly pb::MessageParser<StPathEnumerator> _parser = new pb::MessageParser<StPathEnumerator>(() => new StPathEnumerator());
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<StPathEnumerator> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::MHUrho.Storage.MHUrhoTypesReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StPathEnumerator() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StPathEnumerator(StPathEnumerator other) : this() {
      Path = other.path_ != null ? other.Path.Clone() : null;
      index_ = other.index_;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StPathEnumerator Clone() {
      return new StPathEnumerator(this);
    }

    /// <summary>Field number for the "path" field.</summary>
    public const int PathFieldNumber = 1;
    private global::MHUrho.Storage.StPath path_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::MHUrho.Storage.StPath Path {
      get { return path_; }
      set {
        path_ = value;
      }
    }

    /// <summary>Field number for the "index" field.</summary>
    public const int IndexFieldNumber = 2;
    private int index_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int Index {
      get { return index_; }
      set {
        index_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as StPathEnumerator);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(StPathEnumerator other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(Path, other.Path)) return false;
      if (Index != other.Index) return false;
      return true;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (path_ != null) hash ^= Path.GetHashCode();
      if (Index != 0) hash ^= Index.GetHashCode();
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (path_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(Path);
      }
      if (Index != 0) {
        output.WriteRawTag(16);
        output.WriteInt32(Index);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (path_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Path);
      }
      if (Index != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(Index);
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(StPathEnumerator other) {
      if (other == null) {
        return;
      }
      if (other.path_ != null) {
        if (path_ == null) {
          path_ = new global::MHUrho.Storage.StPath();
        }
        Path.MergeFrom(other.Path);
      }
      if (other.Index != 0) {
        Index = other.Index;
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
            if (path_ == null) {
              path_ = new global::MHUrho.Storage.StPath();
            }
            input.ReadMessage(path_);
            break;
          }
          case 16: {
            Index = input.ReadInt32();
            break;
          }
        }
      }
    }

  }

  public sealed partial class StPath : pb::IMessage<StPath> {
    private static readonly pb::MessageParser<StPath> _parser = new pb::MessageParser<StPath>(() => new StPath());
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<StPath> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::MHUrho.Storage.MHUrhoTypesReflection.Descriptor.MessageTypes[2]; }
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
      waypoints_ = other.waypoints_.Clone();
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StPath Clone() {
      return new StPath(this);
    }

    /// <summary>Field number for the "waypoints" field.</summary>
    public const int WaypointsFieldNumber = 1;
    private static readonly pb::FieldCodec<global::MHUrho.Storage.StWaypoint> _repeated_waypoints_codec
        = pb::FieldCodec.ForMessage(10, global::MHUrho.Storage.StWaypoint.Parser);
    private readonly pbc::RepeatedField<global::MHUrho.Storage.StWaypoint> waypoints_ = new pbc::RepeatedField<global::MHUrho.Storage.StWaypoint>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::RepeatedField<global::MHUrho.Storage.StWaypoint> Waypoints {
      get { return waypoints_; }
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
      if(!waypoints_.Equals(other.waypoints_)) return false;
      return true;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      hash ^= waypoints_.GetHashCode();
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      waypoints_.WriteTo(output, _repeated_waypoints_codec);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      size += waypoints_.CalculateSize(_repeated_waypoints_codec);
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(StPath other) {
      if (other == null) {
        return;
      }
      waypoints_.Add(other.waypoints_);
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
            waypoints_.AddEntriesFrom(input, _repeated_waypoints_codec);
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
