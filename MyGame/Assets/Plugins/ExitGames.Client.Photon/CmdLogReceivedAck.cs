namespace ExitGames.Client.Photon
{
	internal class CmdLogReceivedAck : CmdLogItem
	{
		public int ReceivedSentTime;

		public CmdLogReceivedAck(NCommand command, int timeInt, int rtt, int variance)
		{
			base.TimeInt = timeInt;
			base.Channel = command.commandChannelID;
			base.SequenceNumber = command.ackReceivedReliableSequenceNumber;
			base.Rtt = rtt;
			base.Variance = variance;
			this.ReceivedSentTime = command.ackReceivedSentTime;
		}

		public override string ToString()
		{
			return string.Format("ACK  NOW: {0,5}  CH: {1,3} SQ: {2,4} RTT: {3,4} VAR: {4,3}  Sent: {5,5} Diff: {6,4}", base.TimeInt, base.Channel, base.SequenceNumber, base.Rtt, base.Variance, this.ReceivedSentTime, base.TimeInt - this.ReceivedSentTime);
		}
	}
}
