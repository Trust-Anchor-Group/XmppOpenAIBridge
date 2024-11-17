using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAG.Networking.OpenAI;
using Waher.Content;
using Waher.Networking;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using Waher.Runtime.Inventory;
using Waher.Things;
using Waher.Things.Attributes;
using Waher.Things.Xmpp;

namespace TAG.Things.OpenAI
{
	/// <summary>
	/// Abstract base class for OpenAI extension nodes.
	/// </summary>
	public abstract class OpenAiXmppExtensionNode : XmppExtensionNode, ICommunicationLayer
	{
		private readonly CommunicationLayer communicationLayer = new CommunicationLayer(true);
		private readonly LinkedList<XmppClient> clients = new LinkedList<XmppClient>();

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
		[Masked]
		public string ApiKey { get; set; }

		#region ICommunicationLayer

		public ISniffer[] Sniffers => this.communicationLayer.Sniffers;
		public bool HasSniffers => this.communicationLayer.HasSniffers;
		public void Add(ISniffer Sniffer) => this.communicationLayer.Add(Sniffer);
		public void AddRange(IEnumerable<ISniffer> Sniffers) => this.communicationLayer.AddRange(Sniffers);
		public bool Remove(ISniffer Sniffer) => this.communicationLayer.Remove(Sniffer);
		public IEnumerator<ISniffer> GetEnumerator() => this.communicationLayer.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.communicationLayer.GetEnumerator();

		public bool DecoupledEvents => this.communicationLayer.DecoupledEvents;

		public Task ReceiveBinary(byte[] Data) => this.communicationLayer.ReceiveBinary(Data) ?? Task.CompletedTask;
		public Task TransmitBinary(byte[] Data) => this.communicationLayer.TransmitBinary(Data) ?? Task.CompletedTask;
		public Task ReceiveText(string Text) => this.communicationLayer.ReceiveText(Text) ?? Task.CompletedTask;
		public Task TransmitText(string Text) => this.communicationLayer.TransmitText(Text) ?? Task.CompletedTask;
		public Task Information(string Comment) => this.communicationLayer.Information(Comment) ?? Task.CompletedTask;
		public Task Warning(string Warning) => this.communicationLayer.Warning(Warning) ?? Task.CompletedTask;
		public Task Error(string Error) => this.communicationLayer.Error(Error) ?? Task.CompletedTask;
		public Task Exception(Exception Exception) => this.communicationLayer.Exception(Exception) ?? Task.CompletedTask;
		public Task Exception(string Exception) => this.communicationLayer.Exception(Exception) ?? Task.CompletedTask;
		public Task ReceiveBinary(DateTime Timestamp, byte[] Data) => this.communicationLayer.ReceiveBinary(Timestamp, Data) ?? Task.CompletedTask;
		public Task TransmitBinary(DateTime Timestamp, byte[] Data) => this.communicationLayer.TransmitBinary(Timestamp, Data) ?? Task.CompletedTask;
		public Task ReceiveText(DateTime Timestamp, string Text) => this.communicationLayer.ReceiveText(Timestamp, Text) ?? Task.CompletedTask;
		public Task TransmitText(DateTime Timestamp, string Text) => this.communicationLayer.TransmitText(Timestamp, Text) ?? Task.CompletedTask;
		public Task Information(DateTime Timestamp, string Comment) => this.communicationLayer.Information(Timestamp, Comment) ?? Task.CompletedTask;
		public Task Warning(DateTime Timestamp, string Warning) => this.communicationLayer.Warning(Timestamp, Warning) ?? Task.CompletedTask;
		public Task Error(DateTime Timestamp, string Error) => this.communicationLayer.Error(Timestamp, Error) ?? Task.CompletedTask;
		public Task Exception(DateTime Timestamp, string Exception) => this.communicationLayer.Exception(Timestamp, Exception) ?? Task.CompletedTask;
		public Task Exception(DateTime Timestamp, Exception Exception) => this.communicationLayer.Exception(Timestamp, Exception) ?? Task.CompletedTask;

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

			object Obj = await Header.HeadAsync(ParsedUri, null, null, 10000);
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

		/// <summary>
		/// Registers the extension with an XMPP Client.
		/// </summary>
		/// <param name="Client">XMPP Client</param>
		public override Task RegisterExtension(XmppClient Client)
		{
			this.clients.AddLast(Client);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Unregisters the extension from an XMPP Client.
		/// </summary>
		/// <param name="Client">XMPP Client</param>
		public override Task UnregisterExtension(XmppClient Client)
		{
			this.clients.Remove(Client);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Checks if the extension has been registered on an XMPP Client.
		/// </summary>
		/// <param name="Client">XMPP Client.</param>
		/// <returns>If the extension has been registered.</returns>
		public override bool IsRegisteredExtension(XmppClient Client)
		{
			return this.clients.Contains(Client);
		}

		/// <summary>
		/// Available command objects. If no commands are available, null is returned.
		/// </summary>
		public override Task<IEnumerable<ICommand>> Commands => this.GetCommands();

		public async Task<IEnumerable<ICommand>> GetCommands()
		{
			List<ICommand> Result = new List<ICommand>();

			Result.AddRange(await base.Commands);
			Result.Add(new ReportStatistics(this));

			return Result;
		}
	}
}
