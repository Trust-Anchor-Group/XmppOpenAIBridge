using System;

namespace TAG.Networking.OpenAI
{
	/// <summary>
	/// Event arguments for stream callback events.
	/// </summary>
	public class StreamEventArgs : EventArgs
	{
		/// <summary>
		/// Event arguments for stream callback events.
		/// </summary>
		/// <param name="Total">Total message received.</param>
		/// <param name="Diff">Latest addition.</param>
		/// <param name="Finished">If response has finished.</param>
		/// <param name="State">State object passed in the original request.</param>
		public StreamEventArgs(string Total, string Diff, bool Finished, object State)
		{
			this.Total = Total;
			this.Diff = Diff ?? string.Empty;
			this.State = State;
			this.Finished = Finished;
		}

		/// <summary>
		/// Total message received.s
		/// </summary>
		public string Total { get; }

		/// <summary>
		/// Latest addition.
		/// </summary>
		public string Diff { get; }

		/// <summary>
		/// If response has finished.
		/// </summary>
		public bool Finished { get; }

		/// <summary>
		/// State object passed in the original request.
		/// </summary>
		public object State { get; }
	}
}
