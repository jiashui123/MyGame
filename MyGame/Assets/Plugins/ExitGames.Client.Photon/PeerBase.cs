using Photon.SocketServer.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace ExitGames.Client.Photon
{
	public abstract class PeerBase
	{
		internal delegate void MyAction();

		/// <summary>
		/// This is the replacement for the const values used in eNet like: PS_DISCONNECTED, PS_CONNECTED, etc.
		/// </summary>
		public enum ConnectionStateValue : byte
		{
			/// <summary>No connection is available. Use connect.</summary>
			Disconnected,
			/// <summary>Establishing a connection already. The app should wait for a status callback.</summary>
			Connecting,
			/// <summary>
			/// The low level connection with Photon is established. On connect, the library will automatically
			/// send an Init package to select the application it connects to (see also PhotonPeer.Connect()).
			/// When the Init is done, IPhotonPeerListener.OnStatusChanged() is called with connect.
			/// </summary>
			/// <remarks>Please note that calling operations is only possible after the OnStatusChanged() with StatusCode.Connect.</remarks>
			Connected = 3,
			/// <summary>Connection going to be ended. Wait for status callback.</summary>
			Disconnecting,
			/// <summary>Acknowledging a disconnect from Photon. Wait for status callback.</summary>
			AcknowledgingDisconnect,
			/// <summary>Connection not properly disconnected.</summary>
			Zombie
		}

		internal enum EgMessageType : byte
		{
			Init,
			InitResponse,
			Operation,
			OperationResponse,
			Event,
			InternalOperationRequest = 6,
			InternalOperationResponse,
			Message,
			RawMessage
		}

		public const string ClientVersion = "4.0.5.0";

		internal const int ENET_PEER_PACKET_LOSS_SCALE = 65536;

		internal const int ENET_PEER_DEFAULT_ROUND_TRIP_TIME = 300;

		internal const int ENET_PEER_PACKET_THROTTLE_INTERVAL = 5000;

		protected internal Type SocketImplementation = null;

		internal IPhotonSocket rt;

		public int ByteCountLastOperation;

		public int ByteCountCurrentDispatch;

		internal NCommand CommandInCurrentDispatch;

		internal int TrafficPackageHeaderSize;

		public TrafficStats TrafficStatsIncoming;

		public TrafficStats TrafficStatsOutgoing;

		public TrafficStatsGameLevel TrafficStatsGameLevel;

		private Stopwatch trafficStatsStopwatch;

		private bool trafficStatsEnabled = false;

		internal ConnectionProtocol usedProtocol;

		internal bool crcEnabled = false;

		internal int packetLossByCrc;

		internal int packetLossByChallenge;

		internal DebugLevel debugOut = DebugLevel.ERROR;

		internal readonly Queue<MyAction> ActionQueue = new Queue<MyAction>();

		/// This ID is assigned by the Realtime Server upon connection.
		/// The application does not have to care about this, but it is useful in debugging.
		internal short peerID = -1;

		/// <summary>
		/// This is the (low level) connection state of the peer. It's internal and based on eNet's states.
		/// </summary>
		/// <remarks>Applications can read the "high level" state as PhotonPeer.PeerState, which uses a different enum.</remarks>
		internal ConnectionStateValue peerConnectionState;

		/// <summary>
		/// The serverTimeOffset is serverTimestamp - localTime. Used to approximate the serverTimestamp with help of localTime
		/// </summary>
		internal int serverTimeOffset;

		internal bool serverTimeOffsetIsAvailable;

		internal int roundTripTime;

		internal int roundTripTimeVariance;

		internal int lastRoundTripTime;

		internal int lowestRoundTripTime;

		internal int lastRoundTripTimeVariance;

		internal int highestRoundTripTimeVariance;

		internal int timestampOfLastReceive;

		internal int packetThrottleInterval;

		internal static short peerCount;

		internal long bytesOut;

		internal long bytesIn;

		internal int commandBufferSize = 100;

		internal int warningSize = 100;

		internal int sentCountAllowance = 5;

		internal int DisconnectTimeout = 10000;

		internal int timePingInterval = 1000;

		internal byte ChannelCount = 2;

		internal int limitOfUnreliableCommands = 0;

		internal DiffieHellmanCryptoProvider CryptoProvider;

		private readonly Random lagRandomizer = new Random();

		internal readonly LinkedList<SimulationItem> NetSimListOutgoing = new LinkedList<SimulationItem>();

		internal readonly LinkedList<SimulationItem> NetSimListIncoming = new LinkedList<SimulationItem>();

		private readonly NetworkSimulationSet networkSimulationSettings = new NetworkSimulationSet();

		/// <summary>Size of CommandLog. Default is 0, no logging.</summary>
		internal int CommandLogSize = 0;

		/// <summary>Log of sent reliable commands and incoming ACKs.</summary>
		internal Queue<CmdLogItem> CommandLog;

		/// <summary>Log of incoming reliable commands, used to track which commands from the server this client got. Part of the PhotonPeer.CommandLogToString() result.</summary>
		internal Queue<CmdLogItem> InReliableLog;

		internal byte[] INIT_BYTES = new byte[41];

		internal int timeBase;

		internal int timeInt;

		internal int timeoutInt;

		internal int timeLastAckReceive;

		internal int timeLastSendAck;

		/// <summary>Set to timeInt, whenever SendOutgoingCommands actually checks outgoing queues to send them. Must be connected.</summary>
		internal int timeLastSendOutgoing;

		internal bool ApplicationIsInitialized;

		internal bool isEncryptionAvailable;

		internal static int outgoingStreamBufferSize = 1200;

		internal int outgoingCommandsInStream = 0;

		/// <summary> Maximum Transfer Unit to be used for UDP+TCP</summary>
		internal int mtu = 1200;

		/// <summary> (default=2) Rhttp: minimum number of open connections </summary>
		internal int rhttpMinConnections = 2;

		/// <summary> (default=6) Rhttp: maximum number of open connections, should be &gt; rhttpMinConnections </summary>
		internal int rhttpMaxConnections = 6;

		protected MemoryStream SerializeMemStream = new MemoryStream();

		public long TrafficStatsEnabledTime
		{
			get
			{
				return (this.trafficStatsStopwatch != null) ? this.trafficStatsStopwatch.ElapsedMilliseconds : 0;
			}
		}

		/// <summary>
		/// Enables or disables collection of statistics.
		/// Setting this to true, also starts the stopwatch to measure the timespan the stats are collected.
		/// </summary>
		public bool TrafficStatsEnabled
		{
			get
			{
				return this.trafficStatsEnabled;
			}
			set
			{
				this.trafficStatsEnabled = value;
				if (value)
				{
					if (this.trafficStatsStopwatch == null)
					{
						this.InitializeTrafficStats();
					}
					this.trafficStatsStopwatch.Start();
				}
				else if (this.trafficStatsStopwatch != null)
				{
					this.trafficStatsStopwatch.Stop();
				}
			}
		}

		public string ServerAddress
		{
			get;
			internal set;
		}

		internal string HttpUrlParameters
		{
			get;
			set;
		}

		internal IPhotonPeerListener Listener
		{
			get;
			set;
		}

		public byte QuickResendAttempts
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the currently used settings for the built-in network simulation.
		/// Please check the description of NetworkSimulationSet for more details.
		/// </summary>
		public NetworkSimulationSet NetworkSimulationSettings
		{
			get
			{
				return this.networkSimulationSettings;
			}
		}

		/// <summary>
		/// Count of all bytes going out (including headers)
		/// </summary>
		internal long BytesOut
		{
			get
			{
				return this.bytesOut;
			}
		}

		/// <summary>
		/// Count of all bytes coming in (including headers)
		/// </summary>
		internal long BytesIn
		{
			get
			{
				return this.bytesIn;
			}
		}

		internal abstract int QueuedIncomingCommandsCount
		{
			get;
		}

		internal abstract int QueuedOutgoingCommandsCount
		{
			get;
		}

		public virtual string PeerID
		{
			get
			{
				return ((ushort)this.peerID).ToString();
			}
		}

		protected internal byte[] TcpConnectionPrefix
		{
			get;
			set;
		}

		internal bool IsSendingOnlyAcks
		{
			get;
			set;
		}

		/// <summary>Reduce CommandLog to CommandLogSize. Oldest entries get discarded.</summary>
		internal void CommandLogResize()
		{
			if (this.CommandLogSize <= 0)
			{
				this.CommandLog = null;
				this.InReliableLog = null;
			}
			else
			{
				if (this.CommandLog == null || this.InReliableLog == null)
				{
					this.CommandLogInit();
				}
				while (this.CommandLog.Count > 0 && this.CommandLog.Count > this.CommandLogSize)
				{
					this.CommandLog.Dequeue();
				}
				while (this.InReliableLog.Count > 0 && this.InReliableLog.Count > this.CommandLogSize)
				{
					this.InReliableLog.Dequeue();
				}
			}
		}

		/// <summary>Initializes the CommandLog and InReliableLog according to CommandLogSize. A value of 0 will set both logs to 0.</summary>
		internal void CommandLogInit()
		{
			if (this.CommandLogSize <= 0)
			{
				this.CommandLog = null;
				this.InReliableLog = null;
			}
			else if (this.CommandLog == null || this.InReliableLog == null)
			{
				this.CommandLog = new Queue<CmdLogItem>(this.CommandLogSize);
				this.InReliableLog = new Queue<CmdLogItem>(this.CommandLogSize);
			}
			else
			{
				this.CommandLog.Clear();
				this.InReliableLog.Clear();
			}
		}

		/// <summary>Converts the CommandLog into a readable table-like string with summary.</summary>
		public string CommandLogToString()
		{
			StringBuilder sb = new StringBuilder();
			int resends = (this.usedProtocol == ConnectionProtocol.Udp) ? ((EnetPeer)this).reliableCommandsRepeated : 0;
			sb.AppendFormat("PeerId: {0} Now: {1} Server: {2} State: {3} Total Resends: {4} Received {5}ms ago.\n", this.PeerID, this.timeInt, this.ServerAddress, this.peerConnectionState, resends, SupportClass.GetTickCount() - this.timestampOfLastReceive);
			if (this.CommandLog == null)
			{
				return sb.ToString();
			}
			Queue<CmdLogItem>.Enumerator enumerator = this.CommandLog.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					CmdLogItem item2 = enumerator.Current;
					sb.AppendLine(item2.ToString());
				}
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			sb.AppendLine("Received Reliable Log: ");
			enumerator = this.InReliableLog.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					CmdLogItem item2 = enumerator.Current;
					sb.AppendLine(item2.ToString());
				}
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			return sb.ToString();
		}

		internal void InitOnce()
		{
			this.networkSimulationSettings.peerBase = this;
			this.INIT_BYTES[0] = 243;
			this.INIT_BYTES[1] = 0;
			this.INIT_BYTES[2] = 1;
			this.INIT_BYTES[3] = 6;
			this.INIT_BYTES[4] = 1;
			this.INIT_BYTES[5] = 4;
			this.INIT_BYTES[6] = 0;
			this.INIT_BYTES[7] = 5;
			this.INIT_BYTES[8] = 7;
		}

		/// <summary>Connect to server and send Init (which inlcudes the appId).</summary>
		internal abstract bool Connect(string serverAddress, string appID);

		private string GetHttpKeyValueString(Dictionary<string, string> dic)
		{
			StringBuilder sb = new StringBuilder();
			foreach (KeyValuePair<string, string> item in dic)
			{
				sb.Append(item.Key).Append("=").Append(item.Value)
					.Append("&");
			}
			return sb.ToString();
		}

		internal abstract void Disconnect();

		internal abstract void StopConnection();

		internal abstract void FetchServerTimestamp();

		internal bool EnqueueOperation(Dictionary<byte, object> parameters, byte opCode, bool sendReliable, byte channelId, bool encrypted)
		{
			return this.EnqueueOperation(parameters, opCode, sendReliable, channelId, encrypted, EgMessageType.Operation);
		}

        internal bool EnqueueOperation(byte[] bytes, bool sendReliable, byte channelID, bool encryptede)
        {
            return this.EnqueueOperation( bytes,  sendReliable,  channelID, encryptede, EgMessageType.Operation);
        }

		internal abstract bool EnqueueOperation(Dictionary<byte, object> parameters, byte opCode, bool sendReliable, byte channelId, bool encrypted, EgMessageType messageType);


        internal abstract bool EnqueueOperation(byte[] bytes, bool sendReliable, byte channelID, bool encryptede, EgMessageType messageType);

		/// <summary>
		/// Checks the incoming queue and Dispatches received data if possible.
		/// </summary>
		/// <returns>If a Dispatch happened or not, which shows if more Dispatches might be needed.</returns>
		internal abstract bool DispatchIncomingCommands();

		/// <summary>
		/// Checks outgoing queues for commands to send and puts them on their way.
		/// This creates one package per go in UDP.
		/// </summary>
		/// <returns>If commands are not sent, cause they didn't fit into the package that's sent.</returns>
		internal abstract bool SendOutgoingCommands();

		internal virtual bool SendAcksOnly()
		{
			return false;
		}

		/// <summary> Returns the UDP Payload starting with Magic Number for binary protocol </summary>
		internal byte[] SerializeMessageToMessage(object message, bool encrypt, byte[] messageHeader, bool writeLength = true)
		{
			byte[] fullMessageBytes = default(byte[]);
			lock (this.SerializeMemStream)
			{
				this.SerializeMemStream.Position = 0L;
				this.SerializeMemStream.SetLength(0L);
				if (!encrypt)
				{
					this.SerializeMemStream.Write(messageHeader, 0, messageHeader.Length);
				}
				Protocol.SerializeMessage(this.SerializeMemStream, message);
				if (encrypt)
				{
					byte[] opBytes2 = this.SerializeMemStream.ToArray();
					opBytes2 = this.CryptoProvider.Encrypt(opBytes2);
					this.SerializeMemStream.Position = 0L;
					this.SerializeMemStream.SetLength(0L);
					this.SerializeMemStream.Write(messageHeader, 0, messageHeader.Length);
					this.SerializeMemStream.Write(opBytes2, 0, opBytes2.Length);
				}
				fullMessageBytes = this.SerializeMemStream.ToArray();
			}
			fullMessageBytes[messageHeader.Length - 1] = 8;
			if (encrypt)
			{
				fullMessageBytes[messageHeader.Length - 1] = (byte)(fullMessageBytes[messageHeader.Length - 1] | 0x80);
			}
			if (writeLength)
			{
				int offsetForLength = 1;
				Protocol.Serialize(fullMessageBytes.Length, fullMessageBytes, ref offsetForLength);
			}
			return fullMessageBytes;
		}

		/// <summary> Returns the UDP Payload starting with Magic Number for binary protocol </summary>
		internal byte[] SerializeRawMessageToMessage(byte[] data, bool encrypt, byte[] messageHeader, bool writeLength = true)
		{
			byte[] fullMessageBytes = default(byte[]);
			lock (this.SerializeMemStream)
			{
				this.SerializeMemStream.Position = 0L;
				this.SerializeMemStream.SetLength(0L);
				if (!encrypt)
				{
					this.SerializeMemStream.Write(messageHeader, 0, messageHeader.Length);
				}
				this.SerializeMemStream.Write(data, 0, data.Length);
				if (encrypt)
				{
					byte[] opBytes2 = this.SerializeMemStream.ToArray();
					opBytes2 = this.CryptoProvider.Encrypt(opBytes2);
					this.SerializeMemStream.Position = 0L;
					this.SerializeMemStream.SetLength(0L);
					this.SerializeMemStream.Write(messageHeader, 0, messageHeader.Length);
					this.SerializeMemStream.Write(opBytes2, 0, opBytes2.Length);
				}
				fullMessageBytes = this.SerializeMemStream.ToArray();
			}
			fullMessageBytes[messageHeader.Length - 1] = 9;
			if (encrypt)
			{
				fullMessageBytes[messageHeader.Length - 1] = (byte)(fullMessageBytes[messageHeader.Length - 1] | 0x80);
			}
			if (writeLength)
			{
				int offsetForLength = 1;
				Protocol.Serialize(fullMessageBytes.Length, fullMessageBytes, ref offsetForLength);
			}
			return fullMessageBytes;
		}

		internal abstract byte[] SerializeOperationToMessage(byte opCode, Dictionary<byte, object> parameters, EgMessageType messageType, bool encrypt);

		internal abstract void ReceiveIncomingCommands(byte[] inBuff, int dataLength);

		internal void InitCallback()
		{
			if (this.peerConnectionState == ConnectionStateValue.Connecting)
			{
				this.peerConnectionState = ConnectionStateValue.Connected;
			}
			this.ApplicationIsInitialized = true;
			this.FetchServerTimestamp();
			this.Listener.OnStatusChanged(StatusCode.Connect);
		}

		/// <summary>
		/// Internally uses an operation to exchange encryption keys with the server.
		/// </summary>
		/// <returns>If the op could be sent.</returns>
		internal bool ExchangeKeysForEncryption()
		{
			this.isEncryptionAvailable = false;
			this.CryptoProvider = new DiffieHellmanCryptoProvider();
			Dictionary<byte, object> parameters = new Dictionary<byte, object>(1);
			parameters[PhotonCodes.ClientKey] = this.CryptoProvider.PublicKey;
			return this.EnqueueOperation(parameters, PhotonCodes.InitEncryption, true, 0, false, EgMessageType.InternalOperationRequest);
		}

		internal void DeriveSharedKey(OperationResponse operationResponse)
		{
			if (operationResponse.ReturnCode != 0)
			{
				this.EnqueueDebugReturn(DebugLevel.ERROR, "Establishing encryption keys failed. " + operationResponse.ToStringFull());
				this.EnqueueStatusCallback(StatusCode.EncryptionFailedToEstablish);
			}
			else
			{
				byte[] serverPublicKey = (byte[])operationResponse[PhotonCodes.ServerKey];
				if (serverPublicKey == null || serverPublicKey.Length == 0)
				{
					this.EnqueueDebugReturn(DebugLevel.ERROR, "Establishing encryption keys failed. Server's public key is null or empty. " + operationResponse.ToStringFull());
					this.EnqueueStatusCallback(StatusCode.EncryptionFailedToEstablish);
				}
				else
				{
					this.CryptoProvider.DeriveSharedKey(serverPublicKey);
					this.isEncryptionAvailable = true;
					this.EnqueueStatusCallback(StatusCode.EncryptionEstablished);
				}
			}
		}

		internal void EnqueueActionForDispatch(MyAction action)
		{
			lock (this.ActionQueue)
			{
				this.ActionQueue.Enqueue(action);
			}
		}

		internal void EnqueueDebugReturn(DebugLevel level, string debugReturn)
		{
			lock (this.ActionQueue)
			{
				this.ActionQueue.Enqueue(delegate
				{
					this.Listener.DebugReturn(level, debugReturn);
				});
			}
		}

		internal void EnqueueStatusCallback(StatusCode statusValue)
		{
			lock (this.ActionQueue)
			{
				this.ActionQueue.Enqueue(delegate
				{
					this.Listener.OnStatusChanged(statusValue);
				});
			}
		}

		internal virtual void InitPeerBase()
		{
			this.TrafficStatsIncoming = new TrafficStats(this.TrafficPackageHeaderSize);
			this.TrafficStatsOutgoing = new TrafficStats(this.TrafficPackageHeaderSize);
			this.TrafficStatsGameLevel = new TrafficStatsGameLevel();
			this.ByteCountLastOperation = 0;
			this.ByteCountCurrentDispatch = 0;
			this.bytesIn = 0L;
			this.bytesOut = 0L;
			this.packetLossByCrc = 0;
			this.packetLossByChallenge = 0;
			this.networkSimulationSettings.LostPackagesIn = 0;
			this.networkSimulationSettings.LostPackagesOut = 0;
			lock (this.NetSimListOutgoing)
			{
				this.NetSimListOutgoing.Clear();
			}
			lock (this.NetSimListIncoming)
			{
				this.NetSimListIncoming.Clear();
			}
			this.peerConnectionState = ConnectionStateValue.Disconnected;
			this.timeBase = SupportClass.GetTickCount();
			this.isEncryptionAvailable = false;
			this.ApplicationIsInitialized = false;
			this.roundTripTime = 300;
			this.roundTripTimeVariance = 0;
			this.packetThrottleInterval = 5000;
			this.serverTimeOffsetIsAvailable = false;
			this.serverTimeOffset = 0;
		}

        //photonServer用来解析byte数据的
        //188，
		internal virtual bool DeserializeMessageAndCallback(byte[] inBuff)
		{
            UnityEngine.Debug.Log("inBuff'lenght:" + inBuff.Length + "----" + inBuff[0] + "---====--" + inBuff[1]);
			if (inBuff.Length < 2)
			{
				if ((int)this.debugOut >= 1)
				{
					this.Listener.DebugReturn(DebugLevel.ERROR, "Incoming UDP data too short! " + inBuff.Length);
				}
				return false;
			}
			if (inBuff[0] != 243 && inBuff[0] != 253)
			{
				if ((int)this.debugOut >= 1)
				{
					this.Listener.DebugReturn(DebugLevel.ALL, "No regular operation UDP message: " + inBuff[0]);
				}
				return false;
			}
			byte msgType = (byte)(inBuff[1] & 0x7F);//0x7F = 0111 1111 ,1011 1100  = 0011 1100 = 60(10)
            bool isEncrypted = (inBuff[1] & 0x80) > 0;//0x80 = 1000 0000 ,1011 1100  > 0
            MemoryStream stream = null;
			if (msgType != 1)
			{
				try
				{
					if (isEncrypted && msgType != 60)
					{
						inBuff = this.CryptoProvider.Decrypt(inBuff, 2, inBuff.Length - 2);
						stream = new MemoryStream(inBuff);
					}
					else
					{
						stream = new MemoryStream(inBuff);
						stream.Seek(2L, SeekOrigin.Begin);
					}
				}
				catch (Exception ex)
				{
					if ((int)this.debugOut >= 1)
					{
						this.Listener.DebugReturn(DebugLevel.ERROR, ex.ToString());
					}
					SupportClass.WriteStackTrace(ex);
					return false;
				}
			}
			int timeBeforeCallback = 0;
			switch (msgType)
			{
			case 3:
			{
				OperationResponse opRes2 = Protocol.DeserializeOperationResponse(stream);
				if (this.TrafficStatsEnabled)
				{
					this.TrafficStatsGameLevel.CountResult(this.ByteCountCurrentDispatch);
					timeBeforeCallback = SupportClass.GetTickCount();
				}
				this.Listener.OnOperationResponse(opRes2);
				if (this.TrafficStatsEnabled)
				{
					this.TrafficStatsGameLevel.TimeForResponseCallback(opRes2.OperationCode, SupportClass.GetTickCount() - timeBeforeCallback);
				}
				break;
			}
			case 4:
			{
				EventData ev = Protocol.DeserializeEventData(stream);
				if (this.TrafficStatsEnabled)
				{
					this.TrafficStatsGameLevel.CountEvent(this.ByteCountCurrentDispatch);
					timeBeforeCallback = SupportClass.GetTickCount();
				}
				this.Listener.OnEvent(ev);
				if (this.TrafficStatsEnabled)
				{
					this.TrafficStatsGameLevel.TimeForEventCallback(ev.Code, SupportClass.GetTickCount() - timeBeforeCallback);
				}
				break;
			}
			case 1:
				this.InitCallback();
				break;
			case 7:
			{
				OperationResponse opRes2 = Protocol.DeserializeOperationResponse(stream);
				if (this.TrafficStatsEnabled)
				{
					this.TrafficStatsGameLevel.CountResult(this.ByteCountCurrentDispatch);
					timeBeforeCallback = SupportClass.GetTickCount();
				}
				if (opRes2.OperationCode == PhotonCodes.InitEncryption)
				{
					this.DeriveSharedKey(opRes2);
				}
				else if (opRes2.OperationCode == PhotonCodes.Ping)
				{
					TPeer peer = this as TPeer;
					if (peer != null)
					{
						peer.ReadPingResult(opRes2);
					}
					else
					{
						this.EnqueueDebugReturn(DebugLevel.ERROR, "Ping response not used. " + opRes2.ToStringFull());
					}
				}
				else
				{
					this.EnqueueDebugReturn(DebugLevel.ERROR, "Received unknown internal operation. " + opRes2.ToStringFull());
				}
				if (this.TrafficStatsEnabled)
				{
					this.TrafficStatsGameLevel.TimeForResponseCallback(opRes2.OperationCode, SupportClass.GetTickCount() - timeBeforeCallback);
				}
				break;
			}
                case 60:
                    {
                        this.Listener.OnReceive(inBuff);
                        break;
                    }
                   
			default:
				this.EnqueueDebugReturn(DebugLevel.ERROR, "unexpected msgType " + msgType);
				break;
			}
			return true;
		}

		internal void SendNetworkSimulated(MyAction sendAction)
		{
			if (!this.NetworkSimulationSettings.IsSimulationEnabled)
			{
				sendAction();
			}
			else if (this.usedProtocol == ConnectionProtocol.Udp && this.NetworkSimulationSettings.OutgoingLossPercentage > 0 && this.lagRandomizer.Next(101) < this.NetworkSimulationSettings.OutgoingLossPercentage)
			{
				this.networkSimulationSettings.LostPackagesOut++;
			}
			else
			{
				int jitter = (this.networkSimulationSettings.OutgoingJitter > 0) ? (this.lagRandomizer.Next(this.networkSimulationSettings.OutgoingJitter * 2) - this.networkSimulationSettings.OutgoingJitter) : 0;
				int delay = this.networkSimulationSettings.OutgoingLag + jitter;
				int timeToExecute = SupportClass.GetTickCount() + delay;
				SimulationItem simulationItem = new SimulationItem();
				simulationItem.ActionToExecute = sendAction;
				simulationItem.TimeToExecute = timeToExecute;
				simulationItem.Delay = delay;
				SimulationItem simItem = simulationItem;
				lock (this.NetSimListOutgoing)
				{
					if (this.NetSimListOutgoing.Count == 0 || this.usedProtocol == ConnectionProtocol.Tcp)
					{
						this.NetSimListOutgoing.AddLast(simItem);
					}
					else
					{
						LinkedListNode<SimulationItem> node = this.NetSimListOutgoing.First;
						while (node != null && node.Value.TimeToExecute < timeToExecute)
						{
							node = node.Next;
						}
						if (node == null)
						{
							this.NetSimListOutgoing.AddLast(simItem);
						}
						else
						{
							this.NetSimListOutgoing.AddBefore(node, simItem);
						}
					}
				}
			}
		}

		internal void ReceiveNetworkSimulated(MyAction receiveAction)
		{
			if (!this.networkSimulationSettings.IsSimulationEnabled)
			{
				receiveAction();
			}
			else if (this.usedProtocol == ConnectionProtocol.Udp && this.networkSimulationSettings.IncomingLossPercentage > 0 && this.lagRandomizer.Next(101) < this.networkSimulationSettings.IncomingLossPercentage)
			{
				this.networkSimulationSettings.LostPackagesIn++;
			}
			else
			{
				int jitter = (this.networkSimulationSettings.IncomingJitter > 0) ? (this.lagRandomizer.Next(this.networkSimulationSettings.IncomingJitter * 2) - this.networkSimulationSettings.IncomingJitter) : 0;
				int delay = this.networkSimulationSettings.IncomingLag + jitter;
				int timeToExecute = SupportClass.GetTickCount() + delay;
				SimulationItem simulationItem = new SimulationItem();
				simulationItem.ActionToExecute = receiveAction;
				simulationItem.TimeToExecute = timeToExecute;
				simulationItem.Delay = delay;
				SimulationItem simItem = simulationItem;
				lock (this.NetSimListIncoming)
				{
					if (this.NetSimListIncoming.Count == 0 || this.usedProtocol == ConnectionProtocol.Tcp)
					{
						this.NetSimListIncoming.AddLast(simItem);
					}
					else
					{
						LinkedListNode<SimulationItem> node = this.NetSimListIncoming.First;
						while (node != null && node.Value.TimeToExecute < timeToExecute)
						{
							node = node.Next;
						}
						if (node == null)
						{
							this.NetSimListIncoming.AddLast(simItem);
						}
						else
						{
							this.NetSimListIncoming.AddBefore(node, simItem);
						}
					}
				}
			}
		}

		/// <summary>
		/// Core of the Network Simulation, which is available in Debug builds.
		/// Called by a timer in intervals.
		/// </summary>
		protected internal void NetworkSimRun()
		{
			while (true)
			{
				bool flag = true;
				bool enabled = false;
				lock (this.networkSimulationSettings.NetSimManualResetEvent)
				{
					enabled = this.networkSimulationSettings.IsSimulationEnabled;
				}
				if (!enabled)
				{
					this.networkSimulationSettings.NetSimManualResetEvent.WaitOne();
				}
				else
				{
					lock (this.NetSimListIncoming)
					{
						SimulationItem item4 = null;
						while (this.NetSimListIncoming.First != null)
						{
							item4 = this.NetSimListIncoming.First.Value;
							if (item4.stopw.ElapsedMilliseconds < item4.Delay)
							{
								break;
							}
							item4.ActionToExecute();
							this.NetSimListIncoming.RemoveFirst();
						}
					}
					lock (this.NetSimListOutgoing)
					{
						SimulationItem item4 = null;
						while (this.NetSimListOutgoing.First != null)
						{
							item4 = this.NetSimListOutgoing.First.Value;
							if (item4.stopw.ElapsedMilliseconds < item4.Delay)
							{
								break;
							}
							item4.ActionToExecute();
							this.NetSimListOutgoing.RemoveFirst();
						}
					}
					Thread.Sleep(0);
				}
			}
		}

		internal void UpdateRoundTripTimeAndVariance(int lastRoundtripTime)
		{
			if (lastRoundtripTime >= 0)
			{
				this.roundTripTimeVariance -= this.roundTripTimeVariance / 4;
				if (lastRoundtripTime >= this.roundTripTime)
				{
					this.roundTripTime += (lastRoundtripTime - this.roundTripTime) / 8;
					this.roundTripTimeVariance += (lastRoundtripTime - this.roundTripTime) / 4;
				}
				else
				{
					this.roundTripTime += (lastRoundtripTime - this.roundTripTime) / 8;
					this.roundTripTimeVariance -= (lastRoundtripTime - this.roundTripTime) / 4;
				}
				if (this.roundTripTime < this.lowestRoundTripTime)
				{
					this.lowestRoundTripTime = this.roundTripTime;
				}
				if (this.roundTripTimeVariance > this.highestRoundTripTimeVariance)
				{
					this.highestRoundTripTimeVariance = this.roundTripTimeVariance;
				}
			}
		}

		internal void InitializeTrafficStats()
		{
			this.TrafficStatsIncoming = new TrafficStats(this.TrafficPackageHeaderSize);
			this.TrafficStatsOutgoing = new TrafficStats(this.TrafficPackageHeaderSize);
			this.TrafficStatsGameLevel = new TrafficStatsGameLevel();
			this.trafficStatsStopwatch = new Stopwatch();
		}
	}
}
