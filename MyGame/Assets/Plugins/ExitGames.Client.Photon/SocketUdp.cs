using System;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading;

namespace ExitGames.Client.Photon
{
	/// <summary> Internal class to encapsulate the network i/o functionality for the realtime libary.</summary>
	internal class SocketUdp : IPhotonSocket, IDisposable
	{
		private Socket sock;

		private readonly object syncer = new object();

		public SocketUdp(PeerBase npeer)
			: base(npeer)
		{
			if (base.ReportDebugOfLevel(DebugLevel.ALL))
			{
				base.Listener.DebugReturn(DebugLevel.ALL, "CSharpSocket: UDP, Unity3d.");
			}
			base.Protocol = ConnectionProtocol.Udp;
			base.PollReceive = false;
		}

		public void Dispose()
		{
			base.State = PhotonSocketState.Disconnecting;
			if (this.sock != null)
			{
				try
				{
					if (this.sock.Connected)
					{
						this.sock.Close();
					}
				}
				catch (Exception ex)
				{
					base.EnqueueDebugReturn(DebugLevel.INFO, "Exception in Dispose(): " + ex);
				}
			}
			this.sock = null;
			base.State = PhotonSocketState.Disconnected;
		}

		public override bool Connect()
		{
			lock (this.syncer)
			{
				if (!base.Connect())
				{
					return false;
				}
				base.State = PhotonSocketState.Connecting;
				Thread dns = new Thread(this.DnsAndConnect);
				dns.Name = "photon dns thread";
				dns.IsBackground = true;
				dns.Start();
				return true;
			}
		}

		public override bool Disconnect()
		{
			if (base.ReportDebugOfLevel(DebugLevel.INFO))
			{
				base.EnqueueDebugReturn(DebugLevel.INFO, "CSharpSocket.Disconnect()");
			}
			base.State = PhotonSocketState.Disconnecting;
			lock (this.syncer)
			{
				if (this.sock != null)
				{
					try
					{
						this.sock.Close();
					}
					catch (Exception ex)
					{
						base.EnqueueDebugReturn(DebugLevel.INFO, "Exception in Disconnect(): " + ex);
					}
					this.sock = null;
				}
			}
			base.State = PhotonSocketState.Disconnected;
			return true;
		}

		/// <summary>used by PhotonPeer*</summary>
		public override PhotonSocketError Send(byte[] data, int length)
		{
			lock (this.syncer)
			{
				if (this.sock == null || !this.sock.Connected)
				{
					return PhotonSocketError.Skipped;
				}
				try
				{
					this.sock.Send(data, 0, length, SocketFlags.None);
				}
				catch (Exception e)
				{
					if (base.ReportDebugOfLevel(DebugLevel.ERROR))
					{
						base.EnqueueDebugReturn(DebugLevel.ERROR, "Cannot send to: " + base.ServerAddress + ". " + e.Message);
					}
					return PhotonSocketError.Exception;
				}
			}
			return PhotonSocketError.Success;
		}

		public override PhotonSocketError Receive(out byte[] data)
		{
			data = null;
			return PhotonSocketError.NoData;
		}

		internal void DnsAndConnect()
		{
			try
			{
				lock (this.syncer)
				{
					IPAddress ipAddress = IPhotonSocket.GetIpAddress(base.ServerAddress);
					if (ipAddress == null)
					{
						throw new ArgumentException("Invalid IPAddress. Address: " + base.ServerAddress);
					}
					if (ipAddress.AddressFamily != AddressFamily.InterNetwork && ipAddress.AddressFamily != AddressFamily.InterNetworkV6)
					{
						throw new ArgumentException("AddressFamily '" + ipAddress.AddressFamily + "' not supported. Address: " + base.ServerAddress);
					}
					this.sock = new Socket(ipAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
					this.sock.Connect(ipAddress, base.ServerPort);
					base.State = PhotonSocketState.Connected;
				}
			}
			catch (SecurityException se2)
			{
				if (base.ReportDebugOfLevel(DebugLevel.ERROR))
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, "Connect() to '" + base.ServerAddress + "' failed: " + se2.ToString());
				}
				base.HandleException(StatusCode.SecurityExceptionOnConnect);
				return;
			}
			catch (Exception se)
			{
				if (base.ReportDebugOfLevel(DebugLevel.ERROR))
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, "Connect() to '" + base.ServerAddress + "' failed: " + se.ToString());
				}
				base.HandleException(StatusCode.ExceptionOnConnect);
				return;
			}
			Thread run = new Thread(this.ReceiveLoop);
			run.Name = "photon receive thread";
			run.IsBackground = true;
			run.Start();
		}

		/// <summary>Endless loop, run in Receive Thread.</summary>
		public void ReceiveLoop()
		{
			byte[] inBuffer = new byte[base.MTU];
			while (base.State == PhotonSocketState.Connected)
			{
				try
				{
					int read = this.sock.Receive(inBuffer);
					base.HandleReceivedDatagram(inBuffer, read, true);
				}
				catch (Exception e)
				{
					if (base.State != PhotonSocketState.Disconnecting && base.State != 0)
					{
						if (base.ReportDebugOfLevel(DebugLevel.ERROR))
						{
							base.EnqueueDebugReturn(DebugLevel.ERROR, "Receive issue. State: " + base.State + ". Server: '" + base.ServerAddress + "' Exception: " + e);
						}
						base.HandleException(StatusCode.ExceptionOnReceive);
					}
				}
			}
			this.Disconnect();
		}
	}
}
