using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TAG.Networking.OpenAI;
using TAG.Networking.OpenAI.Functions;
using TAG.Networking.OpenAI.Messages;
using Waher.Content;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.IoTGateway;
using Waher.Networking.XMPP;
using Waher.Persistence;
using Waher.Runtime.Cache;
using Waher.Runtime.Counters;
using Waher.Runtime.Language;
using Waher.Things;
using Waher.Things.Attributes;
using DP = Waher.Things.DisplayableParameters;

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

					Message Response2 = await this.ChatQueryWithHistory(e.FromBareJID, Text, chatFunctions, false,
						async (Sender2, e2) =>
						{
							if (string.IsNullOrEmpty(e2.Diff))
							{
								if (First)
								{
									First = false;
									Last = DateTime.Now;

									Xml.Clear();
									if (e2.Finished)
										Xml.Append("<active xmlns='http://jabber.org/protocol/chatstates'/>");
									else
										Xml.Append("<composing xmlns='http://jabber.org/protocol/chatstates'/>");

									string Markdown = string.IsNullOrEmpty(e2.Total) ? "⧖" : e2.Total;
									Xml.Append(await Gateway.GetMultiFormatChatMessageXml(Markdown, true, true));

									XmppClient.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, MessageId,
										e.From, Xml.ToString(), string.Empty, string.Empty, string.Empty, string.Empty,
										string.Empty, null, null);
								}
							}
							else
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
									Xml.Append("<muteDiff xmlns='http://waher.se/Schema/Editing.xsd'/>");
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
							}
						}, null);

					if (!(Response2 is null))
					{
						Xml.Clear();

						if (!First)
						{
							Xml.Append("<replace id='");
							Xml.Append(MessageId);
							Xml.Append("' xmlns='urn:xmpp:message-correct:0'/>");
							Xml.Append("<muteDiff xmlns='http://waher.se/Schema/Editing.xsd'/>");
						}

						Xml.Append("<active xmlns='http://jabber.org/protocol/chatstates'/>");
						Xml.Append(await Gateway.GetMultiFormatChatMessageXml(Response2.Content, true, true));

						XmppClient.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, MessageId,
							e.From, Xml.ToString(), string.Empty, string.Empty, string.Empty, string.Empty,
							string.Empty, null, null);

						if (!string.IsNullOrEmpty(Response2.FunctionName))
						{
							string Args = JSON.Encode(Response2.FunctionArguments, false);
							int c = Response2.FunctionName.Length + Args.Length;
							bool Processed = false;

							await RuntimeCounters.IncrementCounter(this.NodeId + ".Tx", c);
							await RuntimeCounters.IncrementCounter(this.NodeId + "." + e.FromBareJID.ToLower() + ".Tx", c);

							switch (Response2.FunctionName)
							{
								case "ShowImage":
									Processed = await this.ShowImage(XmppClient, e.FromBareJID, Response2.FunctionArguments);
									break;

								case "ShowImages":
									Processed = await this.ShowImages(XmppClient, e.FromBareJID, Response2.FunctionArguments);
									break;

								case "ShowVideo":
									Processed = await this.ShowVideo(XmppClient, e.FromBareJID, Response2.FunctionArguments);
									break;

								case "ShowYouTubeVideo":
									Processed = await this.ShowYouTubeVideo(XmppClient, e.FromBareJID, Response2.FunctionArguments);
									break;

								case "PlayAudio":
									Processed = await this.PlayAudio(XmppClient, e.FromBareJID, Response2.FunctionArguments);
									break;

								case "ShareLink":
									Processed = await this.ShareLink(XmppClient, e.FromBareJID, Response2.FunctionArguments);
									break;

								case "ShareLinks":
									Processed = await this.ShareLinks(XmppClient, e.FromBareJID, Response2.FunctionArguments);
									break;
							}

							if (!Processed)
							{
								StringBuilder Markdown = new StringBuilder();

								Markdown.AppendLine("```");
								Markdown.Append(Response2.FunctionName);
								Markdown.Append('(');
								Markdown.Append(JSON.Encode(Response2.FunctionArguments, true));
								Markdown.AppendLine(")");
								Markdown.AppendLine("```");

								Xml.Clear();
								Xml.Append(await Gateway.GetMultiFormatChatMessageXml(Response2.Content, true, true));

								XmppClient.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, string.Empty,
									e.From, Xml.ToString(), string.Empty, string.Empty, string.Empty, string.Empty,
									string.Empty, null, null);
							}
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

		private readonly static Function[] chatFunctions = new Function[]
		{
			new Function("ShowImage", "Displays an image to the user.",
				new StringParameter("Url", "URL to the image to show.", true),
				new IntegerParameter("Width","Width of image, in pixels.", false, 0, false, null, false),
				new IntegerParameter("Height","Height of image, in pixels.", false, 0, false, null, false),
				new StringParameter("Alt", "Alternative textual description of image, in cases the image cannot be shown.", false)),
			new Function("ShowImages", "Displays an array of images to the user.",
				new ArrayParameter("Images", "Array of images to show.", true,
					new ObjectParameter("Image", "Information about an image.", true,
						new StringParameter("Url", "URL to the image to show.", true),
						new IntegerParameter("Width","Width of image, in pixels.", false, 0, false, null, false),
						new IntegerParameter("Height","Height of image, in pixels.", false, 0, false, null, false),
						new StringParameter("Alt", "Alternative textual description of image, in cases the image cannot be shown.", false)))),
			new Function("ShowVideo", "Displays a video (not YouTube) to the user.",
				new StringParameter("Url", "URL to the video to show.", true),
				new StringParameter("Title", "A descriptive title of the video.", false),
				new IntegerParameter("Width","Width of video, in pixels.", false, 0, false, null, false),
				new IntegerParameter("Height","Height of video, in pixels.", false, 0, false, null, false)),
			new Function("ShowYouTubeVideo", "Displays a video (specifically YouTube videos) to the user.",
				new StringParameter("Url", "URL to the video to show.", true),
				new StringParameter("Title", "A descriptive title of the video.", false),
				new IntegerParameter("Width","Width of video, in pixels.", false, 0, false, null, false),
				new IntegerParameter("Height","Height of video, in pixels.", false, 0, false, null, false)),
			new Function("PlayAudio", "Plays audio to the user.",
				new StringParameter("Url", "URL to the audio to play.", true),
				new StringParameter("Title", "A descriptive title of the audio.", false)),
			new Function("ShareLink", "Shares a link with the user.",
				new StringParameter("Url", "URL to share.", true),
				new StringParameter("Title", "A descriptive title of the link.", false)),
			new Function("ShareLinks", "Shares a collection of links with the user.",
				new ArrayParameter("Links", "Array of Links to share with the user.", true,
					new ObjectParameter("Link", "Information about a link to share.", true,
						new StringParameter("Url", "URL to share.", true),
						new StringParameter("Title", "A descriptive title of the link.", false))))
		};

		private async Task<bool> ShowImage(XmppClient Client, string From, Dictionary<string, object> Arguments)
		{
			if (!Arguments.TryGetValue("Url", out object Obj) || !(Obj is string Url))
				return false;

			StringBuilder Markdown = new StringBuilder();
			Markdown.Append("![");

			if (Arguments.TryGetValue("Alt", out Obj) && Obj is string Alt)
				Markdown.Append(MarkdownDocument.Encode(Alt));

			Markdown.Append("](");
			Markdown.Append(Url);

			if (Arguments.TryGetValue("Width", out Obj) && Obj is int Width)
			{
				Markdown.Append(' ');
				Markdown.Append(Width.ToString());

				if (Arguments.TryGetValue("Height", out Obj) && Obj is int Height)
				{
					Markdown.Append(' ');
					Markdown.Append(Height.ToString());
				}
			}

			Markdown.Append(')');

			StringBuilder Xml = new StringBuilder();
			Xml.Append(await Gateway.GetMultiFormatChatMessageXml(Markdown.ToString(), true, true));

			Xml.Append("<x xmlns='jabber:x:oob'><url>");
			Xml.Append(XML.Encode(Url));
			Xml.Append("</url></x>");

			Client.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, string.Empty,
				From, Xml.ToString(), string.Empty, string.Empty, string.Empty, string.Empty,
				string.Empty, null, null);

			return true;
		}

		private async Task<bool> ShowImages(XmppClient Client, string From, Dictionary<string, object> Arguments)
		{
			if (!Arguments.TryGetValue("Images", out object Obj) || !(Obj is Array Images))
				return false;

			bool Result = false;

			foreach (object Item in Images)
			{
				if (Item is Dictionary<string, object> Image)
				{
					if (await this.ShowImage(Client, From, Image))
						Result = true;
				}
			}

			return Result;
		}

		private async Task<bool> ShowVideo(XmppClient Client, string From, Dictionary<string, object> Arguments)
		{
			if (!Arguments.TryGetValue("Url", out object Obj) || !(Obj is string Url))
				return false;

			StringBuilder Markdown = new StringBuilder();
			Markdown.Append("![");

			if (Arguments.TryGetValue("Title", out Obj) && Obj is string Title)
				Markdown.Append(MarkdownDocument.Encode(Title));

			Markdown.Append("](");
			Markdown.Append(Url);

			if (Arguments.TryGetValue("Width", out Obj) && Obj is int Width)
			{
				Markdown.Append(' ');
				Markdown.Append(Width.ToString());

				if (Arguments.TryGetValue("Height", out Obj) && Obj is int Height)
				{
					Markdown.Append(' ');
					Markdown.Append(Height.ToString());
				}
			}

			Markdown.Append(')');

			StringBuilder Xml = new StringBuilder();
			Xml.Append(await Gateway.GetMultiFormatChatMessageXml(Markdown.ToString(), true, true));

			Xml.Append("<x xmlns='jabber:x:oob'><url>");
			Xml.Append(XML.Encode(Url));
			Xml.Append("</url></x>");

			Client.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, string.Empty,
				From, Xml.ToString(), string.Empty, string.Empty, string.Empty, string.Empty,
				string.Empty, null, null);

			return true;
		}

		private async Task<bool> ShowYouTubeVideo(XmppClient Client, string From, Dictionary<string, object> Arguments)
		{
			if (!Arguments.TryGetValue("Url", out object Obj) || !(Obj is string Url))
				return false;

			StringBuilder Markdown = new StringBuilder();
			Markdown.Append("![");

			if (Arguments.TryGetValue("Title", out Obj) && Obj is string Title)
				Markdown.Append(MarkdownDocument.Encode(Title));

			Markdown.Append("](");
			Markdown.Append(Url);

			if (Arguments.TryGetValue("Width", out Obj) && Obj is int Width)
			{
				Markdown.Append(' ');
				Markdown.Append(Width.ToString());

				if (Arguments.TryGetValue("Height", out Obj) && Obj is int Height)
				{
					Markdown.Append(' ');
					Markdown.Append(Height.ToString());
				}
			}
			else
				Markdown.Append(" 800 400");

			Markdown.Append(')');

			StringBuilder Xml = new StringBuilder();
			Xml.Append(await Gateway.GetMultiFormatChatMessageXml(Markdown.ToString(), true, true));

			Xml.Append("<x xmlns='jabber:x:oob'><url>");
			Xml.Append(XML.Encode(Url));
			Xml.Append("</url></x>");

			Client.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, string.Empty,
				From, Xml.ToString(), string.Empty, string.Empty, string.Empty, string.Empty,
				string.Empty, null, null);

			return true;
		}

		private async Task<bool> PlayAudio(XmppClient Client, string From, Dictionary<string, object> Arguments)
		{
			if (!Arguments.TryGetValue("Url", out object Obj) || !(Obj is string Url))
				return false;

			StringBuilder Markdown = new StringBuilder();
			Markdown.Append("![");

			if (Arguments.TryGetValue("Title", out Obj) && Obj is string Title)
				Markdown.Append(MarkdownDocument.Encode(Title));

			Markdown.Append("](");
			Markdown.Append(Url);
			Markdown.Append(')');

			StringBuilder Xml = new StringBuilder();
			Xml.Append(await Gateway.GetMultiFormatChatMessageXml(Markdown.ToString(), true, true));

			Xml.Append("<x xmlns='jabber:x:oob'><url>");
			Xml.Append(XML.Encode(Url));
			Xml.Append("</url></x>");

			Client.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, string.Empty,
				From, Xml.ToString(), string.Empty, string.Empty, string.Empty, string.Empty,
				string.Empty, null, null);

			return true;
		}

		private async Task<bool> ShareLink(XmppClient Client, string From, Dictionary<string, object> Arguments)
		{
			if (!Arguments.TryGetValue("Url", out object Obj) || !(Obj is string Url))
				return false;

			StringBuilder Markdown = new StringBuilder();

			if (Arguments.TryGetValue("Title", out Obj) && Obj is string Title)
			{
				Markdown.Append('[');
				Markdown.Append(MarkdownDocument.Encode(Title));
				Markdown.Append("](");
				Markdown.Append(Url);
				Markdown.Append(')');
			}
			else
			{
				Markdown.Append('<');
				Markdown.Append(Url);
				Markdown.Append('>');
			}

			StringBuilder Xml = new StringBuilder();
			Xml.Append(await Gateway.GetMultiFormatChatMessageXml(Markdown.ToString(), true, true));

			Xml.Append("<x xmlns='jabber:x:oob'><url>");
			Xml.Append(XML.Encode(Url));
			Xml.Append("</url></x>");

			Client.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, string.Empty,
				From, Xml.ToString(), string.Empty, string.Empty, string.Empty, string.Empty,
				string.Empty, null, null);

			return true;
		}

		private async Task<bool> ShareLinks(XmppClient Client, string From, Dictionary<string, object> Arguments)
		{
			if (!Arguments.TryGetValue("Links", out object Obj) || !(Obj is Array Links))
				return false;

			bool Result = false;

			foreach (object Item in Links)
			{
				if (Item is Dictionary<string, object> Link)
				{
					if (await this.ShareLink(Client, From, Link))
						Result = true;
				}
			}

			return Result;
		}

		/// <summary>
		/// Performs a chat completion query, maintaining a session history.
		/// </summary>
		/// <param name="From">Sender of query.</param>
		/// <param name="Text">Text to send.</param>
		/// <param name="Functions">Function definitions OpenAI can call.</param>
		/// <param name="ClearSession">If session should be restarted or not.</param>
		/// <param name="IntermediateResponseCallback">Callback method for intermediate callbacks.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		/// <returns>Response from OpenAI. Can be null, if text send is empty, or does not include text.</returns>
		public Task<Message> ChatQueryWithHistory(string From, string Text, Function[] Functions, bool ClearSession,
			StreamEventHandler IntermediateResponseCallback, object State)
		{
			return this.ChatQueryWithHistory(From, Text, this.Instructions, Functions, ClearSession,
				IntermediateResponseCallback, State);
		}

		/// <summary>
		/// Performs a chat completion query, maintaining a session history.
		/// </summary>
		/// <param name="From">Sender of query.</param>
		/// <param name="Text">Text to send.</param>
		/// <param name="Functions">Function definitions OpenAI can call.</param>
		/// <param name="Instructions">Instructions for session.</param>
		/// <param name="ClearSession">If session should be restarted or not.</param>
		/// <param name="IntermediateResponseCallback">Callback method for intermediate callbacks.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		/// <returns>Response from OpenAI. Can be null, if text send is empty, or does not include text.</returns>
		public async Task<Message> ChatQueryWithHistory(string From, string Text, string Instructions, Function[] Functions,
			bool ClearSession, StreamEventHandler IntermediateResponseCallback, object State)
		{
			await RuntimeCounters.IncrementCounter(this.NodeId + ".Rx", Text.Length);
			await RuntimeCounters.IncrementCounter(this.NodeId + "." + From.ToLower() + ".Rx", Text.Length);

			using (OpenAIClient Client = new OpenAIClient(this.ApiKey, this.Sniffers))
			{
				Text = await ConvertTextIfSpeech(Client, Text);

				if (!string.IsNullOrEmpty(Text))
				{
					if (!sessions.TryGetValue(From, out ChatHistory Session) ||
						Session.Instructions != Instructions ||
						ClearSession)
					{
						Session = new ChatHistory(From, Instructions);
						sessions[From] = Session;

						Session.Add(new SystemMessage(Instructions), 4000);
					}

					Session.Add(new UserMessage(Text), 4000);

					Message Response2;

					if (IntermediateResponseCallback is null)
						Response2 = await Client.ChatGPT(Session.User.LowerCase, Session.Messages, Functions);
					else
					{
						Response2 = await Client.ChatGPT(Session.User.LowerCase, Session.Messages, Functions,
							IntermediateResponseCallback, State);
					}

					Session.Add(Response2, 4000);

					int c = Response2.Content?.Length ?? 0;

					if (!string.IsNullOrEmpty(Response2.FunctionName))
					{
						c += Response2.FunctionName.Length;
						c += JSON.Encode(Response2.FunctionArguments, false).Length;
					}

					await RuntimeCounters.IncrementCounter(this.NodeId + ".Tx", c);
					await RuntimeCounters.IncrementCounter(this.NodeId + "." + From.ToLower() + ".Tx", c);

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
