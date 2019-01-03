using System;
using System.Collections.Generic;
using System.Text;

namespace ExitGames.Client.Photon
{
	internal class EnetPeer : PeerBase
	{
		private const int CRC_LENGTH = 4;

		/// <summary>Will contain channel 0xFF and any other.</summary>
		private Dictionary<byte, EnetChannel> channels = new Dictionary<byte, EnetChannel>();

		/// <summary>One list for all channels keeps sent commands (for re-sending).</summary>
		private List<NCommand> sentReliableCommands = new List<NCommand>();

		/// <summary>One list for all channels keeps acknowledgements.</summary>
		private Queue<NCommand> outgoingAcknowledgementsList = new Queue<NCommand>();

		internal readonly int windowSize = 128;

		private byte udpCommandCount;

		private byte[] udpBuffer;

		private int udpBufferIndex;

		internal int challenge;

		internal int reliableCommandsRepeated;

		internal int reliableCommandsSent;

		internal int serverSentTime;

		internal static readonly byte[] udpHeader0xF3 = new byte[2]
		{
			243,
			2
		};

		internal static readonly byte[] messageHeader = EnetPeer.udpHeader0xF3;

		private byte[] initData = null;

		private EnetChannel[] channelArray = new EnetChannel[0];

		private Queue<int> commandsToRemove = new Queue<int>();

		internal override int QueuedIncomingCommandsCount
		{
			get
			{
				int x = 0;
				lock (this.channels)
				{
					foreach (EnetChannel value in this.channels.Values)
					{
						x += value.incomingReliableCommandsList.Count;
						x += value.incomingUnreliableCommandsList.Count;
					}
				}
				return x;
			}
		}

		internal override int QueuedOutgoingCommandsCount
		{
			get
			{
				int x = 0;
				lock (this.channels)
				{
					foreach (EnetChannel value in this.channels.Values)
					{
						x += value.outgoingReliableCommandsList.Count;
						x += value.outgoingUnreliableCommandsList.Count;
					}
				}
				return x;
			}
		}

		internal EnetPeer()
		{
			PeerBase.peerCount++;
			base.InitOnce();
			base.TrafficPackageHeaderSize = 12;
		}

		internal EnetPeer(IPhotonPeerListener listener)
			: this()
		{
			base.Listener = listener;
		}

		internal override void InitPeerBase()
		{
			base.InitPeerBase();
			base.peerID = -1;
			this.challenge = SupportClass.ThreadSafeRandom.Next();
			this.udpBuffer = new byte[base.mtu];
			this.reliableCommandsSent = 0;
			this.reliableCommandsRepeated = 0;
			lock (this.channels)
			{
				this.channels = new Dictionary<byte, EnetChannel>();
			}
			lock (this.channels)
			{
				this.channels[255] = new EnetChannel(255, base.commandBufferSize);
				for (byte i = 0; i < base.ChannelCount; i = (byte)(i + 1))
				{
					this.channels[i] = new EnetChannel(i, base.commandBufferSize);
				}
				this.channelArray = new EnetChannel[base.ChannelCount + 1];
				int c = 0;
				foreach (EnetChannel value in this.channels.Values)
				{
					this.channelArray[c++] = value;
				}
			}
			lock (this.sentReliableCommands)
			{
				this.sentReliableCommands = new List<NCommand>(base.commandBufferSize);
			}
			lock (this.outgoingAcknowledgementsList)
			{
				this.outgoingAcknowledgementsList = new Queue<NCommand>(base.commandBufferSize);
			}
			base.CommandLogInit();
		}

		internal override bool Connect(string ipport, string appID)
		{
			if (base.peerConnectionState != 0)
			{
				base.Listener.DebugReturn(DebugLevel.WARNING, "Connect() can't be called if peer is not Disconnected. Not connecting. peerConnectionState: " + base.peerConnectionState);
				return false;
			}
			if ((int)base.debugOut >= 5)
			{
				base.Listener.DebugReturn(DebugLevel.ALL, "Connect()");
			}
			base.ServerAddress = ipport;
			this.InitPeerBase();
			if (appID == null)
			{
				appID = "LoadBalancing";
			}
			for (int i = 0; i < 32; i++)
			{
				base.INIT_BYTES[i + 9] = (byte)((i < appID.Length) ? ((byte)appID[i]) : 0);
			}
			this.initData = base.INIT_BYTES;
			base.rt = new SocketUdp(this);
			if (base.rt == null)
			{
				base.Listener.DebugReturn(DebugLevel.ERROR, "Connect() failed, because SocketImplementation or socket was null. Set PhotonPeer.SocketImplementation before Connect().");
				return false;
			}
			if (base.rt.Connect())
			{
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsOutgoing.ControlCommandBytes += 44;
					base.TrafficStatsOutgoing.ControlCommandCount++;
				}
				base.peerConnectionState = ConnectionStateValue.Connecting;
				this.QueueOutgoingReliableCommand(new NCommand(this, 2, null, 255));
				return true;
			}
			return false;
		}

