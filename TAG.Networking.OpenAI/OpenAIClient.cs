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
	/// Size of generated images
	/// </summary>
	public enum ImageSize
	{
		/// <summary>
		/// 256x256 pixels
		/// </summary>
		ImageSize256x256,

		/// <summary>
		/// 512x512 pixels
		/// </summary>
		ImageSize512x512,

		/// <summary>
		/// 1024x1024 pixels
		/// </summary>
		ImageSize1024x1024
	}

	/// <summary>
	/// Client for communicating with the OpenAI API.
	/// </summary>
	public class OpenAIClient : Sniffable, IDisposable
	{
		private static readonly Uri chatCompletionsUri = new Uri("https://api.openai.com/v1/chat/completions");
		private static readonly Uri audioTranscriptionsUri = new Uri("https://api.openai.com/v1/audio/transcriptions");
		private static readonly Uri imagesGenerationsUri = new Uri("https://api.openai.com/v1/images/generations");

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
		/// <param name="User">User performing the action.</param>
		/// <param name="Messages">Messages in conversation.</param>
		/// <returns>Message response.</returns>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public Task<Message> ChatGPT(params Message[] Messages)
		{
			return this.ChatGPT(string.Empty, Messages);
		}

		/// <summary>
		/// Performs a request to OpenAI ChatGPT turbo 3.5, and returns the textual
		/// response.
		/// </summary>
		/// <param name="User">User performing the action.</param>
		/// <param name="Messages">Messages in conversation.</param>
		/// <returns>Message response.</returns>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public Task<Message> ChatGPT(string User, params Message[] Messages)
		{
			return this.ChatGPT(User, (IEnumerable<Message>)Messages);
		}

		/// <summary>
		/// Performs a request to OpenAI ChatGPT turbo 3.5, and returns the textual
		/// response.
		/// </summary>
		/// <param name="User">User performing the action.</param>
		/// <param name="Messages">Messages in conversation.</param>
		/// <returns>Message response.</returns>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public async Task<Message> ChatGPT(string User, IEnumerable<Message> Messages)
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

			if (!string.IsNullOrEmpty(User))
				Request["user"] = User;

			if (this.HasSniffers)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("POST(");
				sb.Append(chatCompletionsUri.ToString());
				sb.AppendLine(",");
				sb.Append(JSON.Encode(Request, true));
				sb.Append(")");

				this.TransmitText(sb.ToString());
			}

			try
			{
				object ResponseObj = await InternetContent.PostAsync(chatCompletionsUri, Request,
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
				sb.Append(audioTranscriptionsUri.ToString());
				sb.Append(",\"");
				sb.Append(EncodedContentType);
				sb.AppendLine("\",");
				sb.Append(Encoding.UTF8.GetString(Encoded));
				sb.Append(")");

				this.TransmitText(sb.ToString());
			}

			try
			{
				KeyValuePair<byte[], string> P2 = await InternetContent.PostAsync(audioTranscriptionsUri,
					Encoded, EncodedContentType,
					new KeyValuePair<string, string>("Accept", "application/json"),
					new KeyValuePair<string, string>("Authorization", "Bearer " + this.apiKey));

				object ResponseObj = await InternetContent.DecodeAsync(P2.Value, P2.Key, audioTranscriptionsUri);

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

		/// <summary>
		/// Generates an image from a description text.
		/// </summary>
		/// <param name="Description">Image description</param>
		/// <param name="User">User performing the action.</param>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		/// <returns>Generated image.</returns>
		public async Task<Uri> CreateImage(string Description, string User)
		{
			return (await this.CreateImages(Description, ImageSize.ImageSize1024x1024, 1, User))[0];
		}

		/// <summary>
		/// Generates an image from a description text.
		/// </summary>
		/// <param name="Description">Image description</param>
		/// <param name="Size">Size of images to generate</param>
		/// <param name="User">User performing the action.</param>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		/// <returns>Generated image.</returns>
		public async Task<Uri> CreateImage(string Description, ImageSize Size,
			string User)
		{
			return (await this.CreateImages(Description, Size, 1, User))[0];
		}

		/// <summary>
		/// Generates images from a description text.
		/// </summary>
		/// <param name="Description">Image description</param>
		/// <param name="Size">Size of images to generate</param>
		/// <param name="NumberOfImages">Number of images to create (1-10)</param>
		/// <param name="User">User performing the action.</param>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		/// <returns>Generated images.</returns>
		public async Task<Uri[]> CreateImages(string Description, ImageSize Size,
			int NumberOfImages, string User)
		{
			Dictionary<string, object> Request = new Dictionary<string, object>()
			{
				{ "prompt", Description },
				{ "n", NumberOfImages }
			};

			if (!string.IsNullOrEmpty(User))
				Request["user"] = User;

			switch (Size)
			{
				case ImageSize.ImageSize256x256:
					Request["size"] = "256x256";
					break;

				case ImageSize.ImageSize512x512:
					Request["size"] = "512x512";
					break;

				case ImageSize.ImageSize1024x1024:
					Request["size"] = "1024x1024";
					break;
			}

			if (this.HasSniffers)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("POST(");
				sb.Append(imagesGenerationsUri.ToString());
				sb.AppendLine(",");
				sb.Append(JSON.Encode(Request, true));
				sb.Append(")");

				this.TransmitText(sb.ToString());
			}

			try
			{
				object ResponseObj = await InternetContent.PostAsync(imagesGenerationsUri, Request,
					new KeyValuePair<string, string>("Accept", "application/json"),
					new KeyValuePair<string, string>("Authorization", "Bearer " + this.apiKey));

				if (this.HasSniffers)
					this.ReceiveText(JSON.Encode(ResponseObj, true));

				if (!(ResponseObj is Dictionary<string, object> Response))
					throw new Exception("Unexpected response returned: " + ResponseObj.GetType().FullName);

				if (!Response.TryGetValue("data", out object Obj) || !(Obj is Array Data))
					throw new Exception("Data not found in response.");

				int i, c = Data.Length;

				if (c != NumberOfImages)
					throw new Exception("Not the expected number of images returned.");

				Uri[] Uris = new Uri[c];

				for (i = 0; i < c; i++)
				{
					object ItemObj = Data.GetValue(i);
					if (!(ItemObj is Dictionary<string, object> Item))
						throw new Exception("Data item not of expected type.");

					if (!Item.TryGetValue("url", out Obj) || !(Obj is string Url))
						throw new Exception("Item URL not of expected type.");

					Uris[i] = new Uri(Url);
				}

				return Uris;
			}
			catch (WebException ex)
			{
				throw this.ProcessWebException(ex);
			}
		}

	}
}
