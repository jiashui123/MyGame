namespace ExitGames.Client.Photon
{
	internal static class PhotonCodes
	{
		/// <summary>Result code for any (internal) operation.</summary>
		public const byte Ok = 0;

		/// <summary>Param code. Used in internal op: InitEncryption.</summary>
		internal static byte ClientKey = 1;

		/// <summary>Encryption-Mode code. Used in internal op: InitEncryption.</summary>
		internal static byte ModeKey = 2;

		/// <summary>Param code. Used in internal op: InitEncryption.</summary>
		internal static byte ServerKey = 1;

		/// <summary>Code of internal op: InitEncryption.</summary>
		internal static byte InitEncryption = 0;

		/// <summary>TODO: Code of internal op: Ping (used in PUN binary websockets).</summary>
		internal static byte Ping = 1;
	}
}
