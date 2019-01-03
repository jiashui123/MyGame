using System;
using System.Net.Sockets;

namespace ExitGames.Client.Photon
{
	/// <summary>Uses C# Socket class from System.Net.Sockets (as Unity usually does).</summary>
	/// <remarks>Incompatible with Windows 8 Store/Phone API.</remarks>
	public class PingMono : PhotonPing
	{
		private Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

		public override bool StartPing(string ip)
		{
			base.Init();
			try
			{
				this.sock.ReceiveTimeout = 5000;
				this.sock.Connect(ip, 5055);
				base.PingBytes[base.PingBytes.Length - 1] = base.PingId;
				this.sock.Send(base.PingBytes);
				base.PingBytes[base.PingBytes.Length - 1] = (byte)(base.PingId - 1);
			}
			catch (Exception e)
			{
				this.sock = null;
				Console.WriteLine(e);
			}
			return false;
		}

		public override bool Done()
		{
			if (base.GotResult || this.sock == null)
			{
				return true;
			}
			if (this.sock.Available <= 0)
			{
				return false;
			}
			int read = this.sock.Receive(base.PingBytes, SocketFlags.None);
			if (base.PingBytes[base.PingBytes.Length - 1] != base.PingId || read != base.PingLength)
			{
				base.DebugString += " ReplyMatch is false! ";
			}
			base.Successful = (read == base.PingBytes.Length && base.PingBytes[base.PingBytes.Length - 1] == base.PingId);
			base.GotResult = true;
			return true;
		}

		public override void Dispose()
		{
			try
			{
				this.sock.Close();
			}
			catch
			{
			}
			this.sock = null;
		}
	}
}
