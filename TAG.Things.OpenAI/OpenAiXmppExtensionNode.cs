using System.Collections;
using System.Collections.Generic;
using Waher.Networking.Sniffers;
using Waher.Things.Attributes;
using Waher.Things.Xmpp;

namespace TAG.Things.OpenAI
{
	/// <summary>
	/// Abstract base class for OpenAI extension nodes.
	/// </summary>
	public abstract class OpenAiXmppExtensionNode : XmppExtensionNode, ISniffable
	{
		private readonly Sniffable sniffers = new Sniffable();

		/// <summary>
		/// Abstract base class for OpenAI extension nodes.
		/// </summary>
		public OpenAiXmppExtensionNode()
			: base()
		{
		}

		/// <summary>
		/// OpenAI API Key
		/// </summary>
		[Page(1, "OpenAI", 100)]
		[Header(2, "API Key:")]
		[ToolTip(3, "API Key used when communicating with the OpenAI API.")]
		public string ApiKey { get; set; }

		#region ISniffable

		public ISniffer[] Sniffers => this.sniffers.Sniffers;
		public bool HasSniffers => this.sniffers.HasSniffers;
		public void Add(ISniffer Sniffer) => this.sniffers.Add(Sniffer);
		public void AddRange(IEnumerable<ISniffer> Sniffers) => this.sniffers.AddRange(Sniffers);
		public bool Remove(ISniffer Sniffer) => this.sniffers.Remove(Sniffer);
		public IEnumerator<ISniffer> GetEnumerator() => this.sniffers.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.sniffers.GetEnumerator();

		#endregion
	}
}
