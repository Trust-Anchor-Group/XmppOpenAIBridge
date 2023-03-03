namespace TAG.Networking.OpenAI.Messages
{
	/// <summary>
	/// OpenAI system message.
	/// </summary>
	public class SystemMessage : Message
	{
		/// <summary>
		/// OpenAI system message.
		/// </summary>
		/// <param name="Content">Content of message.</param>
		public SystemMessage(string Content)
			: base(Content)
		{
		}

		/// <summary>
		/// Role of actor issuing the message.
		/// </summary>
		public override string Role => "system";
	}
}
