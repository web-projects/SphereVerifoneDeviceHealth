// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: log_message.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace IPA5.XO.ProtoBuf {

  /// <summary>Holder for reflection information generated from log_message.proto</summary>
  public static partial class LogMessageReflection {

    #region Descriptor
    /// <summary>File descriptor for log_message.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static LogMessageReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChFsb2dfbWVzc2FnZS5wcm90bxIQSVBBNS5YTy5Qcm90b0J1ZiL6BgoKTG9n",
            "TWVzc2FnZRJACgpwYXJhbWV0ZXJzGAEgAygLMiwuSVBBNS5YTy5Qcm90b0J1",
            "Zi5Mb2dNZXNzYWdlLlBhcmFtZXRlcnNFbnRyeRI4Cglsb2dfbGV2ZWwYAiAB",
            "KA4yJS5JUEE1LlhPLlByb3RvQnVmLkxvZ01lc3NhZ2UuTG9nTGV2ZWwSDwoH",
            "bWVzc2FnZRgDIAEoCRIRCglleGNlcHRpb24YBCABKAkSFQoNYXNzZW1ibHlf",
            "bmFtZRgFIAEoCRI2Cghsb2dfdHlwZRgGIAEoDjIkLklQQTUuWE8uUHJvdG9C",
            "dWYuTG9nTWVzc2FnZS5Mb2dUeXBlEhQKDG1lc3NhZ2VfbmFtZRgHIAEoCRIS",
            "CgpzZXNzaW9uX2lkGAggASgJEh8KF2xpbmtfcmVxdWVzdF9tZXNzYWdlX2lk",
            "GAkgASgJEh4KFmxpbmtfYWN0aW9uX21lc3NhZ2VfaWQYCiABKAkSEgoKdGlt",
            "ZV9zdGFtcBgLIAEoCRI2CgVpbnB1dBgMIAMoCzInLklQQTUuWE8uUHJvdG9C",
            "dWYuTG9nTWVzc2FnZS5JbnB1dEVudHJ5Eg4KBm91dHB1dBgNIAEoCRINCgVj",
            "bGFzcxgOIAEoCRIQCghmdW5jdGlvbhgPIAEoCRIOCgZ0YXJnZXQYECABKAkS",
            "DgoGc291cmNlGBEgASgJEhEKCWhvc3RfbmFtZRgSIAEoCRITCgtzdGF0dXNf",
            "Y29kZRgTIAEoBRITCgtzdGF0dXNfdHlwZRgUIAEoBRoxCg9QYXJhbWV0ZXJz",
            "RW50cnkSCwoDa2V5GAEgASgJEg0KBXZhbHVlGAIgASgJOgI4ARosCgpJbnB1",
            "dEVudHJ5EgsKA2tleRgBIAEoCRINCgV2YWx1ZRgCIAEoCToCOAEiTQoITG9n",
            "TGV2ZWwSCQoFVFJBQ0UQABIJCgVERUJVRxABEggKBElORk8QAhIICgRXQVJO",
            "EAMSCQoFRVJST1IQBBIMCghDUklUSUNBTBAFIocBCgdMb2dUeXBlEg0KCUVY",
            "Q0VQVElPThAAEgsKB01FU1NBR0UQARIKCgZTVEFUVVMQAhIJCgVJTlBVVBAD",
            "EgoKBk9VVFBVVBAEEhQKEElOVEVHUkFUSU9OSU5QVVQQBRIVChFJTlRFR1JB",
            "VElPTk9VVFBVVBAGEhAKDE5PVEFWQUlMQUJMRRAHIj0KCExvZ0JhdGNoEjEK",
            "C2xvZ01lc3NhZ2VzGAEgAygLMhwuSVBBNS5YTy5Qcm90b0J1Zi5Mb2dNZXNz",
            "YWdlYgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::IPA5.XO.ProtoBuf.LogMessage), global::IPA5.XO.ProtoBuf.LogMessage.Parser, new[]{ "Parameters", "LogLevel", "Message", "Exception", "AssemblyName", "LogType", "MessageName", "SessionId", "LinkRequestMessageId", "LinkActionMessageId", "TimeStamp", "Input", "Output", "Class", "Function", "Target", "Source", "HostName", "StatusCode", "StatusType" }, null, new[]{ typeof(global::IPA5.XO.ProtoBuf.LogMessage.Types.LogLevel), typeof(global::IPA5.XO.ProtoBuf.LogMessage.Types.LogType) }, null, new pbr::GeneratedClrTypeInfo[] { null, null, }),
            new pbr::GeneratedClrTypeInfo(typeof(global::IPA5.XO.ProtoBuf.LogBatch), global::IPA5.XO.ProtoBuf.LogBatch.Parser, new[]{ "LogMessages" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class LogMessage : pb::IMessage<LogMessage> {
    private static readonly pb::MessageParser<LogMessage> _parser = new pb::MessageParser<LogMessage>(() => new LogMessage());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<LogMessage> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::IPA5.XO.ProtoBuf.LogMessageReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public LogMessage() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public LogMessage(LogMessage other) : this() {
      parameters_ = other.parameters_.Clone();
      logLevel_ = other.logLevel_;
      message_ = other.message_;
      exception_ = other.exception_;
      assemblyName_ = other.assemblyName_;
      logType_ = other.logType_;
      messageName_ = other.messageName_;
      sessionId_ = other.sessionId_;
      linkRequestMessageId_ = other.linkRequestMessageId_;
      linkActionMessageId_ = other.linkActionMessageId_;
      timeStamp_ = other.timeStamp_;
      input_ = other.input_.Clone();
      output_ = other.output_;
      class_ = other.class_;
      function_ = other.function_;
      target_ = other.target_;
      source_ = other.source_;
      hostName_ = other.hostName_;
      statusCode_ = other.statusCode_;
      statusType_ = other.statusType_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public LogMessage Clone() {
      return new LogMessage(this);
    }

    /// <summary>Field number for the "parameters" field.</summary>
    public const int ParametersFieldNumber = 1;
    private static readonly pbc::MapField<string, string>.Codec _map_parameters_codec
        = new pbc::MapField<string, string>.Codec(pb::FieldCodec.ForString(10, ""), pb::FieldCodec.ForString(18, ""), 10);
    private readonly pbc::MapField<string, string> parameters_ = new pbc::MapField<string, string>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::MapField<string, string> Parameters {
      get { return parameters_; }
    }

    /// <summary>Field number for the "log_level" field.</summary>
    public const int LogLevelFieldNumber = 2;
    private global::IPA5.XO.ProtoBuf.LogMessage.Types.LogLevel logLevel_ = global::IPA5.XO.ProtoBuf.LogMessage.Types.LogLevel.Trace;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::IPA5.XO.ProtoBuf.LogMessage.Types.LogLevel LogLevel {
      get { return logLevel_; }
      set {
        logLevel_ = value;
      }
    }

    /// <summary>Field number for the "message" field.</summary>
    public const int MessageFieldNumber = 3;
    private string message_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Message {
      get { return message_; }
      set {
        message_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "exception" field.</summary>
    public const int ExceptionFieldNumber = 4;
    private string exception_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Exception {
      get { return exception_; }
      set {
        exception_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "assembly_name" field.</summary>
    public const int AssemblyNameFieldNumber = 5;
    private string assemblyName_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string AssemblyName {
      get { return assemblyName_; }
      set {
        assemblyName_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "log_type" field.</summary>
    public const int LogTypeFieldNumber = 6;
    private global::IPA5.XO.ProtoBuf.LogMessage.Types.LogType logType_ = global::IPA5.XO.ProtoBuf.LogMessage.Types.LogType.Exception;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::IPA5.XO.ProtoBuf.LogMessage.Types.LogType LogType {
      get { return logType_; }
      set {
        logType_ = value;
      }
    }

    /// <summary>Field number for the "message_name" field.</summary>
    public const int MessageNameFieldNumber = 7;
    private string messageName_ = "";
    /// <summary>
    ///name of the message: Initialization, DeviceStatus,ADA,Event
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string MessageName {
      get { return messageName_; }
      set {
        messageName_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "session_id" field.</summary>
    public const int SessionIdFieldNumber = 8;
    private string sessionId_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string SessionId {
      get { return sessionId_; }
      set {
        sessionId_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "link_request_message_id" field.</summary>
    public const int LinkRequestMessageIdFieldNumber = 9;
    private string linkRequestMessageId_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string LinkRequestMessageId {
      get { return linkRequestMessageId_; }
      set {
        linkRequestMessageId_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "link_action_message_id" field.</summary>
    public const int LinkActionMessageIdFieldNumber = 10;
    private string linkActionMessageId_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string LinkActionMessageId {
      get { return linkActionMessageId_; }
      set {
        linkActionMessageId_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "time_stamp" field.</summary>
    public const int TimeStampFieldNumber = 11;
    private string timeStamp_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string TimeStamp {
      get { return timeStamp_; }
      set {
        timeStamp_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "input" field.</summary>
    public const int InputFieldNumber = 12;
    private static readonly pbc::MapField<string, string>.Codec _map_input_codec
        = new pbc::MapField<string, string>.Codec(pb::FieldCodec.ForString(10, ""), pb::FieldCodec.ForString(18, ""), 98);
    private readonly pbc::MapField<string, string> input_ = new pbc::MapField<string, string>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::MapField<string, string> Input {
      get { return input_; }
    }

    /// <summary>Field number for the "output" field.</summary>
    public const int OutputFieldNumber = 13;
    private string output_ = "";
    /// <summary>
    ///output json
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Output {
      get { return output_; }
      set {
        output_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "class" field.</summary>
    public const int ClassFieldNumber = 14;
    private string class_ = "";
    /// <summary>
    ///current assembly name, internal logging only
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Class {
      get { return class_; }
      set {
        class_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "function" field.</summary>
    public const int FunctionFieldNumber = 15;
    private string function_ = "";
    /// <summary>
    ///current assembly name, internal logging only
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Function {
      get { return function_; }
      set {
        function_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "target" field.</summary>
    public const int TargetFieldNumber = 16;
    private string target_ = "";
    /// <summary>
    ///target component name
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Target {
      get { return target_; }
      set {
        target_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "source" field.</summary>
    public const int SourceFieldNumber = 17;
    private string source_ = "";
    /// <summary>
    ///Source component name
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Source {
      get { return source_; }
      set {
        source_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "host_name" field.</summary>
    public const int HostNameFieldNumber = 18;
    private string hostName_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string HostName {
      get { return hostName_; }
      set {
        hostName_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "status_code" field.</summary>
    public const int StatusCodeFieldNumber = 19;
    private int statusCode_;
    /// <summary>
    ///status code id
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int StatusCode {
      get { return statusCode_; }
      set {
        statusCode_ = value;
      }
    }

    /// <summary>Field number for the "status_type" field.</summary>
    public const int StatusTypeFieldNumber = 20;
    private int statusType_;
    /// <summary>
    ///status type id
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int StatusType {
      get { return statusType_; }
      set {
        statusType_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as LogMessage);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(LogMessage other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!Parameters.Equals(other.Parameters)) return false;
      if (LogLevel != other.LogLevel) return false;
      if (Message != other.Message) return false;
      if (Exception != other.Exception) return false;
      if (AssemblyName != other.AssemblyName) return false;
      if (LogType != other.LogType) return false;
      if (MessageName != other.MessageName) return false;
      if (SessionId != other.SessionId) return false;
      if (LinkRequestMessageId != other.LinkRequestMessageId) return false;
      if (LinkActionMessageId != other.LinkActionMessageId) return false;
      if (TimeStamp != other.TimeStamp) return false;
      if (!Input.Equals(other.Input)) return false;
      if (Output != other.Output) return false;
      if (Class != other.Class) return false;
      if (Function != other.Function) return false;
      if (Target != other.Target) return false;
      if (Source != other.Source) return false;
      if (HostName != other.HostName) return false;
      if (StatusCode != other.StatusCode) return false;
      if (StatusType != other.StatusType) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      hash ^= Parameters.GetHashCode();
      if (LogLevel != global::IPA5.XO.ProtoBuf.LogMessage.Types.LogLevel.Trace) hash ^= LogLevel.GetHashCode();
      if (Message.Length != 0) hash ^= Message.GetHashCode();
      if (Exception.Length != 0) hash ^= Exception.GetHashCode();
      if (AssemblyName.Length != 0) hash ^= AssemblyName.GetHashCode();
      if (LogType != global::IPA5.XO.ProtoBuf.LogMessage.Types.LogType.Exception) hash ^= LogType.GetHashCode();
      if (MessageName.Length != 0) hash ^= MessageName.GetHashCode();
      if (SessionId.Length != 0) hash ^= SessionId.GetHashCode();
      if (LinkRequestMessageId.Length != 0) hash ^= LinkRequestMessageId.GetHashCode();
      if (LinkActionMessageId.Length != 0) hash ^= LinkActionMessageId.GetHashCode();
      if (TimeStamp.Length != 0) hash ^= TimeStamp.GetHashCode();
      hash ^= Input.GetHashCode();
      if (Output.Length != 0) hash ^= Output.GetHashCode();
      if (Class.Length != 0) hash ^= Class.GetHashCode();
      if (Function.Length != 0) hash ^= Function.GetHashCode();
      if (Target.Length != 0) hash ^= Target.GetHashCode();
      if (Source.Length != 0) hash ^= Source.GetHashCode();
      if (HostName.Length != 0) hash ^= HostName.GetHashCode();
      if (StatusCode != 0) hash ^= StatusCode.GetHashCode();
      if (StatusType != 0) hash ^= StatusType.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      parameters_.WriteTo(output, _map_parameters_codec);
      if (LogLevel != global::IPA5.XO.ProtoBuf.LogMessage.Types.LogLevel.Trace) {
        output.WriteRawTag(16);
        output.WriteEnum((int) LogLevel);
      }
      if (Message.Length != 0) {
        output.WriteRawTag(26);
        output.WriteString(Message);
      }
      if (Exception.Length != 0) {
        output.WriteRawTag(34);
        output.WriteString(Exception);
      }
      if (AssemblyName.Length != 0) {
        output.WriteRawTag(42);
        output.WriteString(AssemblyName);
      }
      if (LogType != global::IPA5.XO.ProtoBuf.LogMessage.Types.LogType.Exception) {
        output.WriteRawTag(48);
        output.WriteEnum((int) LogType);
      }
      if (MessageName.Length != 0) {
        output.WriteRawTag(58);
        output.WriteString(MessageName);
      }
      if (SessionId.Length != 0) {
        output.WriteRawTag(66);
        output.WriteString(SessionId);
      }
      if (LinkRequestMessageId.Length != 0) {
        output.WriteRawTag(74);
        output.WriteString(LinkRequestMessageId);
      }
      if (LinkActionMessageId.Length != 0) {
        output.WriteRawTag(82);
        output.WriteString(LinkActionMessageId);
      }
      if (TimeStamp.Length != 0) {
        output.WriteRawTag(90);
        output.WriteString(TimeStamp);
      }
      input_.WriteTo(output, _map_input_codec);
      if (Output.Length != 0) {
        output.WriteRawTag(106);
        output.WriteString(Output);
      }
      if (Class.Length != 0) {
        output.WriteRawTag(114);
        output.WriteString(Class);
      }
      if (Function.Length != 0) {
        output.WriteRawTag(122);
        output.WriteString(Function);
      }
      if (Target.Length != 0) {
        output.WriteRawTag(130, 1);
        output.WriteString(Target);
      }
      if (Source.Length != 0) {
        output.WriteRawTag(138, 1);
        output.WriteString(Source);
      }
      if (HostName.Length != 0) {
        output.WriteRawTag(146, 1);
        output.WriteString(HostName);
      }
      if (StatusCode != 0) {
        output.WriteRawTag(152, 1);
        output.WriteInt32(StatusCode);
      }
      if (StatusType != 0) {
        output.WriteRawTag(160, 1);
        output.WriteInt32(StatusType);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      size += parameters_.CalculateSize(_map_parameters_codec);
      if (LogLevel != global::IPA5.XO.ProtoBuf.LogMessage.Types.LogLevel.Trace) {
        size += 1 + pb::CodedOutputStream.ComputeEnumSize((int) LogLevel);
      }
      if (Message.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Message);
      }
      if (Exception.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Exception);
      }
      if (AssemblyName.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(AssemblyName);
      }
      if (LogType != global::IPA5.XO.ProtoBuf.LogMessage.Types.LogType.Exception) {
        size += 1 + pb::CodedOutputStream.ComputeEnumSize((int) LogType);
      }
      if (MessageName.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(MessageName);
      }
      if (SessionId.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(SessionId);
      }
      if (LinkRequestMessageId.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(LinkRequestMessageId);
      }
      if (LinkActionMessageId.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(LinkActionMessageId);
      }
      if (TimeStamp.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(TimeStamp);
      }
      size += input_.CalculateSize(_map_input_codec);
      if (Output.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Output);
      }
      if (Class.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Class);
      }
      if (Function.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Function);
      }
      if (Target.Length != 0) {
        size += 2 + pb::CodedOutputStream.ComputeStringSize(Target);
      }
      if (Source.Length != 0) {
        size += 2 + pb::CodedOutputStream.ComputeStringSize(Source);
      }
      if (HostName.Length != 0) {
        size += 2 + pb::CodedOutputStream.ComputeStringSize(HostName);
      }
      if (StatusCode != 0) {
        size += 2 + pb::CodedOutputStream.ComputeInt32Size(StatusCode);
      }
      if (StatusType != 0) {
        size += 2 + pb::CodedOutputStream.ComputeInt32Size(StatusType);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(LogMessage other) {
      if (other == null) {
        return;
      }
      parameters_.Add(other.parameters_);
      if (other.LogLevel != global::IPA5.XO.ProtoBuf.LogMessage.Types.LogLevel.Trace) {
        LogLevel = other.LogLevel;
      }
      if (other.Message.Length != 0) {
        Message = other.Message;
      }
      if (other.Exception.Length != 0) {
        Exception = other.Exception;
      }
      if (other.AssemblyName.Length != 0) {
        AssemblyName = other.AssemblyName;
      }
      if (other.LogType != global::IPA5.XO.ProtoBuf.LogMessage.Types.LogType.Exception) {
        LogType = other.LogType;
      }
      if (other.MessageName.Length != 0) {
        MessageName = other.MessageName;
      }
      if (other.SessionId.Length != 0) {
        SessionId = other.SessionId;
      }
      if (other.LinkRequestMessageId.Length != 0) {
        LinkRequestMessageId = other.LinkRequestMessageId;
      }
      if (other.LinkActionMessageId.Length != 0) {
        LinkActionMessageId = other.LinkActionMessageId;
      }
      if (other.TimeStamp.Length != 0) {
        TimeStamp = other.TimeStamp;
      }
      input_.Add(other.input_);
      if (other.Output.Length != 0) {
        Output = other.Output;
      }
      if (other.Class.Length != 0) {
        Class = other.Class;
      }
      if (other.Function.Length != 0) {
        Function = other.Function;
      }
      if (other.Target.Length != 0) {
        Target = other.Target;
      }
      if (other.Source.Length != 0) {
        Source = other.Source;
      }
      if (other.HostName.Length != 0) {
        HostName = other.HostName;
      }
      if (other.StatusCode != 0) {
        StatusCode = other.StatusCode;
      }
      if (other.StatusType != 0) {
        StatusType = other.StatusType;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            parameters_.AddEntriesFrom(input, _map_parameters_codec);
            break;
          }
          case 16: {
            LogLevel = (global::IPA5.XO.ProtoBuf.LogMessage.Types.LogLevel) input.ReadEnum();
            break;
          }
          case 26: {
            Message = input.ReadString();
            break;
          }
          case 34: {
            Exception = input.ReadString();
            break;
          }
          case 42: {
            AssemblyName = input.ReadString();
            break;
          }
          case 48: {
            LogType = (global::IPA5.XO.ProtoBuf.LogMessage.Types.LogType) input.ReadEnum();
            break;
          }
          case 58: {
            MessageName = input.ReadString();
            break;
          }
          case 66: {
            SessionId = input.ReadString();
            break;
          }
          case 74: {
            LinkRequestMessageId = input.ReadString();
            break;
          }
          case 82: {
            LinkActionMessageId = input.ReadString();
            break;
          }
          case 90: {
            TimeStamp = input.ReadString();
            break;
          }
          case 98: {
            input_.AddEntriesFrom(input, _map_input_codec);
            break;
          }
          case 106: {
            Output = input.ReadString();
            break;
          }
          case 114: {
            Class = input.ReadString();
            break;
          }
          case 122: {
            Function = input.ReadString();
            break;
          }
          case 130: {
            Target = input.ReadString();
            break;
          }
          case 138: {
            Source = input.ReadString();
            break;
          }
          case 146: {
            HostName = input.ReadString();
            break;
          }
          case 152: {
            StatusCode = input.ReadInt32();
            break;
          }
          case 160: {
            StatusType = input.ReadInt32();
            break;
          }
        }
      }
    }

    #region Nested types
    /// <summary>Container for nested types declared in the LogMessage message type.</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static partial class Types {
      public enum LogLevel {
        [pbr::OriginalName("TRACE")] Trace = 0,
        [pbr::OriginalName("DEBUG")] Debug = 1,
        [pbr::OriginalName("INFO")] Info = 2,
        [pbr::OriginalName("WARN")] Warn = 3,
        [pbr::OriginalName("ERROR")] Error = 4,
        [pbr::OriginalName("CRITICAL")] Critical = 5,
      }

      public enum LogType {
        [pbr::OriginalName("EXCEPTION")] Exception = 0,
        [pbr::OriginalName("MESSAGE")] Message = 1,
        [pbr::OriginalName("STATUS")] Status = 2,
        [pbr::OriginalName("INPUT")] Input = 3,
        [pbr::OriginalName("OUTPUT")] Output = 4,
        [pbr::OriginalName("INTEGRATIONINPUT")] Integrationinput = 5,
        [pbr::OriginalName("INTEGRATIONOUTPUT")] Integrationoutput = 6,
        [pbr::OriginalName("NOTAVAILABLE")] Notavailable = 7,
      }

    }
    #endregion

  }

  public sealed partial class LogBatch : pb::IMessage<LogBatch> {
    private static readonly pb::MessageParser<LogBatch> _parser = new pb::MessageParser<LogBatch>(() => new LogBatch());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<LogBatch> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::IPA5.XO.ProtoBuf.LogMessageReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public LogBatch() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public LogBatch(LogBatch other) : this() {
      logMessages_ = other.logMessages_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public LogBatch Clone() {
      return new LogBatch(this);
    }

    /// <summary>Field number for the "logMessages" field.</summary>
    public const int LogMessagesFieldNumber = 1;
    private static readonly pb::FieldCodec<global::IPA5.XO.ProtoBuf.LogMessage> _repeated_logMessages_codec
        = pb::FieldCodec.ForMessage(10, global::IPA5.XO.ProtoBuf.LogMessage.Parser);
    private readonly pbc::RepeatedField<global::IPA5.XO.ProtoBuf.LogMessage> logMessages_ = new pbc::RepeatedField<global::IPA5.XO.ProtoBuf.LogMessage>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::RepeatedField<global::IPA5.XO.ProtoBuf.LogMessage> LogMessages {
      get { return logMessages_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as LogBatch);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(LogBatch other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if(!logMessages_.Equals(other.logMessages_)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      hash ^= logMessages_.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      logMessages_.WriteTo(output, _repeated_logMessages_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      size += logMessages_.CalculateSize(_repeated_logMessages_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(LogBatch other) {
      if (other == null) {
        return;
      }
      logMessages_.Add(other.logMessages_);
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            logMessages_.AddEntriesFrom(input, _repeated_logMessages_codec);
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code