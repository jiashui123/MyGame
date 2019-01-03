namespace ExitGames.Client.Photon
{
	/// <summary>
	///   The gp type.
	/// </summary>
	internal enum GpType : byte
	{
		/// <summary>
		///   Unkown type.
		/// </summary>
		Unknown,
		/// <summary>
		///   An array of objects.
		/// </summary>
		/// <remarks>
		///   This type is new in version 1.5.
		/// </remarks>
		Array = 121,
		/// <summary>
		///   A boolean Value.
		/// </summary>
		Boolean = 111,
		/// <summary>
		///   A byte value.
		/// </summary>
		Byte = 98,
		/// <summary>
		///   An array of bytes.
		/// </summary>
		ByteArray = 120,
		/// <summary>
		///   An array of objects.
		/// </summary>
		ObjectArray = 122,
		/// <summary>
		///   A 16-bit integer value.
		/// </summary>
		Short = 107,
		/// <summary>
		///   A 32-bit floating-point value.
		/// </summary>
		/// <remarks>
		///   This type is new in version 1.5.
		/// </remarks>
		Float = 102,
		/// <summary>
		///   A dictionary
		/// </summary>
		/// <remarks>
		///   This type is new in version 1.6.
		/// </remarks>
		Dictionary = 68,
		/// <summary>
		///   A 64-bit floating-point value.
		/// </summary>
		/// <remarks>
		///   This type is new in version 1.5.
		/// </remarks>
		Double = 100,
		/// <summary>
		///   A Hashtable.
		/// </summary>
		Hashtable = 104,
		/// <summary>
		///   A 32-bit integer value.
		/// </summary>
		Integer,
		/// <summary>
		///   An array of 32-bit integer values.
		/// </summary>
		IntegerArray = 110,
		/// <summary>
		///   A 64-bit integer value.
		/// </summary>
		Long = 108,
		/// <summary>
		///   A string value.
		/// </summary>
		String = 115,
		/// <summary>
		///   An array of string values.
		/// </summary>
		StringArray = 97,
		/// <summary>
		///   A costum type
		/// </summary>
		Custom = 99,
		/// <summary>
		///   Null value don't have types.
		/// </summary>
		Null = 42,
		EventData = 101,
		OperationRequest = 113,
		OperationResponse = 112
	}
}
