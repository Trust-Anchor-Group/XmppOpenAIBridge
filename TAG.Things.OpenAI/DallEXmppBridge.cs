using System;
using System.Threading.Tasks;
using TAG.Networking.OpenAI;
using Waher.Networking.XMPP;
using Waher.Runtime.Language;
using Waher.Things.Attributes;

namespace TAG.Things.OpenAI
{
	/// <summary>
	/// Implements a bridge between XMPP and DALL-E
	/// </summary>
	public class DallEXmppBridge : OpenAiXmppExtensionNode
	{
		/// <summary>
		/// Implements a bridge between XMPP and DALL-E
		/// </summary>
		public DallEXmppBridge()
			: base()
		{
		}

		/// <summary>
		/// Size of generated images.
		/// </summary>
		[Page(1, "OpenAI", 100)]
		[Header(8, "Image Size:")]
		[ToolTip(9, "Images generated will have this size.")]
		[Option(ImageSize.ImageSize256x256, "256x256")]
		[Option(ImageSize.ImageSize512x512, "512x512")]
		[Option(ImageSize.ImageSize1024x1024, "1024x1024")]
		public ImageSize ImageSize { get; set; }

		/// <summary>
		/// Gets the type name of the node.
		/// </summary>
		/// <param name="Language">Language to use.</param>
		/// <returns>Localized type node.</returns>
		public override Task<string> GetTypeNameAsync(Language Language)
		{
			return Language.GetStringAsync(typeof(OpenAiXmppExtensionNode), 7, "DALL-E-XMPP Bridge");
		}

		/// <summary>
		/// Registers the extension with an XMPP Client.
		/// </summary>
		/// <param name="Client">XMPP Client</param>
		public override Task RegisterExtension(XmppClient Client)
		{
			Client.OnChatMessage += this.Client_OnChatMessage;
			return Task.CompletedTask;
		}

		/// <summary>
		/// Unregisters the extension from an XMPP Client.
		/// </summary>
		/// <param name="Client">XMPP Client</param>
		public override Task UnregisterExtension(XmppClient Client)
		{
			Client.OnChatMessage -= this.Client_OnChatMessage;
			return Task.CompletedTask;
		}

		private async Task Client_OnChatMessage(object Sender, MessageEventArgs e)
		{
			if (!(Sender is XmppClient XmppClient))
				return;

			RosterItem Contact = XmppClient.GetRosterItem(e.FromBareJID);
			if (Contact is null ||
				(Contact.State != SubscriptionState.Both &&
				Contact.State != SubscriptionState.From))
			{
				return;
			}

			try
			{
				using (OpenAIClient Client = new OpenAIClient(this.ApiKey, this.Sniffers))
				{
					string Text = await ConvertTextIfSpeech(Client, e.Body);
					if (string.IsNullOrEmpty(Text))
						return;

					Uri ImageUri = await Client.CreateImage(e.FromBareJID.ToLower(), this.ImageSize, Text);

					XmppClient.SendChatMessage(e.From, ImageUri.ToString());
				}
			}
			catch (Exception ex)
			{
				XmppClient.SendChatMessage(e.From, ex.Message);
			}
		}

	}
}
