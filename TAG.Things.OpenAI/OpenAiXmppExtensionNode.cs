using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAG.Networking.OpenAI;
using Waher.Content;
using Waher.Networking.Sniffers;
using Waher.Runtime.Inventory;
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

		/// <summary>
		/// Checks if text sent to the service is an URI that points to audio
		/// that can be downloaded. If so, the audio is converted to text using
		/// the Whisper API.
		/// </summary>
		/// <param name="Client">OpenAI client.</param>
		/// <param name="Text">Text sent.</param>
		/// <returns>Text, or converted text. If URI represents non-audio, null
		/// is returned.</returns>
		protected static async Task<string> ConvertTextIfSpeech(OpenAIClient Client, string Text)
		{
			Text = Text?.Trim();
			if (string.IsNullOrEmpty(Text))
				return string.Empty;

			if (!Uri.TryCreate(Text, UriKind.Absolute, out Uri ParsedUri))
				return Text;

			if (!InternetContent.CanHead(ParsedUri, out Grade _, out IContentHeader Header))
				return Text;

			object Obj = await Header.HeadAsync(ParsedUri, null, 10000);
			if (!(Obj is Dictionary<string, object> Headers))
				return null;

			if (!Headers.TryGetValue("Content-Type", out Obj) ||
				!(Obj is string ContentType) ||
				!ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			Text = await Client.Whisper(ParsedUri);

			return Text;
		}
	}
}
