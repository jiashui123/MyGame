using System;
using System.Collections.Generic;
using System.IO;

namespace ExitGames.Client.Photon
{
	internal class TPeer : PeerBase
	{
		/// <summary>TCP "Package" header: 7 bytes</summary>
		internal const int TCP_HEADER_BYTES = 7;

		/// <summary>TCP "Message" header: 2 bytes</summary>
		internal const int MSG_HEADER_BYTES = 2;

		/// <summary>TCP header combined: 9 bytes</summary>
		public const int ALL_HEADER_BYTES = 9;

		private Queue<byte[]> incomingList = new Queue<byte[]>(32);

		internal List<byte[]> outgoingStream;

		private int lastPingResult;

		private byte[] pingRequest = new byte[5]
		{
			240,
			0,
			0,
			0,
			0
		};

		internal static readonly byte[] tcpFramedMessageHead = new byte[9]
		{
			251,
			0,
			0,
			0,
			0,
			0,
			0,
			243,
			2
		};

		internal static readonly byte[] tcpMsgHead = new byte[2]
		{
			243,
			2
		};

		internal byte[] messageHeader;

		/// <summary>Defines if the (TCP) socket implementation needs to do "framing".</summary>
		/// <remarks>The WebSocket protocol (e.g.) includes framing, so when that is used, we set DoFraming to false.</remarks>
		protected internal bool DoFraming = true;

		internal override int QueuedIncomingCommandsCount
		{
			get
			{
				return this.incomingList.Count;
			}
		}

		internal override int QueuedOutgoingCommandsCount
		{
			get
			{
				return base.outgoingCommandsInStream;
			}
		}

		internal TPeer()
		{
			PeerBase.peerCount++;
			base.InitOnce();
			base.TrafficPackageHeaderSize = 0;
		}

		internal TPeer(IPhotonPeerListener listener)
			: this()
		{
			base.Listener = listener;
		}

		internal override void InitPeerBase()
		{
			base.InitPeerBase();
			this.incomingList = new Queue<byte[]>(32);
		}

		internal override bool Connect(string serverAddress, string appID)
		{
			if (base.peerConnectionState != 0)
			{
				base.Listener.DebugReturn(DebugLevel.WARNING, "Connect() can't be called if peer is not Disconnected. Not connecting.");
				return false;
			}
			if ((int)base.debugOut >= 5)
			{
				base.Listener.DebugReturn(DebugLevel.ALL, "Connect()");
			}
			base.ServerAddress = serverAddress;
			this.InitPeerBase();
			this.outgoingStream = new List<byte[]>();
			if (appID == null)
			{
				appID = "LoadBalancing";
			}
			for (int i = 0; i < 32; i++)
			{
				base.INIT_BYTES[i + 9] = (byte)((i < appID.Length) ? ((byte)appID[i]) : 0);
			}
			if (base.SocketImplementation != null)
			{
				base.rt = (IPhotonSocket)Activator.CreateInstance(base.SocketImplementation, this);
			}
			else
			{
				base.rt = new SocketTcp(this);
			}
			if (base.rt == null)
			{
				base.Listener.DebugReturn(DebugLevel.ERROR, "Connect() failed, because SocketImplementation or socket was null. Set PhotonPeer.SocketImplementation before Connect(). SocketImplementation: " + base.SocketImplementation);
				return false;
			}
			this.messageHeader = (this.DoFraming ? TPeer.tcpFramedMessageHead : TPeer.tcpMsgHead);
			if (base.rt.Connect())
			{
				base.peerConnectionState = ConnectionStateValue.Connecting;
				this.EnqueueInit();
				this.SendOutgoingCommands();
				return true;
			}
			base.peerConnectionState = ConnectionStateValue.Disconnected;
			return false;
		}

		internal override void Disconnect()
		{
			if (base.peerConnectionState != 0 && base.peerConnectionState != ConnectionStateValue.Disconnecting)
			{
				if ((int)base.debugOut >= 5)
				{
					base.Listener.DebugReturn(DebugLevel.ALL, "TPeer.Disconnect()");
				}
				this.StopConnection();
			}
		}

