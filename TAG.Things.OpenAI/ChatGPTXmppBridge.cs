using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TAG.Networking.OpenAI;
using TAG.Networking.OpenAI.Messages;
using Waher.IoTGateway;
using Waher.Networking.XMPP;
using Waher.Persistence;
using Waher.Runtime.Cache;
using Waher.Runtime.Language;
using Waher.Things;
using Waher.Things.Attributes;
using DP = Waher.Things.DisplayableParameters;
using Waher.Runtime.Counters;

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
					string MessageId = Guid.NewGuid().ToString();
					bool First = true;
					StringBuilder Xml = new StringBuilder();
					DateTime Last = DateTime.MinValue;

					Message Response2 = await this.ChatQueryWithHistory(e.FromBareJID, Text, false,
						async (Sender2, e2) =>
						{
							await RuntimeCounters.IncrementCounter(this.NodeId + ".Tx", e2.Diff.Length);
							await RuntimeCounters.IncrementCounter(this.NodeId + "." + e.FromBareJID.ToLower() + ".Tx", e2.Diff.Length);

							DateTime Now = DateTime.Now;
							if (Now.Subtract(Last).TotalSeconds < 1)
								return;

							Last = Now;
							Xml.Clear();

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

							XmppClient.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, MessageId,
								e.From, Xml.ToString(), string.Empty, string.Empty, string.Empty, string.Empty,
								string.Empty, null, null);
						}, null);

					if (!(Response2 is null))
					{
						Xml.Clear();

						if (!First)
						{
							Xml.Append("<replace id='");
							Xml.Append(MessageId);
							Xml.Append("' xmlns='urn:xmpp:message-correct:0'/>");
						}

						Xml.Append("<active xmlns='http://jabber.org/protocol/chatstates'/>");
						Xml.Append(await Gateway.GetMultiFormatChatMessageXml(Response2.Content, true, true));

						XmppClient.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, MessageId,
							e.From, Xml.ToString(), string.Empty, string.Empty, string.Empty, string.Empty,
							string.Empty, null, null);
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
		/// Performs a chat completion query, maintaining a session history.
		/// </summary>
		/// <param name="From">Sender of query.</param>
		/// <param name="Text">Text to send.</param>
		/// <param name="ClearSession">If session should be restarted or not.</param>
		/// <param name="IntermediateResponseCallback">Callback method for intermediate callbacks.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		/// <returns>Response from OpenAI. Can be null, if text send is empty, or does not include text.</returns>
		public Task<Message> ChatQueryWithHistory(string From, string Text, bool ClearSession, 
			StreamEventHandler IntermediateResponseCallback, object State)
		{
			return this.ChatQueryWithHistory(From, Text, this.Instructions, ClearSession, IntermediateResponseCallback, State);
		}

		/// <summary>
		/// Performs a chat completion query, maintaining a session history.
		/// </summary>
		/// <param name="From">Sender of query.</param>
		/// <param name="Text">Text to send.</param>
		/// <param name="Instructions">Instructions for session.</param>
		/// <param name="ClearSession">If session should be restarted or not.</param>
		/// <param name="IntermediateResponseCallback">Callback method for intermediate callbacks.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		/// <returns>Response from OpenAI. Can be null, if text send is empty, or does not include text.</returns>
		public async Task<Message> ChatQueryWithHistory(string From, string Text, string Instructions, bool ClearSession,
			StreamEventHandler IntermediateResponseCallback, object State)
		{
			await RuntimeCounters.IncrementCounter(this.NodeId + ".Rx", Text.Length);
			await RuntimeCounters.IncrementCounter(this.NodeId + "." + From.ToLower() + ".Rx", Text.Length);

			using (OpenAIClient Client = new OpenAIClient(this.ApiKey, this.Sniffers))
			{
				Text = await ConvertTextIfSpeech(Client, Text);

				if (!string.IsNullOrEmpty(Text))
				{
					if (!sessions.TryGetValue(From, out ChatHistory Session) || 
						Session.Instructions != Instructions)
					{
						Session = new ChatHistory(From, Instructions);
						sessions[From] = Session;

						Session.Add(new SystemMessage(Instructions), 4000);
					}

					Session.Add(new UserMessage(Text), 4000);

					Message Response2 = await Client.ChatGPT(Session.User.LowerCase, Session.Messages,
						IntermediateResponseCallback, State);

					Session.Add(Response2, 4000);

					return Response2;
				}
				else
					return null;
			}
		}

		/// <summary>
		/// Gets generated text from a conversation. No intermediate session history is provided.
		/// </summary>
		/// <param name="Messages">Conversation</param>
		/// <returns>Generated message</returns>
		public async Task<Message> ChatCompletionNoHistory(params Message[] Messages)
		{
			using (OpenAIClient Client = new OpenAIClient(this.ApiKey, this.Sniffers))
			{
				return await Client.ChatGPT(Messages);
			}
		}

		/// <summary>
		/// Gets generated text from a text. No intermediate session history is provided.
		/// </summary>
		/// <param name="Messages">Textual message description</param>
		/// <returns>Generated message</returns>
		public async Task<string> ChatCompletionNoHistory(string Message)
		{
			Message Result = await this.ChatCompletionNoHistory(new UserMessage(Message));
			return Result.Content;
		}
	}
}
