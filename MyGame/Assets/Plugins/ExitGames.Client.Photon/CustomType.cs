using System;

namespace ExitGames.Client.Photon
{
	internal class CustomType
	{
		public readonly byte Code;

		public readonly Type Type;

		public readonly SerializeMethod SerializeFunction;

		public readonly DeserializeMethod DeserializeFunction;

		public readonly SerializeStreamMethod SerializeStreamFunction;

		public readonly DeserializeStreamMethod DeserializeStreamFunction;

		public CustomType(Type type, byte code, SerializeMethod serializeFunction, DeserializeMethod deserializeFunction)
		{
			this.Type = type;
			this.Code = code;
			this.SerializeFunction = serializeFunction;
			this.DeserializeFunction = deserializeFunction;
		}

		public CustomType(Type type, byte code, SerializeStreamMethod serializeFunction, DeserializeStreamMethod deserializeFunction)
		{
			this.Type = type;
			this.Code = code;
			this.SerializeStreamFunction = serializeFunction;
			this.DeserializeStreamFunction = deserializeFunction;
		}
	}
}
