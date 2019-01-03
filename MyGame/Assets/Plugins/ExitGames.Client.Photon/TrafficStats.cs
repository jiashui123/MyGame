namespace ExitGames.Client.Photon
{
	public class TrafficStats
	{
		/// <summary>Gets the byte-size of per-package headers.</summary>
		public int PackageHeaderSize
		{
			get;
			internal set;
		}

		/// <summary>
		/// Counts commands created/received by this client, ignoring repeats (out command count can be higher due to repeats).
		/// </summary>
		public int ReliableCommandCount
		{
			get;
			internal set;
		}

		public int UnreliableCommandCount
		{
			get;
			internal set;
		}

		public int FragmentCommandCount
		{
			get;
			internal set;
		}

		public int ControlCommandCount
		{
			get;
			internal set;
		}

		public int TotalPacketCount
		{
			get;
			internal set;
		}

		public int TotalCommandsInPackets
		{
			get;
			internal set;
		}

		public int ReliableCommandBytes
		{
			get;
			internal set;
		}

		public int UnreliableCommandBytes
		{
			get;
			internal set;
		}

		public int FragmentCommandBytes
		{
			get;
			internal set;
		}

		public int ControlCommandBytes
		{
			get;
			internal set;
		}

		public int TotalCommandCount
		{
			get
			{
				return this.ReliableCommandCount + this.UnreliableCommandCount + this.FragmentCommandCount + this.ControlCommandCount;
			}
		}

		public int TotalCommandBytes
		{
			get
			{
				return this.ReliableCommandBytes + this.UnreliableCommandBytes + this.FragmentCommandBytes + this.ControlCommandBytes;
			}
		}

		/// <summary>Gets count of bytes as traffic, excluding UDP/TCP headers (42 bytes / x bytes).</summary>
		public int TotalPacketBytes
		{
			get
			{
				return this.TotalCommandBytes + this.TotalPacketCount * this.PackageHeaderSize;
			}
		}

		/// <summary>Timestamp of the last incoming ACK read (every second this client sends a PING which must be ACKd.</summary>
		public int TimestampOfLastAck
		{
			get;
			set;
		}

		/// <summary>Timestamp of last incoming reliable command (every second we expect a PING).</summary>
		public int TimestampOfLastReliableCommand
		{
			get;
			set;
		}

		internal TrafficStats(int packageHeaderSize)
		{
			this.PackageHeaderSize = packageHeaderSize;
		}

		internal void CountControlCommand(int size)
		{
			this.ControlCommandBytes += size;
			this.ControlCommandCount++;
		}

		internal void CountReliableOpCommand(int size)
		{
			this.ReliableCommandBytes += size;
			this.ReliableCommandCount++;
		}

		internal void CountUnreliableOpCommand(int size)
		{
			this.UnreliableCommandBytes += size;
			this.UnreliableCommandCount++;
		}

		internal void CountFragmentOpCommand(int size)
		{
			this.FragmentCommandBytes += size;
			this.FragmentCommandCount++;
		}

		public override string ToString()
		{
			return string.Format("TotalPacketBytes: {0} TotalCommandBytes: {1} TotalPacketCount: {2} TotalCommandsInPackets: {3}", this.TotalPacketBytes, this.TotalCommandBytes, this.TotalPacketCount, this.TotalCommandsInPackets);
		}
	}
}
