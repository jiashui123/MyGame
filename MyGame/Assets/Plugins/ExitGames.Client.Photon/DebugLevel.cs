namespace ExitGames.Client.Photon
{
	/// <summary>
	/// Level / amount of DebugReturn callbacks. Each debug level includes output for lower ones: OFF, ERROR, WARNING, INFO, ALL.
	/// </summary>
	public enum DebugLevel : byte
	{
		/// <summary>No debug out.</summary>
		OFF,
		/// <summary>Only error descriptions.</summary>
		ERROR,
		/// <summary>Warnings and errors.</summary>
		WARNING,
		/// <summary>Information about internal workflows, warnings and errors.</summary>
		INFO,
		/// <summary>Most complete workflow description (but lots of debug output), info, warnings and errors.</summary>
		ALL = 5
	}
}
