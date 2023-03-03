using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TAG.Networking.OpenAI.Messages;
using Waher.Content;
using Waher.Networking.Sniffers;

namespace TAG.Networking.OpenAI
{
	/// <summary>
	/// Client for communicating with the OpenAI API.
	/// </summary>
	public class OpenAIClient : Sniffable, IDisposable
	{
		private static readonly Uri completionsUri = new Uri("https://api.openai.com/v1/chat/completions");

		private readonly string apiKey;

		/// <summary>
		/// Client for communicating with the OpenAI API.
		/// </summary>
		/// <param name="ApiKey">API Key</param>
		/// <param name="Sniffers">Optional sniffers</param>
		public OpenAIClient(string ApiKey, params ISniffer[] Sniffers)
			: base(Sniffers)
		{
			this.apiKey = ApiKey;
		}

		/// <summary>
		/// Disposes of the client.
		/// </summary>
		public void Dispose()
		{
		}

		/// <summary>
		/// Performs a request to OpenAI ChatGPT turbo 3.5, and returns the textual
		/// response.
		/// </summary>
		/// <param name="Messages">Messages in conversation.</param>
		/// <returns>Message response.</returns>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public async Task<Message> ChatGPT(params Message[] Messages)
		{
			List<Dictionary<string, object>> Messages2 = new List<Dictionary<string, object>>();

			foreach (Message Message in Messages)
			{
				Messages2.Add(new Dictionary<string, object>()
				{
					{ "role", Message.Role },
					{ "content", Message.Content }
				});
			}

			Dictionary<string, object> Request = new Dictionary<string, object>()
			{
				{ "model", "gpt-3.5-turbo" },
				{ "messages", Messages2.ToArray() },
			};

			if (this.HasSniffers)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("POST(");
				sb.Append(completionsUri.ToString());
				sb.AppendLine(",");
				sb.Append(JSON.Encode(Request, true));
				sb.Append(")");

				this.TransmitText(sb.ToString());
			}

			object ResponseObj = await InternetContent.PostAsync(completionsUri, Request,
				new KeyValuePair<string, string>("Accept", "application/json"),
				new KeyValuePair<string, string>("Authorization", "Bearer " + this.apiKey));

			if (this.HasSniffers)
				this.ReceiveText(JSON.Encode(ResponseObj, true));

			if (!(ResponseObj is Dictionary<string, object> Response))
				throw new Exception("Unexpected response returned: " + ResponseObj.GetType().FullName);

			if (!Response.TryGetValue("choices", out object Obj) ||
				!(Obj is Array Choices) ||
				Choices.Length == 0 ||
				!(Choices.GetValue(0) is Dictionary<string, object> Option) ||
				!Option.TryGetValue("message", out Obj) ||
				!(Obj is Dictionary<string, object> MessageObj))
			{
				throw new Exception("Message not found in response.");
			}

			if (!Message.TryParse(MessageObj, out Message Result))
				throw new Exception("Unable to parse message in response.");

			return Result;
		}
	}
}
