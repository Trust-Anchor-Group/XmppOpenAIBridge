using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TAG.Networking.OpenAI.Files;
using TAG.Networking.OpenAI.Functions;
using TAG.Networking.OpenAI.Messages;
using Waher.Content;
using Waher.Content.Getters;
using Waher.Content.Multipart;
using Waher.Events;
using Waher.Networking;
using Waher.Networking.Sniffers;
using Waher.Runtime.IO;
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
	/// 
	/// Reference:
	/// https://platform.openai.com/docs/api-reference
	/// </summary>
	public class OpenAIClient : CommunicationLayer, IDisposable
	{
		private static readonly Uri chatCompletionsUri = new Uri("https://api.openai.com/v1/chat/completions");
		private static readonly Uri audioTranscriptionsUri = new Uri("https://api.openai.com/v1/audio/transcriptions");
		private static readonly Uri imagesGenerationsUri = new Uri("https://api.openai.com/v1/images/generations");
		private static readonly Uri filesUri = new Uri("https://api.openai.com/v1/files");

		private readonly string model;
		private readonly string apiKey;

		/// <summary>
		/// Client for communicating with the OpenAI API.
		/// </summary>
		/// <param name="ApiKey">API Key</param>
		/// <param name="Sniffers">Optional sniffers</param>
		public OpenAIClient(string ApiKey, params ISniffer[] Sniffers)
			: this("gpt-4", ApiKey, Sniffers)
		{
		}

		/// <summary>
		/// Client for communicating with the OpenAI API.
		/// </summary>
		/// <param name="Model">Model to use.</param>
		/// <param name="ApiKey">API Key</param>
		/// <param name="Sniffers">Optional sniffers</param>
		public OpenAIClient(string Model, string ApiKey, params ISniffer[] Sniffers)
			: base(true, Sniffers)
		{
			this.model = Model;
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
			return this.ChatGPT(string.Empty, Messages, null, null);
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
			return this.ChatGPT(User, Messages, null, null);
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
		public Task<Message> ChatGPT(string User, IEnumerable<Message> Messages)
		{
			return this.ChatGPT(User, Messages, null, null, null);
		}

		/// <summary>
		/// Performs a request to OpenAI ChatGPT turbo 3.5, and returns the textual
		/// response.
		/// </summary>
		/// <param name="User">User performing the action.</param>
		/// <param name="Messages">Messages in conversation.</param>
		/// <param name="StreamCallback">Stream callback for intermediate responses.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		/// <returns>Message response.</returns>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public Task<Message> ChatGPT(string User, IEnumerable<Message> Messages,
			EventHandlerAsync<StreamEventArgs> StreamCallback, object State)
		{
			return this.ChatGPT(User, Messages, null, StreamCallback, State);
		}

		/// <summary>
		/// Performs a request to OpenAI ChatGPT turbo 3.5, and returns the textual
		/// response.
		/// </summary>
		/// <param name="User">User performing the action.</param>
		/// <param name="Messages">Messages in conversation.</param>
		/// <param name="Functions">Functions</param>
		/// <returns>Message response.</returns>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public Task<Message> ChatGPT(string User, IEnumerable<Message> Messages,
			IEnumerable<Function> Functions)
		{
			return this.ChatGPT(User, Messages, Functions, null, null);
		}

		/// <summary>
		/// Performs a request to OpenAI ChatGPT turbo 3.5, and returns the textual
		/// response.
		/// </summary>
		/// <param name="User">User performing the action.</param>
		/// <param name="Messages">Messages in conversation.</param>
		/// <param name="Functions">Functions</param>
		/// <param name="StreamCallback">Stream callback for intermediate responses.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		/// <returns>Message response.</returns>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public async Task<Message> ChatGPT(string User, IEnumerable<Message> Messages,
			IEnumerable<Function> Functions, EventHandlerAsync<StreamEventArgs> StreamCallback, 
			object State)
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
				{ "model", this.model },
				{ "messages", Messages2.ToArray() },
			};

			if (!(Functions is null))
			{
				List<Dictionary<string, object>> FunctionsArray = new List<Dictionary<string, object>>();

				foreach (Function F in Functions)
					FunctionsArray.Add(F.ToJson());

				if (FunctionsArray.Count > 0)
				{
					Request["functions"] = FunctionsArray.ToArray();
					Request["function_call"] = "auto";
				}
			}

			if (!string.IsNullOrEmpty(User))
				Request["user"] = User;

			if (!(StreamCallback is null))
				Request["stream"] = true;

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
				Message Result;

				if (StreamCallback is null)
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

					if (!Message.TryParse(MessageObj, out Result))
						throw new Exception("Unable to parse message in response.");
				}
				else
				{
					HttpClient Client = null;
					HttpRequestMessage HttpRequest = null;
					HttpResponseMessage HttpResponse = null;
					Stream ResponseStream = null;
					StreamReader ResponseReader = null;

					try
					{
						Client = new HttpClient();

						HttpRequest = new HttpRequestMessage(HttpMethod.Post, chatCompletionsUri)
						{
							Content = new StringContent(JSON.Encode(Request, false), Encoding.UTF8, "application/json"),
						};

						HttpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
						HttpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.apiKey);

						HttpResponse = await Client.SendAsync(HttpRequest, HttpCompletionOption.ResponseHeadersRead);
						HttpResponse.EnsureSuccessStatusCode();

						ResponseStream = await HttpResponse.Content.ReadAsStreamAsync();
						ResponseReader = new StreamReader(ResponseStream);

						Result = null;

						bool Finished = false;

						while (!ResponseReader.EndOfStream)
						{
							string Line = await ResponseReader.ReadLineAsync();
							object ResponseObj;

							if (this.HasSniffers)
								this.ReceiveText(Line);

							if (Line.StartsWith("data:"))
							{
								Line = Line[5..];

								try
								{
									ResponseObj = JSON.Parse(Line);
								}
								catch (Exception ex)
								{
									if (this.HasSniffers)
										this.Error(ex.Message);

									continue;
								}

								if (!(ResponseObj is Dictionary<string, object> Response))
								{
									if (this.HasSniffers)
										this.Error("Unexpected response returned: " + ResponseObj.GetType().FullName);

									continue;
								}

								if (!Response.TryGetValue("choices", out object Obj) ||
									!(Obj is Array Choices) ||
									Choices.Length == 0 ||
									!(Choices.GetValue(0) is Dictionary<string, object> Option) ||
									!Option.TryGetValue("delta", out Obj) ||
									!(Obj is Dictionary<string, object> DeltaObj))
								{
									if (this.HasSniffers)
										this.Error("Delta not found in response.");

									continue;
								}

								if (!Message.TryParseDelta(DeltaObj, ref Result, out string Diff))
								{
									if (this.HasSniffers)
										this.Error("Unable to parse response.");

									continue;
								}

								if (Option.TryGetValue("finish_reason", out Obj) &&
									Obj is string FinishReason)
								{
									switch (FinishReason)
									{
										case "stop":
											Finished = true;
											break;

										case "function_call":
											Finished = true;
											
											if (!(Result?.arguments is null))
											{
												try
												{
													Result.FunctionArguments = (Dictionary<string, object>)JSON.Parse(Result.arguments.ToString());
												}
												catch (Exception ex)
												{
													Result.FunctionName = null;
													if (this.HasSniffers)
														this.Error(ex.Message);
												}
											}
											break;
									}
								}

								try
								{
									StreamEventArgs e = new StreamEventArgs(Result.Content, Diff, Finished, State);

									await StreamCallback.Raise(this, e);
								}
								catch (Exception ex)
								{
									Log.Exception(ex);
								}

								if (Finished)
									break;
							}
							else if (Line.Trim() == "[DONE]")
							{
								if (!Finished)
								{
									Finished = true;

									try
									{
										StreamEventArgs e = new StreamEventArgs(Result.Content, string.Empty, Finished, State);

										await StreamCallback.Raise(this, e);
									}
									catch (Exception ex)
									{
										Log.Exception(ex);
									}
								}

								break;
							}
						}

						if (!Finished)
						{
							try
							{
								StreamEventArgs e = new StreamEventArgs(Result.Content, string.Empty, true, State);
								await StreamCallback.Raise(this, e);
							}
							catch (Exception ex)
							{
								Log.Exception(ex);
							}
						}
					}
					finally
					{
						ResponseReader?.Dispose();
						ResponseStream?.Dispose();
						HttpResponse?.Dispose();
						HttpRequest?.Dispose();
						Client?.Dispose();
					}
				}

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

			ContentStreamResponse Content = await InternetContent.GetTempStreamAsync(Uri, 60000,
				new KeyValuePair<string, string>("User-Agent", typeof(OpenAIClient).FullName));
			Content.AssertOk();

			using TemporaryStream File = Content.Encoded;
			
			if (this.HasSniffers)
				this.ReceiveText(File.Length.ToString() + " bytes received.");

			File.Position = 0;

			int Len = (int)Math.Min(int.MaxValue, File.Length);
			byte[] Audio = new byte[Len];

			await File.ReadAsync(Audio, 0, Len);

			string Extension = InternetContent.GetFileExtension(Content.ContentType);
			return await this.Whisper(Audio, Content.ContentType, "audio." + Extension);
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
			byte[] Audio = await Waher.Runtime.IO.Files.ReadAllBytesAsync(FileName);

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
			}, null);

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
				ContentBinaryResponse Content = await InternetContent.PostAsync(audioTranscriptionsUri,
					Encoded, EncodedContentType,
					new KeyValuePair<string, string>("Accept", "application/json"),
					new KeyValuePair<string, string>("Authorization", "Bearer " + this.apiKey));

				Content.AssertOk();

				object ResponseObj = await InternetContent.DecodeAsync(Content.ContentType, Content.Encoded, audioTranscriptionsUri);

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

		/// <summary>
		/// Lists uploaded files.
		/// </summary>
		/// <returns>File references.</returns>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public async Task<FileReference[]> ListFiles()
		{
			if (this.HasSniffers)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("GET(");
				sb.Append(filesUri.ToString());
				sb.Append(")");

				this.TransmitText(sb.ToString());
			}

			try
			{
				object ResponseObj = await InternetContent.GetAsync(filesUri,
					new KeyValuePair<string, string>("Accept", "application/json"),
					new KeyValuePair<string, string>("Authorization", "Bearer " + this.apiKey));

				if (this.HasSniffers)
					this.ReceiveText(JSON.Encode(ResponseObj, true));

				if (!(ResponseObj is Dictionary<string, object> Response))
					throw new Exception("Unexpected response returned: " + ResponseObj.GetType().FullName);

				if (!Response.TryGetValue("data", out object Obj) || !(Obj is Array Data))
					throw new Exception("Unexpected response.");

				List<FileReference> Result = new List<FileReference>();

				foreach (object Item in Data)
				{
					if (FileReference.TryParse(Item, out FileReference Ref))
						Result.Add(Ref);
				}

				return Result.ToArray();
			}
			catch (WebException ex)
			{
				throw this.ProcessWebException(ex);
			}
		}

		/// <summary>
		/// Uploads a file
		/// </summary>
		/// <param name="FullFileName">Full filename of file to upload.</param>
		/// <param name="Purpose">Purpose of file.</param>
		/// <returns>File reference object.</returns>
		public async Task<FileReference> UploadFile(string FullFileName, Purpose Purpose)
		{
			byte[] Bin = await Waher.Runtime.IO.Files.ReadAllBytesAsync(FullFileName);
			string ContentType = InternetContent.GetContentType(Path.GetExtension(FullFileName));

			return await this.UploadFile(Bin, ContentType, Path.GetFileName(FullFileName), Purpose);
		}

		/// <summary>
		/// Uploads a file
		/// </summary>
		/// <param name="TextContent">Text content.</param>
		/// <param name="FileName">Name of file.</param>
		/// <param name="Purpose">Purpose of file.</param>
		/// <returns>File reference object.</returns>
		public Task<FileReference> UploadFile(string TextContent, string FileName, Purpose Purpose)
		{
			byte[] Bin = Encoding.UTF8.GetBytes(TextContent);

			return this.UploadFile(Bin, "text/plain; charset=utf-8", FileName, Purpose);
		}

		/// <summary>
		/// Uploads a file
		/// </summary>
		/// <param name="Data">Binary encoding of file.</param>
		/// <param name="ContentType">Content-Type</param>
		/// <param name="FileName">Name of file.</param>
		/// <param name="Purpose">Purpose of file.</param>
		/// <returns>File reference object.</returns>
		public async Task<FileReference> UploadFile(byte[] Data, string ContentType, string FileName, Purpose Purpose)
		{
			KeyValuePair<byte[], string> P = await FormDataDecoder.Encode(new EmbeddedContent[]
			{
				new EmbeddedContent()
				{
					Name = "file",
					Raw = Data,
					ContentType = ContentType,
					Disposition = ContentDisposition.FormData,
					FileName = FileName
				},
				new EmbeddedContent()
				{
					Name = "purpose",
					Raw = Encoding.UTF8.GetBytes(Purpose.ToString().Replace('_','-')),
					ContentType = "text/plain",
					Disposition = ContentDisposition.FormData
				}
			}, null);

			byte[] Encoded = P.Key;
			string EncodedContentType = P.Value;

			if (this.HasSniffers)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("POST(");
				sb.Append(filesUri.ToString());
				sb.Append(",\"");
				sb.Append(EncodedContentType);
				sb.AppendLine("\",");
				sb.Append(Encoding.UTF8.GetString(Encoded));
				sb.Append(")");

				this.TransmitText(sb.ToString());
			}

			try
			{
				ContentBinaryResponse Content = await InternetContent.PostAsync(filesUri,
					Encoded, EncodedContentType,
					new KeyValuePair<string, string>("Accept", "application/json"),
					new KeyValuePair<string, string>("Authorization", "Bearer " + this.apiKey));

				Content.AssertOk();

				object ResponseObj = await InternetContent.DecodeAsync(Content.ContentType, Content.Encoded, audioTranscriptionsUri);

				if (this.HasSniffers)
					this.ReceiveText(JSON.Encode(ResponseObj, true));

				if (!FileReference.TryParse(ResponseObj, out FileReference Result))
					throw new Exception("Unexpected response returned: " + ResponseObj.GetType().FullName);

				return Result;
			}
			catch (WebException ex)
			{
				throw this.ProcessWebException(ex);
			}
		}

		/// <summary>
		/// Gets a file reference object, given the file ID.
		/// </summary>
		/// <param name="FileId">File ID</param>
		/// <returns>File reference.</returns>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public async Task<FileReference> GetFileReference(string FileId)
		{
			Uri Uri = new Uri(filesUri.ToString() + "/" + FileId);

			if (this.HasSniffers)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("GET(");
				sb.Append(Uri.ToString());
				sb.Append(")");

				this.TransmitText(sb.ToString());
			}

			try
			{
				object ResponseObj = await InternetContent.GetAsync(Uri,
					new KeyValuePair<string, string>("Accept", "application/json"),
					new KeyValuePair<string, string>("Authorization", "Bearer " + this.apiKey));

				if (this.HasSniffers)
					this.ReceiveText(JSON.Encode(ResponseObj, true));

				if (!FileReference.TryParse(ResponseObj, out FileReference Ref))
					throw new Exception("Unexpected response returned: " + ResponseObj.GetType().FullName);

				return Ref;
			}
			catch (WebException ex)
			{
				throw this.ProcessWebException(ex);
			}
		}

		/// <summary>
		/// Gets the contents of a file, given the file ID.
		/// </summary>
		/// <param name="FileId">File ID</param>
		/// <returns>File content.</returns>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public async Task<object> GetFileContent(string FileId)
		{
			Uri Uri = new Uri(filesUri.ToString() + "/" + FileId + "/content");

			if (this.HasSniffers)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("GET(");
				sb.Append(Uri.ToString());
				sb.Append(")");

				this.TransmitText(sb.ToString());
			}

			try
			{
				object ResponseObj = await InternetContent.GetAsync(Uri,
					new KeyValuePair<string, string>("Authorization", "Bearer " + this.apiKey));

				if (this.HasSniffers)
					this.ReceiveText(JSON.Encode(ResponseObj, true));

				return ResponseObj;
			}
			catch (WebException ex)
			{
				throw this.ProcessWebException(ex);
			}
		}

		/// <summary>
		/// Deletes a file, given the file ID.
		/// </summary>
		/// <param name="FileId">File ID</param>
		/// <exception cref="Exception">If unable to communicate with API, 
		/// if exceeding limits, or if something unexpected happened.</exception>
		public async Task DeleteFile(string FileId)
		{
			Uri Uri = new Uri(filesUri.ToString() + "/" + FileId);

			if (this.HasSniffers)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("DELETE(");
				sb.Append(Uri.ToString());
				sb.Append(")");

				this.TransmitText(sb.ToString());
			}

			try
			{
				object ResponseObj = await InternetContent.DeleteAsync(Uri,
					new KeyValuePair<string, string>("Authorization", "Bearer " + this.apiKey));

				if (this.HasSniffers)
					this.ReceiveText(JSON.Encode(ResponseObj, true));
			}
			catch (WebException ex)
			{
				throw this.ProcessWebException(ex);
			}
		}

	}
}
