using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TAG.Networking.OpenAI;
using Waher.Content;
using Waher.Content.Html.Elements;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.IoTGateway;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Events;
using Waher.Runtime.Counters;
using Waher.Runtime.Language;
using Waher.Runtime.Temporary;
using Waher.Security;
using Waher.Things;
using Waher.Things.Attributes;
using DP = Waher.Things.DisplayableParameters;

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
		/// Gets displayable parameters.
		/// </summary>
		/// <param name="Language">Language to use.</param>
		/// <param name="Caller">Information about caller.</param>
		/// <returns>Set of displayable parameters.</returns>
		public override async Task<IEnumerable<DP.Parameter>> GetDisplayableParametersAsync(Language Language, RequestOrigin Caller)
		{
			LinkedList<DP.Parameter> Result = await base.GetDisplayableParametersAsync(Language, Caller) as LinkedList<DP.Parameter>;

			Result.AddLast(new DP.Int64Parameter("Rx", "Received", await RuntimeCounters.GetCount(this.NodeId + ".Rx")));
			Result.AddLast(new DP.Int64Parameter("Tx", "Sent", await RuntimeCounters.GetCount(this.NodeId + ".Tx")));

			return Result;
		}

		/// <summary>
		/// Registers the extension with an XMPP Client.
		/// </summary>
		/// <param name="Client">XMPP Client</param>
		public override Task RegisterExtension(XmppClient Client)
		{
			Client.OnChatMessage += this.Client_OnChatMessage;
			return base.RegisterExtension(Client);
		}

		/// <summary>
		/// Unregisters the extension from an XMPP Client.
		/// </summary>
		/// <param name="Client">XMPP Client</param>
		public override Task UnregisterExtension(XmppClient Client)
		{
			Client.OnChatMessage -= this.Client_OnChatMessage;
			return base.UnregisterExtension(Client);
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
					string Text = e.Body.Trim();
					if (string.IsNullOrEmpty(Text))
						return;

					string MessageId = Guid.NewGuid().ToString();
					await XmppClient.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, MessageId, e.From,
						string.Empty, "⧖", string.Empty, string.Empty, string.Empty, string.Empty, null, null);

					StringBuilder Xml = new StringBuilder();
					string ResponseText;

					Xml.Append("<replace id='");
					Xml.Append(MessageId);
					Xml.Append("' xmlns='urn:xmpp:message-correct:0'/>");
					Xml.Append("<muteDiff xmlns='http://waher.se/Schema/Editing.xsd'/>");

					Text = await ConvertTextIfSpeech(Client, Text);
					if (string.IsNullOrEmpty(Text))
						ResponseText = "?";
					else
					{
						string Hash = Hashes.ComputeSHA256HashString(Encoding.UTF8.GetBytes(this.ApiKey + Text + this.ImageSize.ToString()));
						string FileName = Path.Combine(Gateway.RootFolder, "OpenAI", Hash) + ".png";

						if (!File.Exists(FileName))
						{
							await RuntimeCounters.IncrementCounter(this.NodeId + ".Rx", Text.Length);
							await RuntimeCounters.IncrementCounter(this.NodeId + "." + e.FromBareJID.ToLower() + ".Rx", Text.Length);

							Uri ImageUri = await Client.CreateImage(Text, this.ImageSize, e.FromBareJID.ToLower());
							KeyValuePair<string, TemporaryStream> P = await InternetContent.GetTempStreamAsync(ImageUri);

							using (TemporaryStream f = P.Value)
							{
								int c = (int)Math.Min(int.MaxValue, f.Length);
								byte[] Bin = new byte[c];

								f.Position = 0;
								await f.ReadAsync(Bin, 0, c);

								await Resources.WriteAllBytesAsync(FileName, Bin);

								await RuntimeCounters.IncrementCounter(this.NodeId + ".Tx", c);
								await RuntimeCounters.IncrementCounter(this.NodeId + "." + e.FromBareJID.ToLower() + ".Tx", c);
							}
						}

						string ImageUrl = Gateway.GetUrl("/OpenAI/" + Hash + ".png");

						Xml.Append("<content xmlns=\"urn:xmpp:content\" type=\"text/markdown\">");
						Xml.Append("![");
						Xml.Append(MarkdownDocument.Encode(Text.Replace('\r', ' ').Replace('\n', ' ')));
						Xml.Append("](");
						Xml.Append(ImageUrl);

						switch (this.ImageSize)
						{
							case ImageSize.ImageSize256x256:
								Xml.Append(" 256 256");
								break;

							case ImageSize.ImageSize512x512:
								Xml.Append(" 512 512");
								break;

							case ImageSize.ImageSize1024x1024:
								Xml.Append(" 1024 1024");
								break;
						}

						Xml.Append(")</content>");

						Xml.Append("<html xmlns='http://jabber.org/protocol/xhtml-im'>");
						Xml.Append("<body xmlns='http://www.w3.org/1999/xhtml'>");
						Xml.Append("<img src='");
						Xml.Append(ImageUrl);

						switch (this.ImageSize)
						{
							case ImageSize.ImageSize256x256:
								Xml.Append("' width='256' height='256");
								break;

							case ImageSize.ImageSize512x512:
								Xml.Append("' width='512' height='512");
								break;

							case ImageSize.ImageSize1024x1024:
								Xml.Append("' width='1024' height='1024");
								break;
						}

						Xml.Append("' alt='");
						Xml.Append(XML.HtmlAttributeEncode(Text.Replace('\r', ' ').Replace('\n', ' ')));
						Xml.Append("'/></body></html>");

						Xml.Append("<x xmlns='jabber:x:oob'><url>");
						Xml.Append(XML.Encode(ImageUrl));
						Xml.Append("</url></x>");

						ResponseText = ImageUrl;
					}

					await XmppClient.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, e.From, Xml.ToString(),
						ResponseText, string.Empty, string.Empty, string.Empty, string.Empty, null, null);
				}
			}
			catch (Exception ex)
			{
				await XmppClient.SendChatMessage(e.From, ex.Message);
			}
		}

		/// <summary>
		/// Gets an URI to an image, generated by a textual description.
		/// </summary>
		/// <param name="Description">Text description.</param>
		/// <returns>Image URI</returns>
		public Task<Uri> GetImageUri(string Description)
		{
			return this.GetImageUri(Description, this.ImageSize);
		}

		/// <summary>
		/// Gets an URI to an image, generated by a textual description.
		/// </summary>
		/// <param name="Description">Text description.</param>
		/// <param name="Size">Desired image size.</param>
		/// <returns>Image URI</returns>
		public async Task<Uri> GetImageUri(string Description, ImageSize Size)
		{
			using (OpenAIClient Client = new OpenAIClient(this.ApiKey, this.Sniffers))
			{
				return await Client.CreateImage(Description, Size, string.Empty);
			}
		}

	}
}