		internal override void StopConnection()
		{
			base.peerConnectionState = ConnectionStateValue.Disconnecting;
			if (base.rt != null)
			{
				base.rt.Disconnect();
			}
			lock (this.incomingList)
			{
				this.incomingList.Clear();
			}
			base.peerConnectionState = ConnectionStateValue.Disconnected;
			base.Listener.OnStatusChanged(StatusCode.Disconnect);
		}

		internal override void FetchServerTimestamp()
		{
			if (base.peerConnectionState != ConnectionStateValue.Connected)
			{
				if ((int)base.debugOut >= 3)
				{
					base.Listener.DebugReturn(DebugLevel.INFO, "FetchServerTimestamp() was skipped, as the client is not connected. Current ConnectionState: " + base.peerConnectionState);
				}
				base.Listener.OnStatusChanged(StatusCode.SendError);
			}
			else
			{
				this.SendPing();
				base.serverTimeOffsetIsAvailable = false;
			}
		}

		private void EnqueueInit()
		{
			if (this.DoFraming)
			{
				MemoryStream bout = new MemoryStream(0);
				BinaryWriter bsw = new BinaryWriter(bout);
				byte[] tcpheader = new byte[7]
				{
					251,
					0,
					0,
					0,
					0,
					0,
					1
				};
				int offsetForLength = 1;
				Protocol.Serialize(base.INIT_BYTES.Length + tcpheader.Length, tcpheader, ref offsetForLength);
				bsw.Write(tcpheader);
				bsw.Write(base.INIT_BYTES);
				byte[] init = bout.ToArray();
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsOutgoing.TotalPacketCount++;
					base.TrafficStatsOutgoing.TotalCommandsInPackets++;
					base.TrafficStatsOutgoing.CountControlCommand(init.Length);
				}
				this.EnqueueMessageAsPayload(true, init, 0);
			}
		}

