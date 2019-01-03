using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading;

namespace ExitGames.Client.Photon
{
	/// <summary>
	/// Internal class to encapsulate the network i/o functionality for the realtime libary.
	/// </summary>
	internal class SocketTcp : IPhotonSocket, IDisposable
	{
		private Socket sock;

		private readonly object syncer = new object();

		public SocketTcp(PeerBase npeer)
			: base(npeer)
		{
			if (base.ReportDebugOfLevel(DebugLevel.ALL))
			{
				base.Listener.DebugReturn(DebugLevel.ALL, "SocketTcp: TCP, DotNet, Unity.");
			}
			base.Protocol = ConnectionProtocol.Tcp;
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

		public override bool Disconnect()
		{
			if (base.ReportDebugOfLevel(DebugLevel.INFO))
			{
				base.EnqueueDebugReturn(DebugLevel.INFO, "SocketTcp.Disconnect()");
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

		/// <summary>
		/// used by TPeer*
		/// </summary>
		public override PhotonSocketError Send(byte[] data, int length)
		{
			if (!this.sock.Connected)
			{
				return PhotonSocketError.Skipped;
			}
			try
			{
				this.sock.Send(data);
			}
			catch (Exception e)
			{
				if (base.ReportDebugOfLevel(DebugLevel.ERROR))
				{
					base.EnqueueDebugReturn(DebugLevel.ERROR, "Cannot send to: " + base.ServerAddress + ". " + e.Message);
				}
				base.HandleException(StatusCode.Exception);
				return PhotonSocketError.Exception;
			}
			return PhotonSocketError.Success;
		}

		public override PhotonSocketError Receive(out byte[] data)
		{
			data = null;
			return PhotonSocketError.NoData;
		}

		public void DnsAndConnect()
		{
			try
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
				this.sock = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				this.sock.NoDelay = true;
				this.sock.Connect(ipAddress, base.ServerPort);
				base.State = PhotonSocketState.Connected;
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

		public void ReceiveLoop()
		{
			MemoryStream opCollectionStream = new MemoryStream(base.MTU);
			while (base.State == PhotonSocketState.Connected)
			{
				opCollectionStream.Position = 0L;
				opCollectionStream.SetLength(0L);
				try
				{
					int bytesRead = 0;
					int bytesReadThisTime3 = 0;
					byte[] inBuff2 = new byte[9];
					while (bytesRead < 9)
					{
						bytesReadThisTime3 = this.sock.Receive(inBuff2, bytesRead, 9 - bytesRead, SocketFlags.None);
						bytesRead += bytesReadThisTime3;
						if (bytesReadThisTime3 == 0)
						{
							throw new SocketException(10054);
						}
					}
					if (inBuff2[0] == 240)
					{
						base.HandleReceivedDatagram(inBuff2, inBuff2.Length, false);
					}
					else
					{
						int length2 = inBuff2[1] << 24 | inBuff2[2] << 16 | inBuff2[3] << 8 | inBuff2[4];
						if (base.peerBase.TrafficStatsEnabled)
						{
							if (inBuff2[5] == 0)
							{
								base.peerBase.TrafficStatsIncoming.CountReliableOpCommand(length2);
							}
							else
							{
								base.peerBase.TrafficStatsIncoming.CountUnreliableOpCommand(length2);
							}
						}
						if (base.ReportDebugOfLevel(DebugLevel.ALL))
						{
							base.EnqueueDebugReturn(DebugLevel.ALL, "message length: " + length2);
						}
						opCollectionStream.Write(inBuff2, 7, bytesRead - 7);
						bytesRead = 0;
						length2 -= 9;
						inBuff2 = new byte[length2];
						while (bytesRead < length2)
						{
							bytesReadThisTime3 = this.sock.Receive(inBuff2, bytesRead, length2 - bytesRead, SocketFlags.None);
							bytesRead += bytesReadThisTime3;
							if (bytesReadThisTime3 == 0)
							{
								throw new SocketException(10054);
							}
						}
						opCollectionStream.Write(inBuff2, 0, bytesRead);
						if (opCollectionStream.Length > 0)
						{
							base.HandleReceivedDatagram(opCollectionStream.ToArray(), (int)opCollectionStream.Length, false);
						}
						if (base.ReportDebugOfLevel(DebugLevel.ALL))
						{
							base.EnqueueDebugReturn(DebugLevel.ALL, "TCP < " + opCollectionStream.Length + ((opCollectionStream.Length == length2 + 2) ? " OK" : " BAD"));
						}
					}
				}
				catch (SocketException e2)
				{
					if (base.State != PhotonSocketState.Disconnecting && base.State != 0)
					{
						if (base.ReportDebugOfLevel(DebugLevel.ERROR))
						{
							base.EnqueueDebugReturn(DebugLevel.ERROR, "Receiving failed. SocketException: " + e2.SocketErrorCode);
						}
						if (e2.SocketErrorCode == SocketError.ConnectionReset || e2.SocketErrorCode == SocketError.ConnectionAborted)
						{
							base.HandleException(StatusCode.DisconnectByServer);
						}
						else
						{
							base.HandleException(StatusCode.ExceptionOnReceive);
						}
					}
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
