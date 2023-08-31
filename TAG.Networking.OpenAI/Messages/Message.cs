using System;
using System.Collections.Generic;
using System.Text;
using Waher.Content;

namespace TAG.Networking.OpenAI.Messages
{
	/// <summary>
	/// Abstract base class of an OpenAI message.
	/// </summary>
	public abstract class Message
	{
		internal StringBuilder arguments = null;

		/// <summary>
		/// Abstract base class of an OpenAI message.
		/// </summary>
		/// <param name="Content">Content of message.</param>
		/// <param name="FunctionName">Function name, if calling a function.</param>
		/// <param name="FunctionArguments">Function arguments, if calling a function.</param>
		public Message(string Content, string FunctionName,
			Dictionary<string, object> FunctionArguments)
		{
			this.Content = Content;
			this.FunctionName = FunctionName;
			this.FunctionArguments = FunctionArguments;
		}

		/// <summary>
		/// Role of actor issuing the message.
		/// </summary>
		public abstract string Role { get; }

		/// <summary>
		/// Content of message
		/// </summary>
		public string Content
		{
			get;
			internal set;
		}

		/// <summary>
		/// Function name, if calling a function.
		/// </summary>
		public string FunctionName 
		{ 
			get;
			internal set;
		}

		/// <summary>
		/// Function arguments, if calling a function.
		/// </summary>
		public Dictionary<string, object> FunctionArguments 
		{
			get;
			internal set;
		}

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

			if (!Message.TryGetValue("content", out Obj))
				return false;

			string Content = Obj as string;
			if (Content is null && !(Obj is null))
				return false;

			string FunctionName;
			Dictionary<string, object> FunctionArguments;

			if (Message.TryGetValue("function_call", out Obj))
			{
				if (!(Obj is Dictionary<string, object> FunctionCall))
					return false;

				if (!FunctionCall.TryGetValue("name", out Obj) || !(Obj is string Name))
					return false;
				else
					FunctionName = Name;

				if (!FunctionCall.TryGetValue("arguments", out Obj) || !(Obj is string s))
					return false;

				try
				{
					Obj = JSON.Parse(s);

					FunctionArguments = Obj as Dictionary<string, object>;
					if (FunctionArguments is null)
						return false;
				}
				catch (Exception)
				{
					return false;
				}
			}
			else
			{
				FunctionName = null;
				FunctionArguments = null;
			}

			switch (Role)
			{
				case "system":
					Parsed = new SystemMessage(Content ?? string.Empty);
					break;

				case "user":
					Parsed = new UserMessage(Content ?? string.Empty);
					break;

				case "assistant":
					Parsed = new AssistantMessage(Content ?? string.Empty,
						FunctionName, FunctionArguments);
					break;

				default:
					return false;
			}

			return true;
		}

		public static bool TryParseDelta(Dictionary<string, object> Message, ref Message Parsed, out string Diff)
		{
			object Obj;

			Diff = null;

			if (Parsed is null)
			{
				if (!Message.TryGetValue("role", out Obj) || !(Obj is string Role))
					return false;

				switch (Role)
				{
					case "system":
						Parsed = new SystemMessage(string.Empty);
						break;

					case "user":
						Parsed = new UserMessage(string.Empty);
						break;

					case "assistant":
						Parsed = new AssistantMessage(string.Empty, null, null);
						break;

					default:
						return false;
				}
			}

			if (Message.TryGetValue("content", out Obj) && Obj is string Content)
			{
				Parsed.Content += Content;
				Diff = Content;
			}

			if (Message.TryGetValue("function_call", out Obj) &&
				Obj is Dictionary<string, object> FunctionCall)
			{
				if (FunctionCall.TryGetValue("name", out Obj) && Obj is string Name)
				{
					Parsed.FunctionName = Name;
					Parsed.arguments = new StringBuilder();
				}

				if (FunctionCall.TryGetValue("arguments",out Obj) && Obj is string DeltaArguments)
					Parsed.arguments?.Append(DeltaArguments);
			}

			return true;
		}
	}
}
