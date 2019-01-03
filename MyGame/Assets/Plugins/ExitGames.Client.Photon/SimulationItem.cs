using System.Diagnostics;

namespace ExitGames.Client.Photon
{
	/// <summary>
	/// A simulation item is an action that can be queued to simulate network lag.
	/// </summary>
	internal class SimulationItem
	{
		/// <summary>With this, the actual delay can be measured, compared to the intended lag.</summary>
		internal readonly Stopwatch stopw;

		/// <summary>Timestamp after which this item must be executed.</summary>
		public int TimeToExecute;

		/// <summary>Action to execute when the lag-time passed.</summary>
		public PeerBase.MyAction ActionToExecute;

		public int Delay
		{
			get;
			internal set;
		}

		/// <summary>Starts a new Stopwatch</summary>
		public SimulationItem()
		{
			this.stopw = new Stopwatch();
			this.stopw.Start();
		}
	}
}
