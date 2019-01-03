using System;

namespace ExitGames.Client.Photon
{
	/// <summary> Internal class for "commands" - the package in which operations are sent.</summary>
	internal class NCommand : IComparable<NCommand>
	{
		internal const int FLAG_RELIABLE = 1;

		internal const int FLAG_UNSEQUENCED = 2;

		internal const byte FV_UNRELIABLE = 0;

		internal const byte FV_RELIABLE = 1;

		internal const byte FV_UNRELIBALE_UNSEQUENCED = 2;

		internal const byte CT_NONE = 0;

		internal const byte CT_ACK = 1;

		internal const byte CT_CONNECT = 2;

		internal const byte CT_VERIFYCONNECT = 3;

		internal const byte CT_DISCONNECT = 4;

		internal const byte CT_PING = 5;

		internal const byte CT_SENDRELIABLE = 6;

		internal const byte CT_SENDUNRELIABLE = 7;

		internal const byte CT_SENDFRAGMENT = 8;

		internal const byte CT_EG_SERVERTIME = 12;

		internal const int HEADER_UDP_PACK_LENGTH = 12;

		internal const int CmdSizeMinimum = 12;

		internal const int CmdSizeAck = 20;

		internal const int CmdSizeConnect = 44;

		internal const int CmdSizeVerifyConnect = 44;

		internal const int CmdSizeDisconnect = 12;

		internal const int CmdSizePing = 12;

		internal const int CmdSizeReliableHeader = 12;

		internal const int CmdSizeUnreliableHeader = 16;

		internal const int CmdSizeFragmentHeader = 32;

		internal const int CmdSizeMaxHeader = 36;

		internal byte commandFlags;

		internal byte commandType;

		internal byte commandChannelID;

		internal int reliableSequenceNumber;

		internal int unreliableSequenceNumber;

		internal int unsequencedGroupNumber;

		internal byte reservedByte = 4;

		internal int startSequenceNumber;

		internal int fragmentCount;

		internal int fragmentNumber;

		internal int totalLength;

		internal int fragmentOffset;

		internal int fragmentsRemaining;

		internal byte[] Payload;

		internal int commandSentTime;

		internal byte commandSentCount;

		internal int roundTripTimeout;

		internal int timeoutTime;

		internal int ackReceivedReliableSequenceNumber;

		internal int ackReceivedSentTime;

		private byte[] completeCommand;

		internal int Size;

		/// <summary>this variant does only create outgoing commands and increments . incoming ones are created from a DataInputStream</summary>
		internal NCommand(EnetPeer peer, byte commandType, byte[] payload, byte channel)
		{
			this.commandType = commandType;
			this.commandFlags = 1;
			this.commandChannelID = channel;
			this.Payload = payload;
			this.Size = 12;
			switch (this.commandType)
			{
			case 3:
			case 5:
				break;
			case 2:
			{
				this.Size = 44;
				this.Payload = new byte[32];
				this.Payload[0] = 0;
				this.Payload[1] = 0;
				int mtuOffset = 2;
				Protocol.Serialize((short)peer.mtu, this.Payload, ref mtuOffset);
				this.Payload[4] = 0;
				this.Payload[5] = 0;
				this.Payload[6] = 128;
				this.Payload[7] = 0;
				this.Payload[11] = peer.ChannelCount;
				this.Payload[15] = 0;
				this.Payload[19] = 0;
				this.Payload[22] = 19;
				this.Payload[23] = 136;
				this.Payload[27] = 2;
				this.Payload[31] = 2;
				break;
			}
			case 4:
				this.Size = 12;
				if (peer.peerConnectionState != PeerBase.ConnectionStateValue.Connected)
				{
					this.commandFlags = 2;
					if (peer.peerConnectionState == PeerBase.ConnectionStateValue.Zombie)
					{
						this.reservedByte = 2;
					}
				}
				break;
			case 6:
				this.Size = 12 + payload.Length;
				break;
			case 7:
				this.Size = 16 + payload.Length;
				this.commandFlags = 0;
				break;
			case 8:
				this.Size = 32 + payload.Length;
				break;
			case 1:
				this.Size = 20;
				this.commandFlags = 0;
				break;
			}
		}

		internal static NCommand CreateAck(EnetPeer peer, NCommand commandToAck, int sentTime)
		{
			byte[] payload = new byte[8];
			int offset = 0;
			Protocol.Serialize(commandToAck.reliableSequenceNumber, payload, ref offset);
			Protocol.Serialize(sentTime, payload, ref offset);
			NCommand ack = new NCommand(peer, 1, payload, commandToAck.commandChannelID);
			ack.ackReceivedReliableSequenceNumber = commandToAck.reliableSequenceNumber;
			ack.ackReceivedSentTime = sentTime;
			return ack;
		}

