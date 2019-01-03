#define DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ExitGames.Client.Photon
{
	/// <summary>
	/// Provides tools for the Exit Games Protocol
	/// </summary>
	public class Protocol
	{
		public const string protocolType = "GpBinaryV16";

		internal static readonly Dictionary<Type, CustomType> TypeDict = new Dictionary<Type, CustomType>();

		internal static readonly Dictionary<byte, CustomType> CodeDict = new Dictionary<byte, CustomType>();

		private static readonly byte[] memShort = new byte[2];

		private static readonly long[] memLongBlock = new long[1];

		private static readonly byte[] memLongBlockBytes = new byte[8];

		private static readonly float[] memFloatBlock = new float[1];

		private static readonly byte[] memFloatBlockBytes = new byte[4];

		private static readonly double[] memDoubleBlock = new double[1];

		private static readonly byte[] memDoubleBlockBytes = new byte[8];

		private static readonly byte[] memInteger = new byte[4];

		private static readonly byte[] memLong = new byte[8];

		private static readonly byte[] memFloat = new byte[4];

		private static readonly byte[] memDeserialize = new byte[4];

		private static readonly byte[] memDouble = new byte[8];

		internal static bool TryRegisterType(Type type, byte typeCode, SerializeMethod serializeFunction, DeserializeMethod deserializeFunction)
		{
			if (Protocol.CodeDict.ContainsKey(typeCode) || Protocol.TypeDict.ContainsKey(type))
			{
				return false;
			}
			CustomType customType = new CustomType(type, typeCode, serializeFunction, deserializeFunction);
			Protocol.CodeDict.Add(typeCode, customType);
			Protocol.TypeDict.Add(type, customType);
			return true;
		}

		internal static bool TryRegisterType(Type type, byte typeCode, SerializeStreamMethod serializeFunction, DeserializeStreamMethod deserializeFunction)
		{
			if (Protocol.CodeDict.ContainsKey(typeCode) || Protocol.TypeDict.ContainsKey(type))
			{
				return false;
			}
			CustomType customType = new CustomType(type, typeCode, serializeFunction, deserializeFunction);
			Protocol.CodeDict.Add(typeCode, customType);
			Protocol.TypeDict.Add(type, customType);
			return true;
		}

		private static bool SerializeCustom(MemoryStream dout, object serObject)
		{
			CustomType customType = default(CustomType);
			if (Protocol.TypeDict.TryGetValue(serObject.GetType(), out customType))
			{
				if (customType.SerializeStreamFunction == null)
				{
					byte[] bytesOfCustomType = customType.SerializeFunction(serObject);
					dout.WriteByte(99);
					dout.WriteByte(customType.Code);
					Protocol.SerializeShort(dout, (short)bytesOfCustomType.Length, false);
					dout.Write(bytesOfCustomType, 0, bytesOfCustomType.Length);
					return true;
				}
				dout.WriteByte(99);
				dout.WriteByte(customType.Code);
				long posOfLengthInfo = dout.Position;
				dout.Position += 2L;
				short serializedLength = customType.SerializeStreamFunction(dout, serObject);
				long newPos = dout.Position;
				dout.Position = posOfLengthInfo;
				Protocol.SerializeShort(dout, serializedLength, false);
				dout.Position += serializedLength;
				if (dout.Position != newPos)
				{
					throw new Exception("Serialization failed. Stream position corrupted. Should be " + newPos + " is now: " + dout.Position + " serializedLength: " + serializedLength);
				}
				return true;
			}
			return false;
		}

		private static object DeserializeCustom(MemoryStream din, byte customTypeCode)
		{
			short length = Protocol.DeserializeShort(din);
			CustomType customType = default(CustomType);
			if (Protocol.CodeDict.TryGetValue(customTypeCode, out customType))
			{
				if (customType.DeserializeStreamFunction == null)
				{
					byte[] bytes = new byte[length];
					din.Read(bytes, 0, length);
					return customType.DeserializeFunction(bytes);
				}
				long pos = din.Position;
				object result = customType.DeserializeStreamFunction(din, length);
				int bytesRead = (int)(din.Position - pos);
				if (bytesRead != length)
				{
					din.Position = pos + length;
				}
				return result;
			}
			return null;
		}

		/// <summary>
		/// Serialize creates a byte-array from the given object and returns it.
		/// </summary>
		/// <param name="obj">The object to serialize</param>
		/// <returns>The serialized byte-array</returns>
		public static byte[] Serialize(object obj)
		{
			MemoryStream ms = new MemoryStream(64);
			Protocol.Serialize(ms, obj, true);
			return ms.ToArray();
		}

		/// <summary>
		/// Deserialize returns an object reassembled from the given byte-array.
		/// </summary>
		/// <param name="serializedData">The byte-array to be Deserialized</param>
		/// <returns>The Deserialized object</returns>
		public static object Deserialize(byte[] serializedData)
		{
			MemoryStream memoryStream = new MemoryStream(serializedData);
			return Protocol.Deserialize(memoryStream, (byte)memoryStream.ReadByte());
		}

		internal static object DeserializeMessage(MemoryStream stream)
		{
			return Protocol.Deserialize(stream, (byte)stream.ReadByte());
		}

		internal static byte[] DeserializeRawMessage(MemoryStream stream)
		{
			return (byte[])Protocol.Deserialize(stream, (byte)stream.ReadByte());
		}

		internal static void SerializeMessage(MemoryStream ms, object msg)
		{
			Protocol.Serialize(ms, msg, true);
		}

		private static Type GetTypeOfCode(byte typeCode)
		{
			switch (typeCode)
			{
			case 105:
				return typeof(int);
			case 115:
				return typeof(string);
			case 97:
				return typeof(string[]);
			case 120:
				return typeof(byte[]);
			case 110:
				return typeof(int[]);
			case 104:
				return typeof(Hashtable);
			case 68:
				return typeof(IDictionary);
			case 111:
				return typeof(bool);
			case 107:
				return typeof(short);
			case 108:
				return typeof(long);
			case 98:
				return typeof(byte);
			case 102:
				return typeof(float);
			case 100:
				return typeof(double);
			case 121:
				return typeof(Array);
			case 99:
				return typeof(CustomType);
			case 122:
				return typeof(object[]);
			case 101:
				return typeof(EventData);
			case 113:
				return typeof(OperationRequest);
			case 112:
				return typeof(OperationResponse);
			case 0:
			case 42:
				return typeof(object);
			default:
				Debug.WriteLine("missing type: " + typeCode);
				throw new Exception("deserialize(): " + typeCode);
			}
		}

		private static GpType GetCodeOfType(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Byte:
				return GpType.Byte;
			case TypeCode.String:
				return GpType.String;
			case TypeCode.Boolean:
				return GpType.Boolean;
			case TypeCode.Int16:
				return GpType.Short;
			case TypeCode.Int32:
				return GpType.Integer;
			case TypeCode.Int64:
				return GpType.Long;
			case TypeCode.Single:
				return GpType.Float;
			case TypeCode.Double:
				return GpType.Double;
			default:
				if (type.IsArray)
				{
					if (type == typeof(byte[]))
					{
						return GpType.ByteArray;
					}
					return GpType.Array;
				}
				if (type == typeof(Hashtable))
				{
					return GpType.Hashtable;
				}
				if (type.IsGenericType && typeof(Dictionary<, >) == type.GetGenericTypeDefinition())
				{
					return GpType.Dictionary;
				}
				if (type == typeof(EventData))
				{
					return GpType.EventData;
				}
				if (type == typeof(OperationRequest))
				{
					return GpType.OperationRequest;
				}
				if (type == typeof(OperationResponse))
				{
					return GpType.OperationResponse;
				}
				return GpType.Unknown;
			}
		}

		private static Array CreateArrayByType(byte arrayType, short length)
		{
			return Array.CreateInstance(Protocol.GetTypeOfCode(arrayType), length);
		}

		internal static void SerializeOperationRequest(MemoryStream memStream, OperationRequest serObject, bool setType)
		{
			Protocol.SerializeOperationRequest(memStream, serObject.OperationCode, serObject.Parameters, setType);
		}

		internal static void SerializeOperationRequest(MemoryStream memStream, byte operationCode, Dictionary<byte, object> parameters, bool setType)
		{
			if (setType)
			{
				memStream.WriteByte(113);
			}
			memStream.WriteByte(operationCode);
			Protocol.SerializeParameterTable(memStream, parameters);
		}

		internal static OperationRequest DeserializeOperationRequest(MemoryStream din)
		{
			OperationRequest request = new OperationRequest();
			request.OperationCode = Protocol.DeserializeByte(din);
			request.Parameters = Protocol.DeserializeParameterTable(din);
			return request;
		}

		internal static void SerializeOperationResponse(MemoryStream memStream, OperationResponse serObject, bool setType)
		{
			if (setType)
			{
				memStream.WriteByte(112);
			}
			memStream.WriteByte(serObject.OperationCode);
			Protocol.SerializeShort(memStream, serObject.ReturnCode, false);
			if (string.IsNullOrEmpty(serObject.DebugMessage))
			{
				memStream.WriteByte(42);
			}
			else
			{
				Protocol.SerializeString(memStream, serObject.DebugMessage, false);
			}
			Protocol.SerializeParameterTable(memStream, serObject.Parameters);
		}

		internal static OperationResponse DeserializeOperationResponse(MemoryStream memoryStream)
		{
			OperationResponse response = new OperationResponse();
			response.OperationCode = Protocol.DeserializeByte(memoryStream);
			response.ReturnCode = Protocol.DeserializeShort(memoryStream);
			response.DebugMessage = (Protocol.Deserialize(memoryStream, Protocol.DeserializeByte(memoryStream)) as string);
			response.Parameters = Protocol.DeserializeParameterTable(memoryStream);
			return response;
		}

		internal static void SerializeEventData(MemoryStream memStream, EventData serObject, bool setType)
		{
			if (setType)
			{
				memStream.WriteByte(101);
			}
			memStream.WriteByte(serObject.Code);
			Protocol.SerializeParameterTable(memStream, serObject.Parameters);
		}

		internal static EventData DeserializeEventData(MemoryStream din)
		{
			EventData result = new EventData();
			result.Code = Protocol.DeserializeByte(din);
			result.Parameters = Protocol.DeserializeParameterTable(din);
			return result;
		}

		private static void SerializeParameterTable(MemoryStream memStream, Dictionary<byte, object> parameters)
		{
			if (parameters == null || parameters.Count == 0)
			{
				Protocol.SerializeShort(memStream, 0, false);
			}
			else
			{
				Protocol.SerializeShort(memStream, (short)parameters.Count, false);
				foreach (KeyValuePair<byte, object> parameter in parameters)
				{
					memStream.WriteByte(parameter.Key);
					Protocol.Serialize(memStream, parameter.Value, true);
				}
			}
		}

		private static Dictionary<byte, object> DeserializeParameterTable(MemoryStream memoryStream)
		{
			short numRetVals = Protocol.DeserializeShort(memoryStream);
			Dictionary<byte, object> retVals = new Dictionary<byte, object>(numRetVals);
			for (int i = 0; i < numRetVals; i++)
			{
				byte keyByteCode = (byte)memoryStream.ReadByte();
				object valueObject = retVals[keyByteCode] = Protocol.Deserialize(memoryStream, (byte)memoryStream.ReadByte());
			}
			return retVals;
		}

		/// <summary>
		/// Calls the correct serialization method for the passed object.
		/// </summary>
		private static void Serialize(MemoryStream dout, object serObject, bool setType)
		{
			if (serObject == null)
			{
				if (setType)
				{
					dout.WriteByte(42);
				}
			}
			else
			{
				switch (Protocol.GetCodeOfType(serObject.GetType()))
				{
				case GpType.Byte:
					Protocol.SerializeByte(dout, (byte)serObject, setType);
					break;
				case GpType.String:
					Protocol.SerializeString(dout, (string)serObject, setType);
					break;
				case GpType.Boolean:
					Protocol.SerializeBoolean(dout, (bool)serObject, setType);
					break;
				case GpType.Short:
					Protocol.SerializeShort(dout, (short)serObject, setType);
					break;
				case GpType.Integer:
					Protocol.SerializeInteger(dout, (int)serObject, setType);
					break;
				case GpType.Long:
					Protocol.SerializeLong(dout, (long)serObject, setType);
					break;
				case GpType.Float:
					Protocol.SerializeFloat(dout, (float)serObject, setType);
					break;
				case GpType.Double:
					Protocol.SerializeDouble(dout, (double)serObject, setType);
					break;
				case GpType.Hashtable:
					Protocol.SerializeHashTable(dout, (Hashtable)serObject, setType);
					break;
				case GpType.ByteArray:
					Protocol.SerializeByteArray(dout, (byte[])serObject, setType);
					break;
				case GpType.Array:
					if (serObject is int[])
					{
						Protocol.SerializeIntArrayOptimized(dout, (int[])serObject, setType);
					}
					else if (serObject.GetType().GetElementType() == typeof(object))
					{
						Protocol.SerializeObjectArray(dout, serObject as object[], setType);
					}
					else
					{
						Protocol.SerializeArray(dout, (Array)serObject, setType);
					}
					break;
				case GpType.Dictionary:
					Protocol.SerializeDictionary(dout, (IDictionary)serObject, setType);
					break;
				case GpType.EventData:
					Protocol.SerializeEventData(dout, (EventData)serObject, setType);
					break;
				case GpType.OperationResponse:
					Protocol.SerializeOperationResponse(dout, (OperationResponse)serObject, setType);
					break;
				case GpType.OperationRequest:
					Protocol.SerializeOperationRequest(dout, (OperationRequest)serObject, setType);
					break;
				default:
					if (Protocol.SerializeCustom(dout, serObject))
					{
						break;
					}
					throw new Exception("cannot serialize(): " + serObject.GetType());
				}
			}
		}

		private static void SerializeByte(MemoryStream dout, byte serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(98);
			}
			dout.WriteByte(serObject);
		}

		private static void SerializeBoolean(MemoryStream dout, bool serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(111);
			}
			dout.WriteByte((byte)(serObject ? 1 : 0));
		}

		private static void SerializeShort(MemoryStream dout, short serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(107);
			}
			lock (Protocol.memShort)
			{
				byte[] temp = Protocol.memShort;
				temp[0] = (byte)(serObject >> 8);
				temp[1] = (byte)serObject;
				dout.Write(temp, 0, 2);
			}
		}

		/// <summary>
		/// Serializes a short typed value into a byte-array (target) starting at the also given targetOffset.
		/// The altered offset is known to the caller, because it is given via a referenced parameter.
		/// </summary>
		/// <param name="value">The short value to be serialized</param>
		/// <param name="target">The byte-array to serialize the short to</param>
		/// <param name="targetOffset">The offset in the byte-array</param>
		public static void Serialize(short value, byte[] target, ref int targetOffset)
		{
			target[targetOffset++] = (byte)(value >> 8);
			target[targetOffset++] = (byte)value;
		}

		private static void SerializeInteger(MemoryStream dout, int serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(105);
			}
			lock (Protocol.memInteger)
			{
				byte[] buff = Protocol.memInteger;
				buff[0] = (byte)(serObject >> 24);
				buff[1] = (byte)(serObject >> 16);
				buff[2] = (byte)(serObject >> 8);
				buff[3] = (byte)serObject;
				dout.Write(buff, 0, 4);
			}
		}

		/// <summary>
		/// Serializes an int typed value into a byte-array (target) starting at the also given targetOffset.
		/// The altered offset is known to the caller, because it is given via a referenced parameter.
		/// </summary>
		/// <param name="value">The int value to be serialized</param>
		/// <param name="target">The byte-array to serialize the short to</param>
		/// <param name="targetOffset">The offset in the byte-array</param>
		public static void Serialize(int value, byte[] target, ref int targetOffset)
		{
			target[targetOffset++] = (byte)(value >> 24);
			target[targetOffset++] = (byte)(value >> 16);
			target[targetOffset++] = (byte)(value >> 8);
			target[targetOffset++] = (byte)value;
		}

		private static void SerializeLong(MemoryStream dout, long serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(108);
			}
			lock (Protocol.memLongBlock)
			{
				Protocol.memLongBlock[0] = serObject;
				Buffer.BlockCopy(Protocol.memLongBlock, 0, Protocol.memLongBlockBytes, 0, 8);
				byte[] data = Protocol.memLongBlockBytes;
				if (BitConverter.IsLittleEndian)
				{
					byte temp6 = data[0];
					byte temp5 = data[1];
					byte temp4 = data[2];
					byte temp3 = data[3];
					data[0] = data[7];
					data[1] = data[6];
					data[2] = data[5];
					data[3] = data[4];
					data[4] = temp3;
					data[5] = temp4;
					data[6] = temp5;
					data[7] = temp6;
				}
				dout.Write(data, 0, 8);
			}
		}

		private static void SerializeFloat(MemoryStream dout, float serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(102);
			}
			lock (Protocol.memFloatBlockBytes)
			{
				Protocol.memFloatBlock[0] = serObject;
				Buffer.BlockCopy(Protocol.memFloatBlock, 0, Protocol.memFloatBlockBytes, 0, 4);
				if (BitConverter.IsLittleEndian)
				{
					byte temp2 = Protocol.memFloatBlockBytes[0];
					byte temp = Protocol.memFloatBlockBytes[1];
					Protocol.memFloatBlockBytes[0] = Protocol.memFloatBlockBytes[3];
					Protocol.memFloatBlockBytes[1] = Protocol.memFloatBlockBytes[2];
					Protocol.memFloatBlockBytes[2] = temp;
					Protocol.memFloatBlockBytes[3] = temp2;
				}
				dout.Write(Protocol.memFloatBlockBytes, 0, 4);
			}
		}

		/// <summary>
		/// Serializes an float typed value into a byte-array (target) starting at the also given targetOffset.
		/// The altered offset is known to the caller, because it is given via a referenced parameter.
		/// </summary>
		/// <param name="value">The float value to be serialized</param>
		/// <param name="target">The byte-array to serialize the short to</param>
		/// <param name="targetOffset">The offset in the byte-array</param>
		public static void Serialize(float value, byte[] target, ref int targetOffset)
		{
			lock (Protocol.memFloatBlock)
			{
				Protocol.memFloatBlock[0] = value;
				Buffer.BlockCopy(Protocol.memFloatBlock, 0, target, targetOffset, 4);
			}
			if (BitConverter.IsLittleEndian)
			{
				byte temp2 = target[targetOffset];
				byte temp = target[targetOffset + 1];
				target[targetOffset] = target[targetOffset + 3];
				target[targetOffset + 1] = target[targetOffset + 2];
				target[targetOffset + 2] = temp;
				target[targetOffset + 3] = temp2;
			}
			targetOffset += 4;
		}

		private static void SerializeDouble(MemoryStream dout, double serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(100);
			}
			lock (Protocol.memDoubleBlockBytes)
			{
				Protocol.memDoubleBlock[0] = serObject;
				Buffer.BlockCopy(Protocol.memDoubleBlock, 0, Protocol.memDoubleBlockBytes, 0, 8);
				byte[] data = Protocol.memDoubleBlockBytes;
				if (BitConverter.IsLittleEndian)
				{
					byte temp6 = data[0];
					byte temp5 = data[1];
					byte temp4 = data[2];
					byte temp3 = data[3];
					data[0] = data[7];
					data[1] = data[6];
					data[2] = data[5];
					data[3] = data[4];
					data[4] = temp3;
					data[5] = temp4;
					data[6] = temp5;
					data[7] = temp6;
				}
				dout.Write(data, 0, 8);
			}
		}

		private static void SerializeString(MemoryStream dout, string serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(115);
			}
			byte[] Write = Encoding.UTF8.GetBytes(serObject);
			if (Write.Length > 32767)
			{
				throw new NotSupportedException("Strings that exceed a UTF8-encoded byte-length of 32767 (short.MaxValue) are not supported. Yours is: " + Write.Length);
			}
			Protocol.SerializeShort(dout, (short)Write.Length, false);
			dout.Write(Write, 0, Write.Length);
		}

		private static void SerializeArray(MemoryStream dout, Array serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(121);
			}
			if (serObject.Length > 32767)
			{
				throw new NotSupportedException("String[] that exceed 32767 (short.MaxValue) entries are not supported. Yours is: " + serObject.Length);
			}
			Protocol.SerializeShort(dout, (short)serObject.Length, false);
			Type elementType = serObject.GetType().GetElementType();
			GpType contentTypeCode = Protocol.GetCodeOfType(elementType);
			if (contentTypeCode != 0)
			{
				dout.WriteByte((byte)contentTypeCode);
				if (contentTypeCode == GpType.Dictionary)
				{
					bool setKeyType = default(bool);
					bool setValueType = default(bool);
					Protocol.SerializeDictionaryHeader(dout, (object)serObject, out setKeyType, out setValueType);
					for (int index3 = 0; index3 < serObject.Length; index3++)
					{
						object element = serObject.GetValue(index3);
						Protocol.SerializeDictionaryElements(dout, element, setKeyType, setValueType);
					}
				}
				else
				{
					for (int index3 = 0; index3 < serObject.Length; index3++)
					{
						object o = serObject.GetValue(index3);
						Protocol.Serialize(dout, o, false);
					}
				}
				return;
			}
			CustomType customType = default(CustomType);
			if (Protocol.TypeDict.TryGetValue(elementType, out customType))
			{
				dout.WriteByte(99);
				dout.WriteByte(customType.Code);
				int index3 = 0;
				short serializedLength;
				long newPos;
				while (true)
				{
					if (index3 < serObject.Length)
					{
						object obj = serObject.GetValue(index3);
						if (customType.SerializeStreamFunction == null)
						{
							byte[] bytesOfCustomType = customType.SerializeFunction(obj);
							Protocol.SerializeShort(dout, (short)bytesOfCustomType.Length, false);
							dout.Write(bytesOfCustomType, 0, bytesOfCustomType.Length);
						}
						else
						{
							long posOfLengthInfo = dout.Position;
							dout.Position += 2L;
							serializedLength = customType.SerializeStreamFunction(dout, obj);
							newPos = dout.Position;
							dout.Position = posOfLengthInfo;
							Protocol.SerializeShort(dout, serializedLength, false);
							dout.Position += serializedLength;
							if (dout.Position != newPos)
							{
								break;
							}
						}
						index3++;
						continue;
					}
					return;
				}
				throw new Exception("Serialization failed. Stream position corrupted. Should be " + newPos + " is now: " + dout.Position + " serializedLength: " + serializedLength);
			}
			throw new NotSupportedException("cannot serialize array of type " + elementType);
		}

		private static void SerializeByteArray(MemoryStream dout, byte[] serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(120);
			}
			Protocol.SerializeInteger(dout, serObject.Length, false);
			dout.Write(serObject, 0, serObject.Length);
		}

		private static void SerializeIntArrayOptimized(MemoryStream inWriter, int[] serObject, bool setType)
		{
			if (setType)
			{
				inWriter.WriteByte(121);
			}
			Protocol.SerializeShort(inWriter, (short)serObject.Length, false);
			inWriter.WriteByte(105);
			byte[] temp = new byte[serObject.Length * 4];
			int x = 0;
			for (int i = 0; i < serObject.Length; i++)
			{
				temp[x++] = (byte)(serObject[i] >> 24);
				temp[x++] = (byte)(serObject[i] >> 16);
				temp[x++] = (byte)(serObject[i] >> 8);
				temp[x++] = (byte)serObject[i];
			}
			inWriter.Write(temp, 0, temp.Length);
		}

		private static void SerializeStringArray(MemoryStream dout, string[] serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(97);
			}
			Protocol.SerializeShort(dout, (short)serObject.Length, false);
			for (int i = 0; i < serObject.Length; i++)
			{
				Protocol.SerializeString(dout, serObject[i], false);
			}
		}

		private static void SerializeObjectArray(MemoryStream dout, object[] objects, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(122);
			}
			Protocol.SerializeShort(dout, (short)objects.Length, false);
			foreach (object obj in objects)
			{
				Protocol.Serialize(dout, obj, true);
			}
		}

		private static void SerializeHashTable(MemoryStream dout, Hashtable serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(104);
			}
			Protocol.SerializeShort(dout, (short)serObject.Count, false);
			foreach (DictionaryEntry item in serObject)
			{
				Protocol.Serialize(dout, item.Key, true);
				Protocol.Serialize(dout, item.Value, true);
			}
		}

		private static void SerializeDictionary(MemoryStream dout, IDictionary serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(68);
			}
			bool setKeyType = default(bool);
			bool setValueType = default(bool);
			Protocol.SerializeDictionaryHeader(dout, (object)serObject, out setKeyType, out setValueType);
			Protocol.SerializeDictionaryElements(dout, serObject, setKeyType, setValueType);
		}

		private static void SerializeDictionaryHeader(MemoryStream writer, Type dictType)
		{
			bool setKeyType = default(bool);
			bool setValueType = default(bool);
			Protocol.SerializeDictionaryHeader(writer, (object)dictType, out setKeyType, out setValueType);
		}

		private static void SerializeDictionaryHeader(MemoryStream writer, object dict, out bool setKeyType, out bool setValueType)
		{
			Type[] types = dict.GetType().GetGenericArguments();
			setKeyType = (types[0] == typeof(object));
			setValueType = (types[1] == typeof(object));
			if (setKeyType)
			{
				writer.WriteByte(0);
			}
			else
			{
				GpType keyType = Protocol.GetCodeOfType(types[0]);
				if (keyType == GpType.Unknown || keyType == GpType.Dictionary)
				{
					throw new Exception("Unexpected - cannot serialize Dictionary with key type: " + types[0]);
				}
				writer.WriteByte((byte)keyType);
			}
			if (setValueType)
			{
				writer.WriteByte(0);
			}
			else
			{
				GpType valueType = Protocol.GetCodeOfType(types[1]);
				if (valueType == GpType.Unknown)
				{
					throw new Exception("Unexpected - cannot serialize Dictionary with value type: " + types[0]);
				}
				writer.WriteByte((byte)valueType);
				if (valueType == GpType.Dictionary)
				{
					Protocol.SerializeDictionaryHeader(writer, types[1]);
				}
			}
		}

		private static void SerializeDictionaryElements(MemoryStream writer, object dict, bool setKeyType, bool setValueType)
		{
			IDictionary d = (IDictionary)dict;
			Protocol.SerializeShort(writer, (short)d.Count, false);
			IDictionaryEnumerator enumerator = d.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					DictionaryEntry entry = (DictionaryEntry)enumerator.Current;
					if (!setValueType && entry.Value == null)
					{
						throw new Exception("Can't serialize null in Dictionary with specific value-type.");
					}
					if (!setKeyType && entry.Key == null)
					{
						throw new Exception("Can't serialize null in Dictionary with specific key-type.");
					}
					Protocol.Serialize(writer, entry.Key, setKeyType);
					Protocol.Serialize(writer, entry.Value, setValueType);
				}
			}
			finally
			{
				IDisposable disposable = enumerator as IDisposable;
				if (disposable != null)
				{
					disposable.Dispose();
				}
			}
		}

		private static object Deserialize(MemoryStream din, byte type)
		{
			switch (type)
			{
			case 105:
				return Protocol.DeserializeInteger(din);
			case 115:
				return Protocol.DeserializeString(din);
			case 97:
				return Protocol.DeserializeStringArray(din);
			case 120:
				return Protocol.DeserializeByteArray(din);
			case 110:
				return Protocol.DeserializeIntArray(din);
			case 104:
				return Protocol.DeserializeHashTable(din);
			case 68:
				return Protocol.DeserializeDictionary(din);
			case 111:
				return Protocol.DeserializeBoolean(din);
			case 107:
				return Protocol.DeserializeShort(din);
			case 108:
				return Protocol.DeserializeLong(din);
			case 98:
				return Protocol.DeserializeByte(din);
			case 102:
				return Protocol.DeserializeFloat(din);
			case 100:
				return Protocol.DeserializeDouble(din);
			case 121:
				return Protocol.DeserializeArray(din);
			case 99:
			{
				byte typeCode = (byte)din.ReadByte();
				return Protocol.DeserializeCustom(din, typeCode);
			}
			case 122:
				return Protocol.DeserializeObjectArray(din);
			case 101:
				return Protocol.DeserializeEventData(din);
			case 113:
				return Protocol.DeserializeOperationRequest(din);
			case 112:
				return Protocol.DeserializeOperationResponse(din);
			case 0:
			case 42:
				return null;
			default:
				Debug.WriteLine("missing type: " + type);
				throw new Exception("deserialize(): " + type);
			}
		}

		private static byte DeserializeByte(MemoryStream din)
		{
			return (byte)din.ReadByte();
		}

		private static bool DeserializeBoolean(MemoryStream din)
		{
			return din.ReadByte() != 0;
		}

		private static short DeserializeShort(MemoryStream din)
		{
			lock (Protocol.memShort)
			{
				byte[] data = Protocol.memShort;
				din.Read(data, 0, 2);
				return (short)(data[0] << 8 | data[1]);
			}
		}

		/// <summary>
		/// Deserialize fills the given short typed value with the given byte-array (source) starting at the also given offset.
		/// The result is placed in a variable (value). There is no need to return a value because the parameter value is given by reference.
		/// The altered offset is this way also known to the caller.
		/// </summary>
		/// <param name="value">The short value to deserialized into</param>
		/// <param name="source">The byte-array to deserialize from</param>
		/// <param name="offset">The offset in the byte-array</param>
		public static void Deserialize(out short value, byte[] source, ref int offset)
		{
			value = (short)(source[offset++] << 8 | source[offset++]);
		}

		/// <summary>
		/// DeserializeInteger returns an Integer typed value from the given Memorystream.
		/// </summary>
		private static int DeserializeInteger(MemoryStream din)
		{
			lock (Protocol.memInteger)
			{
				byte[] data = Protocol.memInteger;
				din.Read(data, 0, 4);
				return data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3];
			}
		}

		/// <summary>
		/// Deserialize fills the given int typed value with the given byte-array (source) starting at the also given offset.
		/// The result is placed in a variable (value). There is no need to return a value because the parameter value is given by reference.
		/// The altered offset is this way also known to the caller.
		/// </summary>
		/// <param name="value">The int value to deserialize into</param>
		/// <param name="source">The byte-array to deserialize from</param>
		/// <param name="offset">The offset in the byte-array</param>
		public static void Deserialize(out int value, byte[] source, ref int offset)
		{
			value = (source[offset++] << 24 | source[offset++] << 16 | source[offset++] << 8 | source[offset++]);
		}

		private static long DeserializeLong(MemoryStream din)
		{
			lock (Protocol.memLong)
			{
				byte[] data = Protocol.memLong;
				din.Read(data, 0, 8);
				if (BitConverter.IsLittleEndian)
				{
					return (long)((ulong)data[0] << 56 | (ulong)data[1] << 48 | (ulong)data[2] << 40 | (ulong)data[3] << 32 | (ulong)data[4] << 24 | (ulong)data[5] << 16 | (ulong)data[6] << 8 | data[7]);
				}
				return BitConverter.ToInt64(data, 0);
			}
		}

		private static float DeserializeFloat(MemoryStream din)
		{
			lock (Protocol.memFloat)
			{
				byte[] data = Protocol.memFloat;
				din.Read(data, 0, 4);
				if (BitConverter.IsLittleEndian)
				{
					byte temp2 = data[0];
					byte temp = data[1];
					data[0] = data[3];
					data[1] = data[2];
					data[2] = temp;
					data[3] = temp2;
				}
				return BitConverter.ToSingle(data, 0);
			}
		}

		/// <summary>
		/// Deserialize fills the given float typed value with the given byte-array (source) starting at the also given offset.
		/// The result is placed in a variable (value). There is no need to return a value because the parameter value is given by reference.
		/// The altered offset is this way also known to the caller.
		/// </summary>
		/// <param name="value">The float value to deserialize</param>
		/// <param name="source">The byte-array to deserialize from</param>
		/// <param name="offset">The offset in the byte-array</param>
		public static void Deserialize(out float value, byte[] source, ref int offset)
		{
			if (BitConverter.IsLittleEndian)
			{
				lock (Protocol.memDeserialize)
				{
					byte[] data = Protocol.memDeserialize;
					data[3] = source[offset++];
					data[2] = source[offset++];
					data[1] = source[offset++];
					data[0] = source[offset++];
					value = BitConverter.ToSingle(data, 0);
				}
			}
			else
			{
				value = BitConverter.ToSingle(source, offset);
				offset += 4;
			}
		}

		private static double DeserializeDouble(MemoryStream din)
		{
			lock (Protocol.memDouble)
			{
				byte[] data = Protocol.memDouble;
				din.Read(data, 0, 8);
				if (BitConverter.IsLittleEndian)
				{
					byte temp6 = data[0];
					byte temp5 = data[1];
					byte temp4 = data[2];
					byte temp3 = data[3];
					data[0] = data[7];
					data[1] = data[6];
					data[2] = data[5];
					data[3] = data[4];
					data[4] = temp3;
					data[5] = temp4;
					data[6] = temp5;
					data[7] = temp6;
				}
				return BitConverter.ToDouble(data, 0);
			}
		}

		private static string DeserializeString(MemoryStream din)
		{
			short length = Protocol.DeserializeShort(din);
			if (length == 0)
			{
				return "";
			}
			byte[] Read = new byte[length];
			din.Read(Read, 0, Read.Length);
			return Encoding.UTF8.GetString(Read, 0, Read.Length);
		}

		private static Array DeserializeArray(MemoryStream din)
		{
			short arrayLength = Protocol.DeserializeShort(din);
			byte valuesType = (byte)din.ReadByte();
			Array resultArray;
			switch (valuesType)
			{
			case 121:
			{
				Array innerArray3 = Protocol.DeserializeArray(din);
				Type elementType = innerArray3.GetType();
				resultArray = Array.CreateInstance(elementType, arrayLength);
				resultArray.SetValue(innerArray3, 0);
				for (short i = 1; i < arrayLength; i = (short)(i + 1))
				{
					innerArray3 = Protocol.DeserializeArray(din);
					resultArray.SetValue(innerArray3, i);
				}
				goto IL_01f4;
			}
			case 120:
				resultArray = Array.CreateInstance(typeof(byte[]), arrayLength);
				for (short i = 0; i < arrayLength; i = (short)(i + 1))
				{
					Array innerArray3 = Protocol.DeserializeByteArray(din);
					resultArray.SetValue(innerArray3, i);
				}
				goto IL_01f4;
			case 99:
			{
				byte customTypeCode = (byte)din.ReadByte();
				CustomType customType = default(CustomType);
				if (Protocol.CodeDict.TryGetValue(customTypeCode, out customType))
				{
					resultArray = Array.CreateInstance(customType.Type, arrayLength);
					for (int j = 0; j < arrayLength; j++)
					{
						short objLength = Protocol.DeserializeShort(din);
						if (customType.DeserializeStreamFunction == null)
						{
							byte[] bytes = new byte[objLength];
							din.Read(bytes, 0, objLength);
							resultArray.SetValue(customType.DeserializeFunction(bytes), j);
						}
						else
						{
							resultArray.SetValue(customType.DeserializeStreamFunction(din, objLength), j);
						}
					}
					goto IL_01f4;
				}
				throw new Exception("Cannot find deserializer for custom type: " + customTypeCode);
			}
			case 68:
			{
				Array result = null;
				Protocol.DeserializeDictionaryArray(din, arrayLength, out result);
				return result;
			}
			default:
				{
					resultArray = Protocol.CreateArrayByType(valuesType, arrayLength);
					for (short i = 0; i < arrayLength; i = (short)(i + 1))
					{
						resultArray.SetValue(Protocol.Deserialize(din, valuesType), i);
					}
					goto IL_01f4;
				}
				IL_01f4:
				return resultArray;
			}
		}

		private static byte[] DeserializeByteArray(MemoryStream din)
		{
			int size = Protocol.DeserializeInteger(din);
			byte[] retVal = new byte[size];
			din.Read(retVal, 0, size);
			return retVal;
		}

		private static int[] DeserializeIntArray(MemoryStream din)
		{
			int size = Protocol.DeserializeInteger(din);
			int[] retVal = new int[size];
			for (int i = 0; i < size; i++)
			{
				retVal[i] = Protocol.DeserializeInteger(din);
			}
			return retVal;
		}

		private static string[] DeserializeStringArray(MemoryStream din)
		{
			int size = Protocol.DeserializeShort(din);
			string[] val = new string[size];
			for (int i = 0; i < size; i++)
			{
				val[i] = Protocol.DeserializeString(din);
			}
			return val;
		}

		private static object[] DeserializeObjectArray(MemoryStream din)
		{
			short arrayLength = Protocol.DeserializeShort(din);
			object[] resultArray = new object[arrayLength];
			for (int i = 0; i < arrayLength; i++)
			{
				byte typeCode = (byte)din.ReadByte();
				resultArray[i] = Protocol.Deserialize(din, typeCode);
			}
			return resultArray;
		}

		private static Hashtable DeserializeHashTable(MemoryStream din)
		{
			int size = Protocol.DeserializeShort(din);
			Hashtable value = new Hashtable(size);
			for (int i = 0; i < size; i++)
			{
				object serKey = Protocol.Deserialize(din, (byte)din.ReadByte());
				object serValue = value[serKey] = Protocol.Deserialize(din, (byte)din.ReadByte());
			}
			return value;
		}

		private static IDictionary DeserializeDictionary(MemoryStream din)
		{
			byte keyType = (byte)din.ReadByte();
			byte valType = (byte)din.ReadByte();
			int size = Protocol.DeserializeShort(din);
			bool readKeyType = keyType == 0 || keyType == 42;
			bool readValType = valType == 0 || valType == 42;
			Type j = Protocol.GetTypeOfCode(keyType);
			Type v = Protocol.GetTypeOfCode(valType);
			Type dict = typeof(Dictionary<, >).MakeGenericType(j, v);
			IDictionary value = Activator.CreateInstance(dict) as IDictionary;
			for (int i = 0; i < size; i++)
			{
				object serKey = Protocol.Deserialize(din, readKeyType ? ((byte)din.ReadByte()) : keyType);
				object serValue = Protocol.Deserialize(din, readValType ? ((byte)din.ReadByte()) : valType);
				value.Add(serKey, serValue);
			}
			return value;
		}

		private static bool DeserializeDictionaryArray(MemoryStream din, short size, out Array arrayResult)
		{
			byte keyTypeCode = default(byte);
			byte valTypeCode = default(byte);
			Type dictType = Protocol.DeserializeDictionaryType(din, out keyTypeCode, out valTypeCode);
			arrayResult = Array.CreateInstance(dictType, size);
			for (short j = 0; j < size; j = (short)(j + 1))
			{
				IDictionary dict = Activator.CreateInstance(dictType) as IDictionary;
				if (dict == null)
				{
					return false;
				}
				short dictSize = Protocol.DeserializeShort(din);
				for (int i = 0; i < dictSize; i++)
				{
					object key;
					if (keyTypeCode != 0)
					{
						key = Protocol.Deserialize(din, keyTypeCode);
					}
					else
					{
						byte type2 = (byte)din.ReadByte();
						key = Protocol.Deserialize(din, type2);
					}
					object value;
					if (valTypeCode != 0)
					{
						value = Protocol.Deserialize(din, valTypeCode);
					}
					else
					{
						byte type2 = (byte)din.ReadByte();
						value = Protocol.Deserialize(din, type2);
					}
					dict.Add(key, value);
				}
				arrayResult.SetValue(dict, j);
			}
			return true;
		}

		private static Type DeserializeDictionaryType(MemoryStream reader, out byte keyTypeCode, out byte valTypeCode)
		{
			keyTypeCode = (byte)reader.ReadByte();
			valTypeCode = (byte)reader.ReadByte();
			GpType keyType = (GpType)keyTypeCode;
			GpType valueType = (GpType)valTypeCode;
			Type keyClrType = (keyType != 0) ? Protocol.GetTypeOfCode(keyTypeCode) : typeof(object);
			Type valueClrType = (valueType != 0) ? Protocol.GetTypeOfCode(valTypeCode) : typeof(object);
			return typeof(Dictionary<, >).MakeGenericType(keyClrType, valueClrType);
		}
	}
}
