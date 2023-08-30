using System.Collections.Generic;
using TAG.Networking.OpenAI.Messages;
using Waher.Persistence;

namespace TAG.Things.OpenAI
{
	/// <summary>
	/// Contains information about the chat history, for a specific user.
	/// </summary>
	public class ChatHistory
	{
		private int nrCharacters;

		/// <summary>
		/// Contains information about the chat history, for a specific user.
		/// </summary>
		/// <param name="BareJID">Bare JID of user.</param>
		/// <param name="Instructions">Original instructions for session.</param>
		public ChatHistory(string BareJID, string Instructions)
		{
			this.Messages = new LinkedList<Message>();
			this.User = BareJID;
			this.Instructions = Instructions;
			this.nrCharacters = 0;
		}

		/// <summary>
		/// Identifier of user.
		/// </summary>
		public CaseInsensitiveString User { get; set; }

		/// <summary>
		/// List of messages.
		/// </summary>
		public LinkedList<Message> Messages { get; }

		/// <summary>
		/// Original instructions for session.
		/// </summary>
		public string Instructions { get; }

		/// <summary>
		/// Adds a message to the chat history.
		/// </summary>
		/// <param name="Message">Message</param>
		/// <param name="MaxCharacters">Maximum number of characters.</param>
		public void Add(Message Message, int MaxCharacters)
		{
			this.nrCharacters += Message.Content.Length;

			while (this.nrCharacters > MaxCharacters && !(this.Messages.First is null))
			{
				this.nrCharacters -= this.Messages.First.Value.Content.Length;
				this.Messages.RemoveFirst();
			}

			this.Messages.AddLast(Message);
		}
	}
}
