using System.Collections.Generic;

namespace TAG.Networking.OpenAI.Messages
{
	/// <summary>
	/// Abstract base class of an OpenAI message.
	/// </summary>
	public abstract class Message
	{
		/// <summary>
		/// Abstract base class of an OpenAI message.
		/// </summary>
		/// <param name="Content">Content of message.</param>
		public Message(string Content)
		{
			this.Content = Content;
		}

		/// <summary>
		/// Role of actor issuing the message.
		/// </summary>
		public abstract string Role { get; }

		/// <summary>
		/// Content of message
		/// </summary>
		public string Content { get; }

		/// <summary>
		/// Parses a message object returned from the API.
		/// </summary>
		/// <param name="Message">Message object.</param>
		/// <param name="Parsed">Parsed result.</param>
		/// <returns>If able to parse the message object.</returns>
		public static bool TryParse(Dictionary<string, object> Message, out Message Parsed)
		{
			Parsed = null;

			if (!Message.TryGetValue("role", out object Obj) || !(Obj is string Role))
				return false;

			if (!Message.TryGetValue("content", out Obj) || !(Obj is string Content))
				return false;

			switch (Role)
			{
				case "system": 
					Parsed = new SystemMessage(Content);
					break;

				case "user": 
					Parsed = new UserMessage(Content);
					break;

				case "assistant": 
					Parsed = new AssistantMessage(Content);
					break;

				default:
					return false;
			}

			return true;
		}
	}
}
