using System;
using System.Threading.Tasks;
using TAG.Networking.OpenAI;
using TAG.Networking.OpenAI.Messages;
using Waher.Networking.XMPP;
using Waher.Persistence;
using Waher.Runtime.Cache;
using Waher.Runtime.Language;
using Waher.Things.Attributes;

namespace TAG.Things.OpenAI
{
	/// <summary>
	/// Implements a bridge between XMPP and ChatGPT
	/// </summary>
	public class ChatGPTXmppBridge : OpenAiXmppExtensionNode
	{
		private static Cache<CaseInsensitiveString, ChatHistory> sessions =
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

			string Text = e.Body.Trim();
			if (string.IsNullOrEmpty(Text))
				return;

			try
			{
				if (!sessions.TryGetValue(e.FromBareJID, out ChatHistory Session))
				{
					Session = new ChatHistory(e.FromBareJID);
					sessions[e.FromBareJID] = Session;
				}

				// TODO: Check Audio
				// TODO: Check multi-format

				Session.Add(new UserMessage(e.Body), 2000);

				using (OpenAIClient Client = new OpenAIClient(this.ApiKey, this.Sniffers))
				{
					Message Response = await Client.ChatGPT(Session.User.LowerCase, Session.Messages);
					Session.Add(Response, 2000);

					XmppClient.SendChatMessage(e.From, Response.Content);
				}
			}
			catch (Exception ex)
			{
				XmppClient.SendChatMessage(e.From, ex.Message);
			}
		}

	}
}
