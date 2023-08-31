namespace TAG.Networking.OpenAI.Messages
{
	/// <summary>
	/// OpenAI user message.
	/// </summary>
	public class UserMessage : Message
	{
		/// <summary>
		/// OpenAI user message.
		/// </summary>
		/// <param name="Content">Content of message.</param>
		public UserMessage(string Content)
			: base(Content, null, null)
		{
		}

		/// <summary>
		/// Role of actor issuing the message.
		/// </summary>
		public override string Role => "user";
	}
}
