namespace TAG.Networking.OpenAI.Messages
{
	/// <summary>
	/// OpenAI assistant message.
	/// </summary>
	public class AssistantMessage : Message
	{
		/// <summary>
		/// OpenAI assistant message.
		/// </summary>
		/// <param name="Content">Content of message.</param>
		public AssistantMessage(string Content)
			: base(Content)
		{
		}

		/// <summary>
		/// Role of actor issuing the message.
		/// </summary>
		public override string Role => "assistant";
	}
}
