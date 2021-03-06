// automatically generated by the FlatBuffers compiler, do not modify

namespace fb
{

using System;
using FlatBuffers;

public struct Body : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static Body GetRootAsBody(ByteBuffer _bb) { return GetRootAsBody(_bb, new Body()); }
  public static Body GetRootAsBody(ByteBuffer _bb, Body obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p.bb_pos = _i; __p.bb = _bb; }
  public Body __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public Command Cmd { get { int o = __p.__offset(4); return o != 0 ? (Command)__p.bb.GetInt(o + __p.bb_pos) : Command.NotSet; } }
  public Status status { get { int o = __p.__offset(6); return o != 0 ? (Status)__p.bb.GetInt(o + __p.bb_pos) : Status.NotSet; } }
  public string Data1 { get { int o = __p.__offset(8); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
  public ArraySegment<byte>? GetData1Bytes() { return __p.__vector_as_arraysegment(8); }
  public string Data2 { get { int o = __p.__offset(10); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
  public ArraySegment<byte>? GetData2Bytes() { return __p.__vector_as_arraysegment(10); }

  public static Offset<Body> CreateBody(FlatBufferBuilder builder,
      Command cmd = Command.NotSet,
      Status status = Status.NotSet,
      StringOffset data1Offset = default(StringOffset),
      StringOffset data2Offset = default(StringOffset)) {
    builder.StartObject(4);
    Body.AddData2(builder, data2Offset);
    Body.AddData1(builder, data1Offset);
    Body.AddStatus(builder, status);
    Body.AddCmd(builder, cmd);
    return Body.EndBody(builder);
  }

  public static void StartBody(FlatBufferBuilder builder) { builder.StartObject(4); }
  public static void AddCmd(FlatBufferBuilder builder, Command cmd) { builder.AddInt(0, (int)cmd, 0); }
  public static void AddStatus(FlatBufferBuilder builder, Status status) { builder.AddInt(1, (int)status, 0); }
  public static void AddData1(FlatBufferBuilder builder, StringOffset data1Offset) { builder.AddOffset(2, data1Offset.Value, 0); }
  public static void AddData2(FlatBufferBuilder builder, StringOffset data2Offset) { builder.AddOffset(3, data2Offset.Value, 0); }
  public static Offset<Body> EndBody(FlatBufferBuilder builder) {
    int o = builder.EndObject();
    return new Offset<Body>(o);
  }
  public static void FinishBodyBuffer(FlatBufferBuilder builder, Offset<Body> offset) { builder.Finish(offset.Value); }
};


}
