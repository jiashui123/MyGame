namespace ExitGames.Client.Photon
{
	internal class CmdLogSentReliable : CmdLogItem
	{
		public int Resend;

		public int RoundtripTimeout;

		public int Timeout;

		public bool TriggeredTimeout;

		public CmdLogSentReliable(NCommand command, int timeInt, int rtt, int variance, bool triggeredTimeout = false)
		{
			base.TimeInt = timeInt;
			base.Channel = command.commandChannelID;
			base.SequenceNumber = command.reliableSequenceNumber;
			base.Rtt = rtt;
			base.Variance = variance;
			this.Resend = command.commandSentCount;
			this.RoundtripTimeout = command.roundTripTimeout;
			this.Timeout = command.timeoutTime;
			this.TriggeredTimeout = triggeredTimeout;
		}

		public override string ToString()
		{
			return string.Format("SND  NOW: {0,5}  CH: {1,3} SQ: {2,4} RTT: {3,4} VAR: {4,3}  Resend#: {5,2} ResendIn: {7} Timeout: {6,5} {8}", base.TimeInt, base.Channel, base.SequenceNumber, base.Rtt, base.Variance, this.Resend, this.Timeout, this.RoundtripTimeout, this.TriggeredTimeout ? "< TIMEOUT" : "");
		}
	}
}
