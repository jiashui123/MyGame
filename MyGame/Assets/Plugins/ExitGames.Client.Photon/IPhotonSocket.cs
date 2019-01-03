using System;
using System.Net;
using System.Net.Sockets;

namespace ExitGames.Client.Photon
{
	public abstract class IPhotonSocket
	{
		internal PeerBase peerBase;

		public bool PollReceive;

		protected IPhotonPeerListener Listener
		{
			get
			{
				return this.peerBase.Listener;
			}
		}

		public ConnectionProtocol Protocol
		{
			get;
			protected set;
		}

		public PhotonSocketState State
		{
			get;
			protected set;
		}

		public string ServerAddress
		{
			get;
			protected set;
		}

		public int ServerPort
		{
			get;
			protected set;
		}

		public bool Connected
		{
			get
			{
				return this.State == PhotonSocketState.Connected;
			}
		}

		public int MTU
		{
			get
			{
				return this.peerBase.mtu;
			}
		}

		public IPhotonSocket(PeerBase peerBase)
		{
			if (peerBase == null)
			{
				throw new Exception("Can't init without peer");
			}
			this.peerBase = peerBase;
		}

		public virtual bool Connect()
		{
			if (this.State != 0)
			{
				if ((int)this.peerBase.debugOut >= 1)
				{
					this.peerBase.Listener.DebugReturn(DebugLevel.ERROR, "Connect() failed: connection in State: " + this.State);
				}
				return false;
			}
			if (this.peerBase == null || this.Protocol != this.peerBase.usedProtocol)
			{
				return false;
			}
			string host = default(string);
			ushort hostPort = default(ushort);
			if (!this.TryParseAddress(this.peerBase.ServerAddress, out host, out hostPort))
			{
				if ((int)this.peerBase.debugOut >= 1)
				{
					this.peerBase.Listener.DebugReturn(DebugLevel.ERROR, "Failed parsing address: " + this.peerBase.ServerAddress);
				}
				return false;
			}
			this.ServerAddress = host;
			this.ServerPort = hostPort;
			return true;
		}

		public abstract bool Disconnect();

		public abstract PhotonSocketError Send(byte[] data, int length);

		public abstract PhotonSocketError Receive(out byte[] data);

		public void HandleReceivedDatagram(byte[] inBuffer, int length, bool willBeReused)
		{
			if (this.peerBase.NetworkSimulationSettings.IsSimulationEnabled)
			{
				if (willBeReused)
				{
					byte[] inBufferCopy = new byte[length];
					Buffer.BlockCopy(inBuffer, 0, inBufferCopy, 0, length);
					this.peerBase.ReceiveNetworkSimulated(delegate
					{
						this.peerBase.ReceiveIncomingCommands(inBufferCopy, length);
					});
				}
				else
				{
					this.peerBase.ReceiveNetworkSimulated(delegate
					{
						this.peerBase.ReceiveIncomingCommands(inBuffer, length);
					});
				}
			}
			else
			{
				this.peerBase.ReceiveIncomingCommands(inBuffer, length);
			}
		}

		public bool ReportDebugOfLevel(DebugLevel levelOfMessage)
		{
			return (int)this.peerBase.debugOut >= (int)levelOfMessage;
		}

		public void EnqueueDebugReturn(DebugLevel debugLevel, string message)
		{
			this.peerBase.EnqueueDebugReturn(debugLevel, message);
		}

		protected internal void HandleException(StatusCode statusCode)
		{
			this.State = PhotonSocketState.Disconnecting;
			this.peerBase.EnqueueStatusCallback(statusCode);
			this.peerBase.EnqueueActionForDispatch(delegate
			{
				this.peerBase.Disconnect();
			});
		}

		/// <summary>
		/// Separates the given address into address (host name or IP) and port. Port must be included after colon!
		/// </summary>
		/// <remarks>
		/// This method expects any address to include a port. The final ':' in addressAndPort has to separate it.
		/// IPv6 addresses have multiple colons and <b>must use brackets</b> to separate address from port.
		///
		/// Examples:
		///     ns.exitgames.com:5058
		///     http://[2001:db8:1f70::999:de8:7648:6e8]:100/
		///     [2001:db8:1f70::999:de8:7648:6e8]:100
		/// See:
		///     http://serverfault.com/questions/205793/how-can-one-distinguish-the-host-and-the-port-in-an-ipv6-url
		/// </remarks>
		protected internal bool TryParseAddress(string addressAndPort, out string address, out ushort port)
		{
			address = string.Empty;
			port = 0;
			if (string.IsNullOrEmpty(addressAndPort))
			{
				return false;
			}
			int idx = addressAndPort.LastIndexOf(':');
			if (idx <= 0)
			{
				return false;
			}
			if (addressAndPort.IndexOf(':') != idx && (!addressAndPort.Contains("[") || !addressAndPort.Contains("]")))
			{
				return false;
			}
			address = addressAndPort.Substring(0, idx);
			string portString = addressAndPort.Substring(idx + 1);
			return ushort.TryParse(portString, out port);
		}

		protected internal static IPAddress GetIpAddress(string serverIp)
		{
			IPAddress ipAddress = null;
			if (IPAddress.TryParse(serverIp, out ipAddress))
			{
				return ipAddress;
			}
			IPHostEntry hostEntry = Dns.GetHostEntry(serverIp);
			IPAddress[] addresses = hostEntry.AddressList;
			IPAddress[] array = addresses;
			foreach (IPAddress ipA in array)
			{
				if (ipA.AddressFamily == AddressFamily.InterNetwork)
				{
					return ipA;
				}
			}
			array = addresses;
			foreach (IPAddress ipA in array)
			{
				if (ipA.AddressFamily == AddressFamily.InterNetworkV6)
				{
					return ipA;
				}
			}
			return null;
		}
	}
}