		/// <summary>reads the command values (commandHeader and command-values) from incoming bytestream and populates the incoming command*</summary>
		internal NCommand(EnetPeer peer, byte[] inBuff, ref int readingOffset)
		{
			this.commandType = inBuff[readingOffset++];
			this.commandChannelID = inBuff[readingOffset++];
			this.commandFlags = inBuff[readingOffset++];
			this.reservedByte = inBuff[readingOffset++];
			Protocol.Deserialize(out this.Size, inBuff, ref readingOffset);
			Protocol.Deserialize(out this.reliableSequenceNumber, inBuff, ref readingOffset);
			peer.bytesIn += this.Size;
			switch (this.commandType)
			{
			case 1:
				Protocol.Deserialize(out this.ackReceivedReliableSequenceNumber, inBuff, ref readingOffset);
				Protocol.Deserialize(out this.ackReceivedSentTime, inBuff, ref readingOffset);
				break;
			case 6:
				this.Payload = new byte[this.Size - 12];
				break;
			case 7:
				Protocol.Deserialize(out this.unreliableSequenceNumber, inBuff, ref readingOffset);
				this.Payload = new byte[this.Size - 16];
				break;
			case 8:
				Protocol.Deserialize(out this.startSequenceNumber, inBuff, ref readingOffset);
				Protocol.Deserialize(out this.fragmentCount, inBuff, ref readingOffset);
				Protocol.Deserialize(out this.fragmentNumber, inBuff, ref readingOffset);
				Protocol.Deserialize(out this.totalLength, inBuff, ref readingOffset);
				Protocol.Deserialize(out this.fragmentOffset, inBuff, ref readingOffset);
				this.Payload = new byte[this.Size - 32];
				this.fragmentsRemaining = this.fragmentCount;
				break;
			case 3:
			{
				short outgoingPeerID = default(short);
				Protocol.Deserialize(out outgoingPeerID, inBuff, ref readingOffset);
				readingOffset += 30;
				if (peer.peerID == -1)
				{
					peer.peerID = outgoingPeerID;
				}
				break;
			}
			}
			if (this.Payload != null)
			{
				Buffer.BlockCopy(inBuff, readingOffset, this.Payload, 0, this.Payload.Length);
				readingOffset += this.Payload.Length;
			}
		}

		internal byte[] Serialize()
		{
			if (this.completeCommand != null)
			{
				return this.completeCommand;
			}
			int payloadLength = (this.Payload != null) ? this.Payload.Length : 0;
			int headerLength = 12;
			if (this.commandType == 7)
			{
				headerLength = 16;
			}
			else if (this.commandType == 8)
			{
				headerLength = 32;
			}
			this.completeCommand = new byte[headerLength + payloadLength];
			this.completeCommand[0] = this.commandType;
			this.completeCommand[1] = this.commandChannelID;
			this.completeCommand[2] = this.commandFlags;
			this.completeCommand[3] = this.reservedByte;
			int offset = 4;
			Protocol.Serialize(this.completeCommand.Length, this.completeCommand, ref offset);
			Protocol.Serialize(this.reliableSequenceNumber, this.completeCommand, ref offset);
			if (this.commandType == 7)
			{
				offset = 12;
				Protocol.Serialize(this.unreliableSequenceNumber, this.completeCommand, ref offset);
			}
			else if (this.commandType == 8)
			{
				offset = 12;
				Protocol.Serialize(this.startSequenceNumber, this.completeCommand, ref offset);
				Protocol.Serialize(this.fragmentCount, this.completeCommand, ref offset);
				Protocol.Serialize(this.fragmentNumber, this.completeCommand, ref offset);
				Protocol.Serialize(this.totalLength, this.completeCommand, ref offset);
				Protocol.Serialize(this.fragmentOffset, this.completeCommand, ref offset);
			}
			if (payloadLength > 0)
			{
				Buffer.BlockCopy(this.Payload, 0, this.completeCommand, headerLength, payloadLength);
			}
			this.Payload = null;
			return this.completeCommand;
		}

		public int CompareTo(NCommand other)
		{
			if ((this.commandFlags & 1) != 0)
			{
				return this.reliableSequenceNumber - other.reliableSequenceNumber;
			}
			return this.unreliableSequenceNumber - other.unreliableSequenceNumber;
		}

		public override string ToString()
		{
			if (this.commandType == 1)
			{
				return string.Format("CMD({1} ack for c#:{0} s#/time {2}/{3})", this.commandChannelID, this.commandType, this.ackReceivedReliableSequenceNumber, this.ackReceivedSentTime);
			}
			return string.Format("CMD({1} c#:{0} r/u: {2}/{3} st/r#/rt:{4}/{5}/{6})", this.commandChannelID, this.commandType, this.reliableSequenceNumber, this.unreliableSequenceNumber, this.commandSentTime, this.commandSentCount, this.timeoutTime);
		}
	}
}
