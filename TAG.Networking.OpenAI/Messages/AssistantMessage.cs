using System.Collections.Generic;

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
		/// <param name="FunctionName">Function name, if calling a function.</param>
		/// <param name="FunctionArguments">Function arguments, if calling a function.</param>
		public AssistantMessage(string Content, string FunctionName, 
			Dictionary<string,object> FunctionArguments)
			: base(Content, FunctionName, FunctionArguments)
		{
		}

		/// <summary>
		/// Role of actor issuing the message.
		/// </summary>
		public override string Role => "assistant";
	}
}