		/// <summary>
		/// Checks the incoming queue and Dispatches received data if possible. Returns if a Dispatch happened or
		/// not, which shows if more Dispatches might be needed.
		/// </summary>
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
						goto IL_0041;
					}
				}
				break;
				IL_0041:
				action();
			}
			byte[] payload = default(byte[]);
			lock (this.incomingList)
			{
				if (this.incomingList.Count <= 0)
				{
					return false;
				}
				payload = this.incomingList.Dequeue();
			}
			base.ByteCountCurrentDispatch = payload.Length + 3;
			return this.DeserializeMessageAndCallback(payload);
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
			base.timeInt = SupportClass.GetTickCount() - base.timeBase;
			base.timeLastSendOutgoing = base.timeInt;
			if (base.peerConnectionState == ConnectionStateValue.Connected && SupportClass.GetTickCount() - this.lastPingResult > base.timePingInterval)
			{
				this.SendPing();
			}
			lock (this.outgoingStream)
			{
				foreach (byte[] item in this.outgoingStream)
				{
					this.SendData(item);
				}
				this.outgoingStream.Clear();
				base.outgoingCommandsInStream = 0;
			}
			return false;
		}

		/// <summary>Sends a ping in intervals to keep connection alive (server will timeout connection if nothing is sent).</summary>
		/// <returns>Always false in this case (local queues are ignored. true would be: "call again to send remaining data").</returns>
		internal override bool SendAcksOnly()
		{
			if (base.rt == null || !base.rt.Connected)
			{
				return false;
			}
			base.timeInt = SupportClass.GetTickCount() - base.timeBase;
			base.timeLastSendOutgoing = base.timeInt;
			if (base.peerConnectionState == ConnectionStateValue.Connected && SupportClass.GetTickCount() - this.lastPingResult > base.timePingInterval)
			{
				this.SendPing();
			}
			return false;
		}

        internal override bool EnqueueOperation(byte[] bytes, bool sendReliable, byte channelId, bool encryptede, EgMessageType messageType)
        {
            if (base.peerConnectionState != ConnectionStateValue.Connected)
            {
                if ((int)base.debugOut >= 1)
                {
                    base.Listener.DebugReturn(DebugLevel.ERROR, "Cannot send op: " + bytes[0].ToString() + "! Not connected. PeerState: " + base.peerConnectionState);
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
            return this.EnqueueMessageAsPayload(sendReliable, bytes, channelId);
        }


        internal override bool EnqueueOperation(Dictionary<byte, object> parameters, byte opCode, bool sendReliable, byte channelId, bool encrypt, EgMessageType messageType)
		{
			if (base.peerConnectionState != ConnectionStateValue.Connected)
			{
				if ((int)base.debugOut >= 1)
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, "Cannot send op: " + opCode + "! Not connected. PeerState: " + base.peerConnectionState);
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
			return this.EnqueueMessageAsPayload(sendReliable, opBytes, channelId);
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
					base.SerializeMemStream.Write(this.messageHeader, 0, this.messageHeader.Length);
				}
				Protocol.SerializeOperationRequest(base.SerializeMemStream, opc, parameters, false);
				if (encrypt)
				{
					byte[] opBytes2 = base.SerializeMemStream.ToArray();
					opBytes2 = base.CryptoProvider.Encrypt(opBytes2);
					base.SerializeMemStream.Position = 0L;
					base.SerializeMemStream.SetLength(0L);
					base.SerializeMemStream.Write(this.messageHeader, 0, this.messageHeader.Length);
					base.SerializeMemStream.Write(opBytes2, 0, opBytes2.Length);
				}
				fullMessageBytes = base.SerializeMemStream.ToArray();
			}
			if (messageType != EgMessageType.Operation)
			{
				fullMessageBytes[this.messageHeader.Length - 1] = (byte)messageType;
			}
			if (encrypt)
			{
				fullMessageBytes[this.messageHeader.Length - 1] = (byte)(fullMessageBytes[this.messageHeader.Length - 1] | 0x80);
			}
			if (this.DoFraming)
			{
				int offsetForLength = 1;
				Protocol.Serialize(fullMessageBytes.Length, fullMessageBytes, ref offsetForLength);
			}
			return fullMessageBytes;
		}

		/// <summary>enqueues serialized operations to be sent as tcp stream / package</summary>
		internal bool EnqueueMessageAsPayload(bool sendReliable, byte[] opMessage, byte channelId)
		{
			if (opMessage == null)
			{
				return false;
			}
			if (this.DoFraming)
			{
				opMessage[5] = channelId;
				opMessage[6] = (byte)(sendReliable ? 1 : 0);
			}
			lock (this.outgoingStream)
			{
				this.outgoingStream.Add(opMessage);
				base.outgoingCommandsInStream++;
				if (base.outgoingCommandsInStream % base.warningSize == 0)
				{
					base.Listener.OnStatusChanged(StatusCode.QueueOutgoingReliableWarning);
				}
			}
			int payloadByteCount = base.ByteCountLastOperation = opMessage.Length;
			if (base.TrafficStatsEnabled)
			{
				if (sendReliable)
				{
					base.TrafficStatsOutgoing.CountReliableOpCommand(payloadByteCount);
				}
				else
				{
					base.TrafficStatsOutgoing.CountUnreliableOpCommand(payloadByteCount);
				}
				base.TrafficStatsGameLevel.CountOperation(payloadByteCount);
			}
			return true;
		}

		/// <summary>Sends a ping and modifies this.lastPingResult to avoid another ping for a while.</summary>
		internal void SendPing()
		{
			this.lastPingResult = SupportClass.GetTickCount();
			if (!this.DoFraming)
			{
				int timestamp = SupportClass.GetTickCount();
				this.EnqueueOperation(new Dictionary<byte, object>
				{
					{
						(byte)1,
						(object)timestamp
					}
				}, PhotonCodes.Ping, true, 0, false, EgMessageType.InternalOperationRequest);
			}
			else
			{
				int offset = 1;
				Protocol.Serialize(SupportClass.GetTickCount(), this.pingRequest, ref offset);
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsOutgoing.CountControlCommand(this.pingRequest.Length);
				}
				this.SendData(this.pingRequest);
			}
		}

		internal void SendData(byte[] data)
		{
			try
			{
				base.bytesOut += data.Length;
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsOutgoing.TotalPacketCount++;
					base.TrafficStatsOutgoing.TotalCommandsInPackets += base.outgoingCommandsInStream;
				}
				if (base.NetworkSimulationSettings.IsSimulationEnabled)
				{
					base.SendNetworkSimulated(delegate
					{
						base.rt.Send(data, data.Length);
					});
				}
				else
				{
					base.rt.Send(data, data.Length);
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

		/// <summary>reads incoming tcp-packages to create and queue incoming commands*</summary>
		internal override void ReceiveIncomingCommands(byte[] inbuff, int dataLength)
		{
			if (inbuff == null)
			{
				if ((int)base.debugOut >= 1)
				{
					base.EnqueueDebugReturn(DebugLevel.ERROR, "checkAndQueueIncomingCommands() inBuff: null");
				}
			}
			else
			{
				base.timestampOfLastReceive = SupportClass.GetTickCount();
				base.timeInt = SupportClass.GetTickCount() - base.timeBase;
				base.timeLastSendOutgoing = base.timeInt;
				base.bytesIn += inbuff.Length + 7;
				if (base.TrafficStatsEnabled)
				{
					base.TrafficStatsIncoming.TotalPacketCount++;
					base.TrafficStatsIncoming.TotalCommandsInPackets++;
				}
				if (inbuff[0] == 243 || inbuff[0] == 244)
				{
					lock (this.incomingList)
					{
						this.incomingList.Enqueue(inbuff);
						if (this.incomingList.Count % base.warningSize == 0)
						{
							base.EnqueueStatusCallback(StatusCode.QueueIncomingReliableWarning);
						}
					}
				}
				else if (inbuff[0] == 240)
				{
					base.TrafficStatsIncoming.CountControlCommand(inbuff.Length);
					this.ReadPingResult(inbuff);
				}
				else if ((int)base.debugOut >= 1)
				{
					base.EnqueueDebugReturn(DebugLevel.ERROR, "receiveIncomingCommands() MagicNumber should be 0xF0, 0xF3 or 0xF4. Is: " + inbuff[0]);
				}
			}
		}

		private void ReadPingResult(byte[] inbuff)
		{
			int serverSentTime = 0;
			int clientSentTime = 0;
			int offset = 1;
			Protocol.Deserialize(out serverSentTime, inbuff, ref offset);
			Protocol.Deserialize(out clientSentTime, inbuff, ref offset);
			base.lastRoundTripTime = SupportClass.GetTickCount() - clientSentTime;
			if (!base.serverTimeOffsetIsAvailable)
			{
				base.roundTripTime = base.lastRoundTripTime;
			}
			base.UpdateRoundTripTimeAndVariance(base.lastRoundTripTime);
			if (!base.serverTimeOffsetIsAvailable)
			{
				base.serverTimeOffset = serverSentTime + (base.lastRoundTripTime >> 1) - SupportClass.GetTickCount();
				base.serverTimeOffsetIsAvailable = true;
			}
		}

		protected internal void ReadPingResult(OperationResponse operationResponse)
		{
			int serverSentTime = (int)operationResponse.Parameters[2];
			int clientSentTime = (int)operationResponse.Parameters[1];
			base.lastRoundTripTime = SupportClass.GetTickCount() - clientSentTime;
			if (!base.serverTimeOffsetIsAvailable)
			{
				base.roundTripTime = base.lastRoundTripTime;
			}
			base.UpdateRoundTripTimeAndVariance(base.lastRoundTripTime);
			if (!base.serverTimeOffsetIsAvailable)
			{
				base.serverTimeOffset = serverSentTime + (base.lastRoundTripTime >> 1) - SupportClass.GetTickCount();
				base.serverTimeOffsetIsAvailable = true;
			}
		}
	}
}