		internal override void Disconnect()
		{
			if (base.peerConnectionState != 0 && base.peerConnectionState != ConnectionStateValue.Disconnecting)
			{
				if (this.outgoingAcknowledgementsList != null)
				{
					lock (this.outgoingAcknowledgementsList)
					{
						this.outgoingAcknowledgementsList.Clear();
					}
				}
				if (this.sentReliableCommands != null)
				{
					lock (this.sentReliableCommands)
					{
						this.sentReliableCommands.Clear();
					}
				}
				lock (this.channels)
				{
					foreach (EnetChannel value in this.channels.Values)
					{
						value.clearAll();
					}
				}
				bool oldSettings = base.NetworkSimulationSettings.IsSimulationEnabled;
				base.NetworkSimulationSettings.IsSimulationEnabled = false;
				NCommand disconnectCommand = new NCommand(this, 4, null, 255);
				this.QueueOutgoingReliableCommand(disconnectCommand);
				this.SendOutgoingCommands();
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsOutgoing.CountControlCommand(disconnectCommand.Size);
				}
				base.rt.Disconnect();
				base.NetworkSimulationSettings.IsSimulationEnabled = oldSettings;
				base.peerConnectionState = ConnectionStateValue.Disconnected;
				base.Listener.OnStatusChanged(StatusCode.Disconnect);
			}
		}

		internal override void StopConnection()
		{
			if (base.rt != null)
			{
				base.rt.Disconnect();
			}
			base.peerConnectionState = ConnectionStateValue.Disconnected;
			if (base.Listener != null)
			{
				base.Listener.OnStatusChanged(StatusCode.Disconnect);
			}
		}

		internal override void FetchServerTimestamp()
		{
			if (base.peerConnectionState != ConnectionStateValue.Connected)
			{
				if ((int)base.debugOut >= 3)
				{
					base.EnqueueDebugReturn(DebugLevel.INFO, "FetchServerTimestamp() was skipped, as the client is not connected. Current ConnectionState: " + base.peerConnectionState);
				}
			}
			else
			{
				this.CreateAndEnqueueCommand(12, new byte[0], 255);
			}
		}

		/// <summary>
		/// Checks the incoming queue and Dispatches received data if possible.
		/// </summary>
		/// <returns>If a Dispatch happened or not, which shows if more Dispatches might be needed.</returns>
		internal override bool DispatchIncomingCommands()
		{
			while (true)
			{
				bool flag = true;
				MyAction action = default(MyAction);
				lock (base.ActionQueue)
				{
					if (base.ActionQueue.Count > 0)
					{
						action = base.ActionQueue.Dequeue();
						goto IL_0043;
					}
				}
				break;
				IL_0043:
				action();
			}
			NCommand command = null;
			lock (this.channels)
			{
				for (int index = 0; index < this.channelArray.Length; index++)
				{
					EnetChannel channel = this.channelArray[index];
					if (channel.incomingUnreliableCommandsList.Count > 0)
					{
						int lowestAvailableUnreliableCommandNumber = 2147483647;
						foreach (int key in channel.incomingUnreliableCommandsList.Keys)
						{
							NCommand cmd = channel.incomingUnreliableCommandsList[key];
							if (key < channel.incomingUnreliableSequenceNumber || cmd.reliableSequenceNumber < channel.incomingReliableSequenceNumber)
							{
								this.commandsToRemove.Enqueue(key);
							}
							else if (base.limitOfUnreliableCommands > 0 && channel.incomingUnreliableCommandsList.Count > base.limitOfUnreliableCommands)
							{
								this.commandsToRemove.Enqueue(key);
							}
							else if (key < lowestAvailableUnreliableCommandNumber && cmd.reliableSequenceNumber <= channel.incomingReliableSequenceNumber)
							{
								lowestAvailableUnreliableCommandNumber = key;
							}
						}
						while (this.commandsToRemove.Count > 0)
						{
							channel.incomingUnreliableCommandsList.Remove(this.commandsToRemove.Dequeue());
						}
						if (lowestAvailableUnreliableCommandNumber < 2147483647)
						{
							command = channel.incomingUnreliableCommandsList[lowestAvailableUnreliableCommandNumber];
						}
						if (command != null)
						{
							channel.incomingUnreliableCommandsList.Remove(command.unreliableSequenceNumber);
							channel.incomingUnreliableSequenceNumber = command.unreliableSequenceNumber;
							break;
						}
					}
					if (command == null && channel.incomingReliableCommandsList.Count > 0)
					{
						channel.incomingReliableCommandsList.TryGetValue(channel.incomingReliableSequenceNumber + 1, out command);
						if (command != null)
						{
							if (command.commandType != 8)
							{
								channel.incomingReliableSequenceNumber = command.reliableSequenceNumber;
								channel.incomingReliableCommandsList.Remove(command.reliableSequenceNumber);
							}
							else if (command.fragmentsRemaining > 0)
							{
								command = null;
							}
							else
							{
								byte[] completePayload = new byte[command.totalLength];
								int fragmentSequenceNumber = command.startSequenceNumber;
								while (fragmentSequenceNumber < command.startSequenceNumber + command.fragmentCount)
								{
									if (channel.ContainsReliableSequenceNumber(fragmentSequenceNumber))
									{
										NCommand fragment = channel.FetchReliableSequenceNumber(fragmentSequenceNumber);
										Buffer.BlockCopy(fragment.Payload, 0, completePayload, fragment.fragmentOffset, fragment.Payload.Length);
										channel.incomingReliableCommandsList.Remove(fragment.reliableSequenceNumber);
										fragmentSequenceNumber++;
										continue;
									}
									throw new Exception("command.fragmentsRemaining was 0, but not all fragments are found to be combined!");
								}
								if ((int)base.debugOut >= 5)
								{
									base.Listener.DebugReturn(DebugLevel.ALL, "assembled fragmented payload from " + command.fragmentCount + " parts. Dispatching now.");
								}
								command.Payload = completePayload;
								command.Size = 12 * command.fragmentCount + command.totalLength;
								channel.incomingReliableSequenceNumber = command.reliableSequenceNumber + command.fragmentCount - 1;
							}
							break;
						}
					}
				}
			}
			if (command != null && command.Payload != null)
			{
				base.ByteCountCurrentDispatch = command.Size;
				base.CommandInCurrentDispatch = command;
				if (this.DeserializeMessageAndCallback(command.Payload))
				{
					base.CommandInCurrentDispatch = null;
					return true;
				}
				base.CommandInCurrentDispatch = null;
			}
			return false;
		}

		/// <summary>
		/// gathers acks until udp-packet is full and sends it!
		/// </summary>
		internal override bool SendAcksOnly()
		{
			if (base.peerConnectionState == ConnectionStateValue.Disconnected)
			{
				return false;
			}
			if (base.rt == null || !base.rt.Connected)
			{
				return false;
			}
			lock (this.udpBuffer)
			{
				int remainingCommands = 0;
				this.udpBufferIndex = 12;
				if (base.crcEnabled)
				{
					this.udpBufferIndex += 4;
				}
				this.udpCommandCount = 0;
				base.timeInt = SupportClass.GetTickCount() - base.timeBase;
				lock (this.outgoingAcknowledgementsList)
				{
					if (this.outgoingAcknowledgementsList.Count > 0)
					{
						remainingCommands = this.SerializeToBuffer(this.outgoingAcknowledgementsList);
						base.timeLastSendAck = base.timeInt;
					}
				}
				if (base.timeInt > base.timeoutInt && this.sentReliableCommands.Count > 0)
				{
					lock (this.sentReliableCommands)
					{
						foreach (NCommand sentReliableCommand in this.sentReliableCommands)
						{
							if (sentReliableCommand != null && sentReliableCommand.roundTripTimeout != 0 && base.timeInt - sentReliableCommand.commandSentTime > sentReliableCommand.roundTripTimeout)
							{
								sentReliableCommand.commandSentCount = 1;
								sentReliableCommand.roundTripTimeout = 0;
								sentReliableCommand.timeoutTime = 2147483647;
								sentReliableCommand.commandSentTime = base.timeInt;
							}
						}
					}
				}
				if (this.udpCommandCount <= 0)
				{
					return false;
				}
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsOutgoing.TotalPacketCount++;
					base.TrafficStatsOutgoing.TotalCommandsInPackets += this.udpCommandCount;
				}
				this.SendData(this.udpBuffer, this.udpBufferIndex);
				return remainingCommands > 0;
			}
		}

		/// <summary>
		/// gathers commands from all (out)queues until udp-packet is full and sends it!
		/// </summary>
		internal override bool SendOutgoingCommands()
		{
			if (base.peerConnectionState == ConnectionStateValue.Disconnected)
			{
				return false;
			}
			if (!base.rt.Connected)
			{
				return false;
			}
			lock (this.udpBuffer)
			{
				int remainingCommands = 0;
				this.udpBufferIndex = 12;
				if (base.crcEnabled)
				{
					this.udpBufferIndex += 4;
				}
				this.udpCommandCount = 0;
				base.timeInt = SupportClass.GetTickCount() - base.timeBase;
				base.timeLastSendOutgoing = base.timeInt;
				lock (this.outgoingAcknowledgementsList)
				{
					if (this.outgoingAcknowledgementsList.Count > 0)
					{
						remainingCommands = this.SerializeToBuffer(this.outgoingAcknowledgementsList);
						base.timeLastSendAck = base.timeInt;
					}
				}
				if (!base.IsSendingOnlyAcks && base.timeInt > base.timeoutInt && this.sentReliableCommands.Count > 0)
				{
					lock (this.sentReliableCommands)
					{
						Queue<NCommand> commandsToResend = new Queue<NCommand>();
						foreach (NCommand sentReliableCommand in this.sentReliableCommands)
						{
							if (sentReliableCommand != null && base.timeInt - sentReliableCommand.commandSentTime > sentReliableCommand.roundTripTimeout)
							{
								if (sentReliableCommand.commandSentCount > base.sentCountAllowance || base.timeInt > sentReliableCommand.timeoutTime)
								{
									if ((int)base.debugOut >= 2)
									{
										base.Listener.DebugReturn(DebugLevel.WARNING, "Timeout-disconnect! Command: " + sentReliableCommand + " now: " + base.timeInt + " challenge: " + Convert.ToString(this.challenge, 16));
									}
									if (base.CommandLog != null)
									{
										base.CommandLog.Enqueue(new CmdLogSentReliable(sentReliableCommand, base.timeInt, base.roundTripTime, base.roundTripTimeVariance, true));
										base.CommandLogResize();
									}
									base.peerConnectionState = ConnectionStateValue.Zombie;
									base.Listener.OnStatusChanged(StatusCode.TimeoutDisconnect);
									this.Disconnect();
									return false;
								}
								commandsToResend.Enqueue(sentReliableCommand);
							}
						}
						while (commandsToResend.Count > 0)
						{
							NCommand command3 = commandsToResend.Dequeue();
							this.QueueOutgoingReliableCommand(command3);
							this.sentReliableCommands.Remove(command3);
							this.reliableCommandsRepeated++;
							if ((int)base.debugOut >= 3)
							{
								base.Listener.DebugReturn(DebugLevel.INFO, string.Format("Resending: {0}. times out after: {1} sent: {3} now: {2} rtt/var: {4}/{5} last recv: {6}", command3, command3.roundTripTimeout, base.timeInt, command3.commandSentTime, base.roundTripTime, base.roundTripTimeVariance, SupportClass.GetTickCount() - base.timestampOfLastReceive));
							}
						}
					}
				}
				if (!base.IsSendingOnlyAcks && base.peerConnectionState == ConnectionStateValue.Connected && base.timePingInterval > 0 && this.sentReliableCommands.Count == 0 && base.timeInt - base.timeLastAckReceive > base.timePingInterval && !this.AreReliableCommandsInTransit() && this.udpBufferIndex + 12 < this.udpBuffer.Length)
				{
					NCommand command3 = new NCommand(this, 5, null, 255);
					this.QueueOutgoingReliableCommand(command3);
					if (base.TrafficStatsEnabled)
					{
						base.TrafficStatsOutgoing.CountControlCommand(command3.Size);
					}
				}
				if (!base.IsSendingOnlyAcks)
				{
					lock (this.channels)
					{
						for (int index = 0; index < this.channelArray.Length; index++)
						{
							EnetChannel channel = this.channelArray[index];
							remainingCommands += this.SerializeToBuffer(channel.outgoingReliableCommandsList);
							remainingCommands += this.SerializeToBuffer(channel.outgoingUnreliableCommandsList);
						}
					}
				}
				if (this.udpCommandCount <= 0)
				{
					return false;
				}
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsOutgoing.TotalPacketCount++;
					base.TrafficStatsOutgoing.TotalCommandsInPackets += this.udpCommandCount;
				}
				this.SendData(this.udpBuffer, this.udpBufferIndex);
				return remainingCommands > 0;
			}
		}

		/// <summary>
		/// Checks if any channel has a outgoing reliable command.
		/// </summary>
		/// <returns>True if any channel has a outgoing reliable command. False otherwise.</returns>
		private bool AreReliableCommandsInTransit()
		{
			lock (this.channels)
			{
				foreach (EnetChannel value in this.channels.Values)
				{
					if (value.outgoingReliableCommandsList.Count > 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Checks connected state and channel before operation is serialized and enqueued for sending.
		/// </summary>
		/// <param name="parameters">operation parameters</param>
		/// <param name="opCode">code of operation</param>
		/// <param name="sendReliable">send as reliable command</param>
		/// <param name="channelId">channel (sequence) for command</param>
		/// <param name="encrypt">encrypt or not</param>
		/// <param name="messageType">usually EgMessageType.Operation</param>
		/// <returns>if operation could be enqueued</returns>
		internal override bool EnqueueOperation(Dictionary<byte, object> parameters, byte opCode, bool sendReliable, byte channelId, bool encrypt, EgMessageType messageType)
		{
			if (base.peerConnectionState != ConnectionStateValue.Connected)
			{
				if ((int)base.debugOut >= 1)
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, "Cannot send op: " + opCode + " Not connected. PeerState: " + base.peerConnectionState);
				}
				base.Listener.OnStatusChanged(StatusCode.SendError);
				return false;
			}
			if (channelId >= base.ChannelCount)
			{
				if ((int)base.debugOut >= 1)
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, "Cannot send op: Selected channel (" + channelId + ")>= channelCount (" + base.ChannelCount + ").");
				}
				base.Listener.OnStatusChanged(StatusCode.SendError);
				return false;
			}
			byte[] opBytes = this.SerializeOperationToMessage(opCode, parameters, messageType, encrypt);
			return this.CreateAndEnqueueCommand((byte)(sendReliable ? 6 : 7), opBytes, channelId);
		}

		/// <summary>reliable-udp-level function to send some byte[] to the server via un/reliable command</summary>
		/// <remarks>only called when a custom operation should be send</remarks>
		/// <param name="commandType">(enet) command type</param>
		/// <param name="payload">data to carry (operation)</param>
		/// <param name="channelNumber">channel in which to send</param>
		/// <returns>the invocation ID for this operation (the payload)</returns>
		internal bool CreateAndEnqueueCommand(byte commandType, byte[] payload, byte channelNumber)
		{
			if (payload == null)
			{
				return false;
			}
			EnetChannel channel = this.channels[channelNumber];
			base.ByteCountLastOperation = 0;
			int fragmentLength = base.mtu - 12 - 36;
			if (payload.Length > fragmentLength)
			{
				int fragmentCount = (payload.Length + fragmentLength - 1) / fragmentLength;
				int startSequenceNumber = channel.outgoingReliableSequenceNumber + 1;
				int fragmentNumber = 0;
				for (int fragmentOffset = 0; fragmentOffset < payload.Length; fragmentOffset += fragmentLength)
				{
					if (payload.Length - fragmentOffset < fragmentLength)
					{
						fragmentLength = payload.Length - fragmentOffset;
					}
					byte[] tmpPayload = new byte[fragmentLength];
					Buffer.BlockCopy(payload, fragmentOffset, tmpPayload, 0, fragmentLength);
					NCommand command2 = new NCommand(this, 8, tmpPayload, channel.ChannelNumber);
					command2.fragmentNumber = fragmentNumber;
					command2.startSequenceNumber = startSequenceNumber;
					command2.fragmentCount = fragmentCount;
					command2.totalLength = payload.Length;
					command2.fragmentOffset = fragmentOffset;
					this.QueueOutgoingReliableCommand(command2);
					base.ByteCountLastOperation += command2.Size;
					if (base.TrafficStatsEnabled)
					{
						base.TrafficStatsOutgoing.CountFragmentOpCommand(command2.Size);
						base.TrafficStatsGameLevel.CountOperation(command2.Size);
					}
					fragmentNumber++;
				}
			}
			else
			{
				NCommand command2 = new NCommand(this, commandType, payload, channel.ChannelNumber);
				if (command2.commandFlags == 1)
				{
					this.QueueOutgoingReliableCommand(command2);
					base.ByteCountLastOperation = command2.Size;
					if (base.TrafficStatsEnabled)
					{
						base.TrafficStatsOutgoing.CountReliableOpCommand(command2.Size);
						base.TrafficStatsGameLevel.CountOperation(command2.Size);
					}
				}
				else
				{
					this.QueueOutgoingUnreliableCommand(command2);
					base.ByteCountLastOperation = command2.Size;
					if (base.TrafficStatsEnabled)
					{
						base.TrafficStatsOutgoing.CountUnreliableOpCommand(command2.Size);
						base.TrafficStatsGameLevel.CountOperation(command2.Size);
					}
				}
			}
			return true;
		}

		/// <summary> Returns the UDP Payload starting with Magic Number for binary protocol </summary>
		internal override byte[] SerializeOperationToMessage(byte opc, Dictionary<byte, object> parameters, EgMessageType messageType, bool encrypt)
		{
			byte[] fullMessageBytes = default(byte[]);
			lock (base.SerializeMemStream)
			{
				base.SerializeMemStream.Position = 0L;
				base.SerializeMemStream.SetLength(0L);
				if (!encrypt)
				{
					base.SerializeMemStream.Write(EnetPeer.messageHeader, 0, EnetPeer.messageHeader.Length);
				}
				Protocol.SerializeOperationRequest(base.SerializeMemStream, opc, parameters, false);
				if (encrypt)
				{
					byte[] opBytes2 = base.SerializeMemStream.ToArray();
					opBytes2 = base.CryptoProvider.Encrypt(opBytes2);
					base.SerializeMemStream.Position = 0L;
					base.SerializeMemStream.SetLength(0L);
					base.SerializeMemStream.Write(EnetPeer.messageHeader, 0, EnetPeer.messageHeader.Length);
					base.SerializeMemStream.Write(opBytes2, 0, opBytes2.Length);
				}
				fullMessageBytes = base.SerializeMemStream.ToArray();
			}
			if (messageType != EgMessageType.Operation)
			{
				fullMessageBytes[EnetPeer.messageHeader.Length - 1] = (byte)messageType;
			}
			if (encrypt)
			{
				fullMessageBytes[EnetPeer.messageHeader.Length - 1] = (byte)(fullMessageBytes[EnetPeer.messageHeader.Length - 1] | 0x80);
			}
			return fullMessageBytes;
		}

		internal int SerializeToBuffer(Queue<NCommand> commandList)
		{
			while (commandList.Count > 0)
			{
				NCommand command = commandList.Peek();
				if (command == null)
				{
					commandList.Dequeue();
				}
				else
				{
					if (this.udpBufferIndex + command.Size > this.udpBuffer.Length)
					{
						if ((int)base.debugOut >= 3)
						{
							base.Listener.DebugReturn(DebugLevel.INFO, "UDP package is full. Commands in Package: " + this.udpCommandCount + ". Commands left in queue: " + commandList.Count);
						}
						break;
					}
					Buffer.BlockCopy(command.Serialize(), 0, this.udpBuffer, this.udpBufferIndex, command.Size);
					this.udpBufferIndex += command.Size;
					this.udpCommandCount++;
					if ((command.commandFlags & 1) > 0)
					{
						this.QueueSentCommand(command);
						if (base.CommandLog != null)
						{
							base.CommandLog.Enqueue(new CmdLogSentReliable(command, base.timeInt, base.roundTripTime, base.roundTripTimeVariance, false));
							base.CommandLogResize();
						}
					}
					commandList.Dequeue();
				}
			}
			return commandList.Count;
		}

		internal void SendData(byte[] data, int length)
		{
			try
			{
				int offset = 0;
				Protocol.Serialize(base.peerID, data, ref offset);
				data[2] = (byte)(base.crcEnabled ? 204 : 0);
				data[3] = this.udpCommandCount;
				offset = 4;
				Protocol.Serialize(base.timeInt, data, ref offset);
				Protocol.Serialize(this.challenge, data, ref offset);
				if (base.crcEnabled)
				{
					Protocol.Serialize(0, data, ref offset);
					uint crcValue = SupportClass.CalculateCrc(data, length);
					offset -= 4;
					Protocol.Serialize((int)crcValue, data, ref offset);
				}
				base.bytesOut += length;
				if (base.NetworkSimulationSettings.IsSimulationEnabled)
				{
					byte[] dataCopy = new byte[length];
					Buffer.BlockCopy(data, 0, dataCopy, 0, length);
					base.SendNetworkSimulated(delegate
					{
						base.rt.Send(dataCopy, length);
					});
				}
				else
				{
					base.rt.Send(data, length);
				}
			}
			catch (Exception ex)
			{
				if ((int)base.debugOut >= 1)
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, ex.ToString());
				}
				SupportClass.WriteStackTrace(ex);
			}
		}

		internal void QueueSentCommand(NCommand command)
		{
			command.commandSentTime = base.timeInt;
			command.commandSentCount++;
			if (command.roundTripTimeout == 0)
			{
				command.roundTripTimeout = base.roundTripTime + 4 * base.roundTripTimeVariance;
				command.timeoutTime = base.timeInt + base.DisconnectTimeout;
			}
			else if (command.commandSentCount > base.QuickResendAttempts + 1)
			{
				command.roundTripTimeout *= 2;
			}
			lock (this.sentReliableCommands)
			{
				if (this.sentReliableCommands.Count == 0)
				{
					int resendTime = command.commandSentTime + command.roundTripTimeout;
					if (resendTime < base.timeoutInt)
					{
						base.timeoutInt = resendTime;
					}
				}
				this.reliableCommandsSent++;
				this.sentReliableCommands.Add(command);
			}
			if (this.sentReliableCommands.Count >= base.warningSize && this.sentReliableCommands.Count % base.warningSize == 0)
			{
				base.Listener.OnStatusChanged(StatusCode.QueueSentWarning);
			}
		}

		internal void QueueOutgoingReliableCommand(NCommand command)
		{
			EnetChannel channel = this.channels[command.commandChannelID];
			lock (channel)
			{
				Queue<NCommand> outgoingReliableCommands = channel.outgoingReliableCommandsList;
				if (outgoingReliableCommands.Count >= base.warningSize && outgoingReliableCommands.Count % base.warningSize == 0)
				{
					base.Listener.OnStatusChanged(StatusCode.QueueOutgoingReliableWarning);
				}
				if (command.reliableSequenceNumber == 0)
				{
					command.reliableSequenceNumber = ++channel.outgoingReliableSequenceNumber;
				}
				outgoingReliableCommands.Enqueue(command);
			}
		}

		internal void QueueOutgoingUnreliableCommand(NCommand command)
		{
			Queue<NCommand> outgoingUnreliableCommands = this.channels[command.commandChannelID].outgoingUnreliableCommandsList;
			if (outgoingUnreliableCommands.Count >= base.warningSize && outgoingUnreliableCommands.Count % base.warningSize == 0)
			{
				base.Listener.OnStatusChanged(StatusCode.QueueOutgoingUnreliableWarning);
			}
			EnetChannel channel = this.channels[command.commandChannelID];
			command.reliableSequenceNumber = channel.outgoingReliableSequenceNumber;
			command.unreliableSequenceNumber = ++channel.outgoingUnreliableSequenceNumber;
			outgoingUnreliableCommands.Enqueue(command);
		}

		internal void QueueOutgoingAcknowledgement(NCommand command)
		{
			lock (this.outgoingAcknowledgementsList)
			{
				if (this.outgoingAcknowledgementsList.Count >= base.warningSize && this.outgoingAcknowledgementsList.Count % base.warningSize == 0)
				{
					base.Listener.OnStatusChanged(StatusCode.QueueOutgoingAcksWarning);
				}
				this.outgoingAcknowledgementsList.Enqueue(command);
			}
		}

		/// <summary>reads incoming udp-packages to create and queue incoming commands*</summary>
		internal override void ReceiveIncomingCommands(byte[] inBuff, int dataLength)
		{
			base.timestampOfLastReceive = SupportClass.GetTickCount();
			try
			{
				int readingOffset = 0;
				short peerID = default(short);
				Protocol.Deserialize(out peerID, inBuff, ref readingOffset);
				byte flags = inBuff[readingOffset++];
				byte commandCount = inBuff[readingOffset++];
				Protocol.Deserialize(out this.serverSentTime, inBuff, ref readingOffset);
				int inChallenge = default(int);
				Protocol.Deserialize(out inChallenge, inBuff, ref readingOffset);
				if (flags == 204)
				{
					int crc = default(int);
					Protocol.Deserialize(out crc, inBuff, ref readingOffset);
					base.bytesIn += 4L;
					readingOffset -= 4;
					Protocol.Serialize(0, inBuff, ref readingOffset);
					uint localCrc = SupportClass.CalculateCrc(inBuff, dataLength);
					if (crc != (int)localCrc)
					{
						base.packetLossByCrc++;
						if (base.peerConnectionState != 0 && (int)base.debugOut >= 3)
						{
							base.EnqueueDebugReturn(DebugLevel.INFO, string.Format("Ignored package due to wrong CRC. Incoming:  {0:X} Local: {1:X}", (uint)crc, localCrc));
						}
						goto end_IL_000c;
					}
				}
				base.bytesIn += 12L;
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsIncoming.TotalPacketCount++;
					base.TrafficStatsIncoming.TotalCommandsInPackets += commandCount;
				}
				if (commandCount > base.commandBufferSize || commandCount <= 0)
				{
					base.EnqueueDebugReturn(DebugLevel.ERROR, "too many/few incoming commands in package: " + commandCount + " > " + base.commandBufferSize);
				}
				if (inChallenge != this.challenge)
				{
					base.packetLossByChallenge++;
					if (base.peerConnectionState != 0 && (int)base.debugOut >= 5)
					{
						base.EnqueueDebugReturn(DebugLevel.ALL, "Info: Ignoring received package due to wrong challenge. Challenge in-package!=local:" + inChallenge + "!=" + this.challenge + " Commands in it: " + commandCount);
					}
				}
				else
				{
					base.timeInt = SupportClass.GetTickCount() - base.timeBase;
					for (int i = 0; i < commandCount; i++)
					{
						NCommand readCommand = new NCommand(this, inBuff, ref readingOffset);
						if (readCommand.commandType != 1)
						{
							base.EnqueueActionForDispatch(delegate
							{
								this.ExecuteCommand(readCommand);
							});
						}
						else
						{
							base.TrafficStatsIncoming.TimestampOfLastAck = SupportClass.GetTickCount();
							this.ExecuteCommand(readCommand);
						}
						if ((readCommand.commandFlags & 1) > 0)
						{
							if (base.InReliableLog != null)
							{
								base.InReliableLog.Enqueue(new CmdLogReceivedReliable(readCommand, base.timeInt, base.roundTripTime, base.roundTripTimeVariance, base.timeInt - base.timeLastSendOutgoing, base.timeInt - base.timeLastSendAck));
								base.CommandLogResize();
							}
							NCommand ackForCommand = NCommand.CreateAck(this, readCommand, this.serverSentTime);
							this.QueueOutgoingAcknowledgement(ackForCommand);
							if (base.TrafficStatsEnabled)
							{
								base.TrafficStatsIncoming.TimestampOfLastReliableCommand = SupportClass.GetTickCount();
								base.TrafficStatsOutgoing.CountControlCommand(ackForCommand.Size);
							}
						}
					}
				}
				end_IL_000c:;
			}
			catch (Exception ex)
			{
				if ((int)base.debugOut >= 1)
				{
					base.EnqueueDebugReturn(DebugLevel.ERROR, string.Format("Exception while reading commands from incoming data: {0}", ex));
				}
				SupportClass.WriteStackTrace(ex);
			}
		}

		internal bool ExecuteCommand(NCommand command)
		{
			bool success = true;
			switch (command.commandType)
			{
			case 2:
			case 5:
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsIncoming.CountControlCommand(command.Size);
				}
				break;
			case 4:
			{
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsIncoming.CountControlCommand(command.Size);
				}
				StatusCode reason = StatusCode.DisconnectByServer;
				if (command.reservedByte == 1)
				{
					reason = StatusCode.DisconnectByServerLogic;
				}
				else if (command.reservedByte == 3)
				{
					reason = StatusCode.DisconnectByServerUserLimit;
				}
				if ((int)base.debugOut >= 3)
				{
					base.Listener.DebugReturn(DebugLevel.INFO, "Server " + base.ServerAddress + " sent disconnect. PeerId: " + (ushort)base.peerID + " RTT/Variance:" + base.roundTripTime + "/" + base.roundTripTimeVariance + " reason byte: " + command.reservedByte);
				}
				base.peerConnectionState = ConnectionStateValue.Disconnecting;
				base.Listener.OnStatusChanged(reason);
				base.rt.Disconnect();
				base.peerConnectionState = ConnectionStateValue.Disconnected;
				base.Listener.OnStatusChanged(StatusCode.Disconnect);
				break;
			}
			case 1:
			{
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsIncoming.CountControlCommand(command.Size);
				}
				base.timeLastAckReceive = base.timeInt;
				base.lastRoundTripTime = base.timeInt - command.ackReceivedSentTime;
				NCommand removedCommand = this.RemoveSentReliableCommand(command.ackReceivedReliableSequenceNumber, command.commandChannelID);
				if (base.CommandLog != null)
				{
					base.CommandLog.Enqueue(new CmdLogReceivedAck(command, base.timeInt, base.roundTripTime, base.roundTripTimeVariance));
					base.CommandLogResize();
				}
				if (removedCommand != null)
				{
					if (removedCommand.commandType == 12)
					{
						if (base.lastRoundTripTime <= base.roundTripTime)
						{
							base.serverTimeOffset = this.serverSentTime + (base.lastRoundTripTime >> 1) - SupportClass.GetTickCount();
							base.serverTimeOffsetIsAvailable = true;
						}
						else
						{
							this.FetchServerTimestamp();
						}
					}
					else
					{
						base.UpdateRoundTripTimeAndVariance(base.lastRoundTripTime);
						if (removedCommand.commandType == 4 && base.peerConnectionState == ConnectionStateValue.Disconnecting)
						{
							if ((int)base.debugOut >= 3)
							{
								base.EnqueueDebugReturn(DebugLevel.INFO, "Received disconnect ACK by server");
							}
							base.EnqueueActionForDispatch(delegate
							{
								base.rt.Disconnect();
							});
						}
						else if (removedCommand.commandType == 2)
						{
							base.roundTripTime = base.lastRoundTripTime;
						}
					}
				}
				break;
			}
			case 6:
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsIncoming.CountReliableOpCommand(command.Size);
				}
				if (base.peerConnectionState == ConnectionStateValue.Connected)
				{
					success = this.QueueIncomingCommand(command);
				}
				break;
			case 7:
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsIncoming.CountUnreliableOpCommand(command.Size);
				}
				if (base.peerConnectionState == ConnectionStateValue.Connected)
				{
					success = this.QueueIncomingCommand(command);
				}
				break;
			case 8:
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsIncoming.CountFragmentOpCommand(command.Size);
				}
				if (base.peerConnectionState == ConnectionStateValue.Connected)
				{
					if (command.fragmentNumber > command.fragmentCount || command.fragmentOffset >= command.totalLength || command.fragmentOffset + command.Payload.Length > command.totalLength)
					{
						if ((int)base.debugOut >= 1)
						{
							base.Listener.DebugReturn(DebugLevel.ERROR, "Received fragment has bad size: " + command);
						}
					}
					else
					{
						success = this.QueueIncomingCommand(command);
						if (success)
						{
							EnetChannel channel = this.channels[command.commandChannelID];
							if (command.reliableSequenceNumber == command.startSequenceNumber)
							{
								command.fragmentsRemaining--;
								int fragmentSequenceNumber = command.startSequenceNumber + 1;
								while (command.fragmentsRemaining > 0 && fragmentSequenceNumber < command.startSequenceNumber + command.fragmentCount)
								{
									if (channel.ContainsReliableSequenceNumber(fragmentSequenceNumber++))
									{
										command.fragmentsRemaining--;
									}
								}
							}
							else if (channel.ContainsReliableSequenceNumber(command.startSequenceNumber))
							{
								NCommand startCmd = channel.FetchReliableSequenceNumber(command.startSequenceNumber);
								startCmd.fragmentsRemaining--;
							}
						}
					}
				}
				break;
			case 3:
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsIncoming.CountControlCommand(command.Size);
				}
				if (base.peerConnectionState == ConnectionStateValue.Connecting)
				{
					command = new NCommand(this, 6, this.initData, 0);
					this.QueueOutgoingReliableCommand(command);
					if (base.TrafficStatsEnabled)
					{
						base.TrafficStatsOutgoing.CountControlCommand(command.Size);
					}
					base.peerConnectionState = ConnectionStateValue.Connected;
				}
				break;
			}
			return success;
		}

		/// <summary>queues incoming commands in the correct order as either unreliable, reliable or unsequenced. return value determines if the command is queued / done.</summary>
		internal bool QueueIncomingCommand(NCommand command)
		{
			EnetChannel channel = default(EnetChannel);
			this.channels.TryGetValue(command.commandChannelID, out channel);
			if (channel == null)
			{
				if ((int)base.debugOut >= 1)
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, "Received command for non-existing channel: " + command.commandChannelID);
				}
				return false;
			}
			if ((int)base.debugOut >= 5)
			{
				base.Listener.DebugReturn(DebugLevel.ALL, "queueIncomingCommand() " + command + " channel seq# r/u: " + channel.incomingReliableSequenceNumber + "/" + channel.incomingUnreliableSequenceNumber);
			}
			if (command.commandFlags == 1)
			{
				if (command.reliableSequenceNumber <= channel.incomingReliableSequenceNumber)
				{
					if ((int)base.debugOut >= 3)
					{
						base.Listener.DebugReturn(DebugLevel.INFO, "incoming command " + command + " is old (not saving it). Dispatched incomingReliableSequenceNumber: " + channel.incomingReliableSequenceNumber);
					}
					return false;
				}
				if (channel.ContainsReliableSequenceNumber(command.reliableSequenceNumber))
				{
					if ((int)base.debugOut >= 3)
					{
						base.Listener.DebugReturn(DebugLevel.INFO, "Info: command was received before! Old/New: " + channel.FetchReliableSequenceNumber(command.reliableSequenceNumber) + "/" + command + " inReliableSeq#: " + channel.incomingReliableSequenceNumber);
					}
					return false;
				}
				if (channel.incomingReliableCommandsList.Count >= base.warningSize && channel.incomingReliableCommandsList.Count % base.warningSize == 0)
				{
					base.Listener.OnStatusChanged(StatusCode.QueueIncomingReliableWarning);
				}
				channel.incomingReliableCommandsList.Add(command.reliableSequenceNumber, command);
				return true;
			}
			if (command.commandFlags == 0)
			{
				if (command.reliableSequenceNumber < channel.incomingReliableSequenceNumber)
				{
					if ((int)base.debugOut >= 3)
					{
						base.Listener.DebugReturn(DebugLevel.INFO, "incoming reliable-seq# < Dispatched-rel-seq#. not saved.");
					}
					return true;
				}
				if (command.unreliableSequenceNumber <= channel.incomingUnreliableSequenceNumber)
				{
					if ((int)base.debugOut >= 3)
					{
						base.Listener.DebugReturn(DebugLevel.INFO, "incoming unreliable-seq# < Dispatched-unrel-seq#. not saved.");
					}
					return true;
				}
				if (channel.ContainsUnreliableSequenceNumber(command.unreliableSequenceNumber))
				{
					if ((int)base.debugOut >= 3)
					{
						base.Listener.DebugReturn(DebugLevel.INFO, "command was received before! Old/New: " + channel.incomingUnreliableCommandsList[command.unreliableSequenceNumber] + "/" + command);
					}
					return false;
				}
				if (channel.incomingUnreliableCommandsList.Count >= base.warningSize && channel.incomingUnreliableCommandsList.Count % base.warningSize == 0)
				{
					base.Listener.OnStatusChanged(StatusCode.QueueIncomingUnreliableWarning);
				}
				channel.incomingUnreliableCommandsList.Add(command.unreliableSequenceNumber, command);
				return true;
			}
			return false;
		}

		/// <summary>removes commands which are acknowledged*</summary>
		internal NCommand RemoveSentReliableCommand(int ackReceivedReliableSequenceNumber, int ackReceivedChannel)
		{
			NCommand found = null;
			lock (this.sentReliableCommands)
			{
				foreach (NCommand sentReliableCommand in this.sentReliableCommands)
				{
					if (sentReliableCommand != null && sentReliableCommand.reliableSequenceNumber == ackReceivedReliableSequenceNumber && sentReliableCommand.commandChannelID == ackReceivedChannel)
					{
						found = sentReliableCommand;
						break;
					}
				}
				if (found != null)
				{
					this.sentReliableCommands.Remove(found);
					if (this.sentReliableCommands.Count > 0)
					{
						base.timeoutInt = base.timeInt + 25;
					}
				}
				else if ((int)base.debugOut >= 5 && base.peerConnectionState != ConnectionStateValue.Connected && base.peerConnectionState != ConnectionStateValue.Disconnecting)
				{
					base.EnqueueDebugReturn(DebugLevel.ALL, string.Format("No sent command for ACK (Ch: {0} Sq#: {1}). PeerState: {2}.", ackReceivedReliableSequenceNumber, ackReceivedChannel, base.peerConnectionState));
				}
			}
			return found;
		}

		internal string CommandListToString(NCommand[] list)
		{
			if ((int)base.debugOut < 5)
			{
				return string.Empty;
			}
			StringBuilder tmp = new StringBuilder();
			for (int i = 0; i < list.Length; i++)
			{
				tmp.Append(i + "=");
				tmp.Append(list[i]);
				tmp.Append(" # ");
			}
			return tmp.ToString();
		}

        internal override bool EnqueueOperation(byte[] bytes, bool sendReliable, byte channelId, bool encryptede, EgMessageType messageType)
        {
            if (base.peerConnectionState != ConnectionStateValue.Connected)
            {
                if ((int)base.debugOut >= 1)
                {
                    base.Listener.DebugReturn(DebugLevel.ERROR, "Cannot send op: " + bytes[0].ToString() + " Not connected. PeerState: " + base.peerConnectionState);
                }
                base.Listener.OnStatusChanged(StatusCode.SendError);
                return false;
            }
            if (channelId >= base.ChannelCount)
            {
                if ((int)base.debugOut >= 1)
                {
                    base.Listener.DebugReturn(DebugLevel.ERROR, "Cannot send op: Selected channel (" + channelId + ")>= channelCount (" + base.ChannelCount + ").");
                }
                base.Listener.OnStatusChanged(StatusCode.SendError);
                return false;
            }
            return this.CreateAndEnqueueCommand((byte)(sendReliable ? 6 : 7), bytes, channelId);
        }
    }
}
