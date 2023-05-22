﻿using System;
using System.Text;
using System.Threading.Tasks;
using TAG.Networking.OpenAI;
using TAG.Networking.OpenAI.Messages;
using Waher.IoTGateway;
using Waher.Networking.XMPP;
using Waher.Persistence;
using Waher.Runtime.Cache;
using Waher.Runtime.Language;
using Waher.Script.Functions.Strings;
using Waher.Things.Attributes;

namespace TAG.Things.OpenAI
{
	/// <summary>
	/// Implements a bridge between XMPP and ChatGPT
	/// </summary>
	public class ChatGPTXmppBridge : OpenAiXmppExtensionNode
	{
		private readonly static Cache<CaseInsensitiveString, ChatHistory> sessions =
			new Cache<CaseInsensitiveString, ChatHistory>(int.MaxValue, TimeSpan.FromDays(1), TimeSpan.FromMinutes(15));

		/// <summary>
		/// Implements a bridge between XMPP and ChatGPT
		/// </summary>
		public ChatGPTXmppBridge()
			: base()
		{
		}

		/// <summary>
		/// OpenAI API Key
		/// </summary>
		[Page(1, "OpenAI", 100)]
		[Header(5, "Instructions:")]
		[ToolTip(6, "Natural language discritpions to give OpenAI, instructing it what its role is.")]
		public string Instructions { get; set; }

		/// <summary>
		/// Gets the type name of the node.
		/// </summary>
		/// <param name="Language">Language to use.</param>
		/// <returns>Localized type node.</returns>
		public override Task<string> GetTypeNameAsync(Language Language)
		{
			return Language.GetStringAsync(typeof(OpenAiXmppExtensionNode), 4, "ChatGPT-XMPP Bridge");
		}

		/// <summary>
		/// Registers the extension with an XMPP Client.
		/// </summary>
		/// <param name="Client">XMPP Client</param>
		public override Task RegisterExtension(XmppClient Client)
		{
			Client.RegisterFeature("http://jabber.org/protocol/chatstates");

			Client.OnChatMessage += this.Client_OnChatMessage;
			
			return base.RegisterExtension(Client);
		}

		/// <summary>
		/// Unregisters the extension from an XMPP Client.
		/// </summary>
		/// <param name="Client">XMPP Client</param>
		public override Task UnregisterExtension(XmppClient Client)
		{
			Client.UnregisterFeature("http://jabber.org/protocol/chatstates");

			Client.OnChatMessage -= this.Client_OnChatMessage;

			return base.UnregisterExtension(Client);
		}

		private Task Client_OnChatMessage(object Sender, MessageEventArgs e)
		{
			if (!(Sender is XmppClient XmppClient))
				return Task.CompletedTask;

			RosterItem Contact = XmppClient.GetRosterItem(e.FromBareJID);
			if (Contact is null ||
				(Contact.State != SubscriptionState.Both &&
				Contact.State != SubscriptionState.From))
			{
				return Task.CompletedTask;
			}

			string Text = e.Body?.Trim();
			if (string.IsNullOrEmpty(Text))
				return Task.CompletedTask;

			Task _ = Task.Run(async () =>
			{
				try
				{
					using (OpenAIClient Client = new OpenAIClient(this.ApiKey, this.Sniffers))
					{
						Text = await ConvertTextIfSpeech(Client, Text);

						if (!string.IsNullOrEmpty(Text))
						{
							if (!sessions.TryGetValue(e.FromBareJID, out ChatHistory Session))
							{
								Session = new ChatHistory(e.FromBareJID);
								sessions[e.FromBareJID] = Session;

								Session.Add(new SystemMessage(this.Instructions), 2000);
							}

							Session.Add(new UserMessage(e.Body), 2000);

							string MessageId = Guid.NewGuid().ToString();
							bool First = true;

							Message Response2 = await Client.ChatGPT(Session.User.LowerCase, Session.Messages,
								async (Sender2, e2) =>
								{
									StringBuilder Xml = new StringBuilder();

									if (First)
										First = false;
									else
									{
										Xml.Append("<replace id='");
										Xml.Append(MessageId);
										Xml.Append("' xmlns='urn:xmpp:message-correct:0'/>");
									}

									if (e2.Finished)
										Xml.Append("<active xmlns='http://jabber.org/protocol/chatstates'/>");
									else
										Xml.Append("<composing xmlns='http://jabber.org/protocol/chatstates'/>");

									string Markdown = string.IsNullOrEmpty(e2.Total) ? "⧖" : e2.Total;
									Xml.Append(await Gateway.GetMultiFormatChatMessageXml(Markdown, true, true));

									XmppClient.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, e.From,
										Xml.ToString(), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null, null);
								}, null);

							Session.Add(Response2, 2000);
						}
					}
				}
				catch (Exception ex)
				{
					XmppClient.SendChatMessage(e.From, ex.Message);
				}
			});

			return Task.CompletedTask;
		}

		/// <summary>
		/// Gets generated text from a conversation.
		/// </summary>
		/// <param name="Messages">Conversation</param>
		/// <returns>Generated message</returns>
		public async Task<Message> GetText(params Message[] Messages)
		{
			using (OpenAIClient Client = new OpenAIClient(this.ApiKey, this.Sniffers))
			{
				return await Client.ChatGPT(Messages);
			}
		}

		/// <summary>
		/// Gets generated text from a text.
		/// </summary>
		/// <param name="Messages">Textual message description</param>
		/// <returns>Generated message</returns>
		public async Task<string> GetText(string Message)
		{
			Message Result = await this.GetText(new UserMessage(Message));
			return Result.Content;
		}
	}
}
