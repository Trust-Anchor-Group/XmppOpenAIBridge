using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TAG.Networking.OpenAI.Messages;
using Waher.Content;
using Waher.Content.Getters;
using Waher.Content.Multipart;
using Waher.Networking.Sniffers;
using Waher.Runtime.Temporary;

namespace TAG.Networking.OpenAI
{
	/// <summary>
	/// Client for communicating with the OpenAI API.
	/// </summary>
	public class OpenAIClient : Sniffable, IDisposable
	{
		private static readonly Uri completionsUri = new Uri("https://api.openai.com/v1/chat/completions");
		private static readonly Uri transcriptionsUri = new Uri("https://api.openai.com/v1/audio/transcriptions");

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

			try
			{
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
			catch (WebException ex)
			{
				throw this.ProcessWebException(ex);
			}
		}

		private Exception ProcessWebException(WebException ex)
		{
			if (ex.Content is string s)
				return new IOException(s, ex);
			else if (ex.Content is Dictionary<string, object> Obj &&
				Obj.TryGetValue("error", out object Obj2) &&
				Obj2 is Dictionary<string, object> Error &&
				Error.TryGetValue("message", out object Obj3) &&
				Obj3 is string s2)
			{
				return new IOException(s2, ex);
			}
			else
				return ex;

		}

		/// <summary>
		/// Performs a request to OpenAI Whisper API for speech to text conversion.
		/// </summary>
		/// <param name="Uri">URI to audio file to send.</param>
		/// <returns>Text conversion.</returns>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public async Task<string> Whisper(Uri Uri)
		{
			if (this.HasSniffers)
				this.TransmitText("GET(" + Uri.ToString() + ")");

			KeyValuePair<string, TemporaryStream> P = await InternetContent.GetTempStreamAsync(Uri, 60000,
				new KeyValuePair<string, string>("User-Agent", typeof(OpenAIClient).FullName));
			string ContentType = P.Key;

			using (TemporaryStream File = P.Value)
			{
				if (this.HasSniffers)
					this.ReceiveText(File.Length.ToString() + " bytes received.");

				File.Position = 0;

				int Len = (int)Math.Min(int.MaxValue, File.Length);
				byte[] Audio = new byte[Len];

				await File.ReadAsync(Audio, 0, Len);

				string Extension = InternetContent.GetFileExtension(ContentType);
				return await this.Whisper(Audio, ContentType, "audio." + Extension);
			}
		}

		/// <summary>
		/// Performs a request to OpenAI Whisper API for speech to text conversion.
		/// </summary>
		/// <param name="FileName">Full path to audio file to send.</param>
		/// <returns>Text conversion.</returns>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public async Task<string> Whisper(string FileName)
		{
			byte[] Audio = await Resources.ReadAllBytesAsync(FileName);

			FileName = Path.GetFileName(FileName);

			string ContentType = InternetContent.GetContentType(Path.GetExtension(FileName));

			return await this.Whisper(Audio, ContentType, FileName);
		}

		/// <summary>
		/// Performs a request to OpenAI Whisper API for speech to text conversion.
		/// </summary>
		/// <param name="Audio">Binary audio to convert.</param>
		/// <param name="ContentType">Content-Type of audio.</param>
		/// <returns>Text conversion.</returns>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public Task<string> Whisper(byte[] Audio, string ContentType)
		{
			string Extension = InternetContent.GetFileExtension(ContentType);
			return this.Whisper(Audio, ContentType, "audio." + Extension);
		}

		/// <summary>
		/// Performs a request to OpenAI Whisper API for speech to text conversion.
		/// </summary>
		/// <param name="Audio">Binary audio to convert.</param>
		/// <param name="ContentType">Content-Type of audio.</param>
		/// <param name="FileName">Name of audio file.</param>
		/// <returns>Text conversion.</returns>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public async Task<string> Whisper(byte[] Audio, string ContentType, string FileName)
		{
			KeyValuePair<byte[], string> P = await FormDataDecoder.Encode(new EmbeddedContent[]
			{
				new EmbeddedContent()
				{
					Name = "model",
					Raw = Encoding.UTF8.GetBytes("whisper-1"),
					ContentType = "text/plain",
					Disposition = ContentDisposition.FormData
				},
				new EmbeddedContent()
				{
					Name = "file",
					Raw = Audio,
					ContentType = ContentType,
					Disposition = ContentDisposition.FormData,
					FileName = FileName
				}
			});

			byte[] Encoded = P.Key;
			string EncodedContentType = P.Value;

			if (this.HasSniffers)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("POST(");
				sb.Append(transcriptionsUri.ToString());
				sb.Append(",\"");
				sb.Append(EncodedContentType);
				sb.AppendLine("\",");
				sb.Append(Encoding.UTF8.GetString(Encoded));
				sb.Append(")");

				this.TransmitText(sb.ToString());
			}

			try
			{
				KeyValuePair<byte[], string> P2 = await InternetContent.PostAsync(transcriptionsUri,
					Encoded, EncodedContentType,
					new KeyValuePair<string, string>("Accept", "application/json"),
					new KeyValuePair<string, string>("Authorization", "Bearer " + this.apiKey));

				object ResponseObj = await InternetContent.DecodeAsync(P2.Value, P2.Key, transcriptionsUri);

				if (this.HasSniffers)
					this.ReceiveText(JSON.Encode(ResponseObj, true));

				if (!(ResponseObj is Dictionary<string, object> Response))
					throw new Exception("Unexpected response returned: " + ResponseObj.GetType().FullName);

				if (!Response.TryGetValue("text", out object Obj) || !(Obj is string Text))
					throw new Exception("Text not found in response.");

				return Text;
			}
			catch (WebException ex)
			{
				throw this.ProcessWebException(ex);
			}
		}

	}
}
