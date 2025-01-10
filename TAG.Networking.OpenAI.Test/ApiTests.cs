using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using TAG.Networking.OpenAI.Files;
using TAG.Networking.OpenAI.Functions;
using TAG.Networking.OpenAI.Messages;
using Waher.Content;
using Waher.Events;
using Waher.Events.Console;
using Waher.Networking.Sniffers;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Runtime.Inventory;
using Waher.Runtime.Inventory.Loader;
using Waher.Runtime.Settings;
using Waher.Runtime.Temporary;

namespace TAG.Networking.OpenAI.Test
{
	[TestClass]
	public class ApiTests
	{
		private static OpenAIClient? client;

		[AssemblyInitialize]
		public static async Task AssemblyInitialize(TestContext _)
		{
			// Create inventory of available classes.
			TypesLoader.Initialize();

			// Register console event log
			Log.Register(new ConsoleEventSink(true, true));

			// Instantiate local encrypted object database.
			FilesProvider DB = await FilesProvider.CreateAsync(Path.Combine(Directory.GetCurrentDirectory(), "Data"), "Default",
				8192, 10000, 8192, Encoding.UTF8, 10000, true, false);

			await DB.RepairIfInproperShutdown(string.Empty);

			Database.Register(DB);

			// Start embedded modules (database lifecycle)

			await Types.StartAllModules(60000);
		}

		[AssemblyCleanup]
		public static async Task AssemblyCleanup()
		{
			await Log.TerminateAsync();
			await Types.StopAllModules();
		}

		[ClassInitialize]
		public static async Task ClassInitialize(TestContext _)
		{
			// Configuring API Key
			// NOTE: Don't check in API credentials into the repository. Uncomment the code below, and write your
			//       API Key into the runtime setting. Once written, you can empty the string in the code and re-comment
			//       it, so it's not overwritten the next time you run the tests.
			//await RuntimeSettings.SetAsync("OpenAI.APIKey", "ENTER YOUR API KEY HERE");

			// Reading API Key
			string ApiKey = await RuntimeSettings.GetAsync("OpenAI.APIKey", string.Empty);
			if (string.IsNullOrEmpty(ApiKey))
				Assert.Fail("API Key not configured. Make sure the API Key is configured before running tests.");

			client = new OpenAIClient(ApiKey,
				new ConsoleOutSniffer(BinaryPresentationMethod.Base64, LineEnding.NewLine));
		}

		[ClassCleanup(ClassCleanupBehavior.EndOfClass)]
		public static void ClassCleanup()
		{
			client?.Dispose();
			client = null;
		}

		[TestMethod]
		public async Task Test_01_ChatGPT()
		{
			Assert.IsNotNull(client);

			Message Response = await client.ChatGPT("UnitTest",
				new UserMessage("What is the OpenAI mission?"));

			Console.Out.WriteLine(Response.Content);
		}

		[TestMethod]
		public async Task Test_02_Whisper_WAV()
		{
			Assert.IsNotNull(client);

			// Reference: http://www.voiptroubleshooter.com/open_speech/
			Uri SampleAudioUri = new("http://www.voiptroubleshooter.com/open_speech/american/OSR_us_000_0010_8k.wav");

			string Response = await client.Whisper(SampleAudioUri);

			Console.Out.WriteLine(Response);
		}

		[TestMethod]
		public async Task Test_03_Whisper_MP3()
		{
			Assert.IsNotNull(client);

			// Reference: https://commons.wikimedia.org/wiki/Category:Audio_files_of_speeches
			Uri SampleAudioUri = new("https://upload.wikimedia.org/wikipedia/commons/d/dd/Leyenda_de_los_hermanos_ayar.mp3");

			string Response = await client.Whisper(SampleAudioUri);

			Console.Out.WriteLine(Response);
		}

		[TestMethod]
		public async Task Test_04_ImageGeneration_1()
		{
			Assert.IsNotNull(client);

			Uri ImageUri = await client.CreateImage("en grön fågel med en krona på huvudet som spelar piano med sina fötter", "UnitTest");
			Console.Out.WriteLine(ImageUri);

			ContentStreamResponse Content = await InternetContent.GetTempStreamAsync(ImageUri);
			Content.AssertOk();

			await CopyAndDisposeFile(Content.Encoded, Content.ContentType, "Test_04");
		}

		[TestMethod]
		public async Task Test_05_ImageGeneration_5()
		{
			Assert.IsNotNull(client);

			Uri[] ImageUris = await client.CreateImages("A pink train that falls of a bridge in winter", ImageSize.ImageSize1024x1024, 5, "UnitTest");
			int i = 1;

			foreach (Uri Uri in ImageUris)
			{
				Console.Out.WriteLine(Uri);

				ContentStreamResponse Content = await InternetContent.GetTempStreamAsync(Uri);
				Content.AssertOk();

				await CopyAndDisposeFile(Content.Encoded, Content.ContentType, "Test_05_" + i.ToString());

				i++;
			}
		}

		private static async Task CopyAndDisposeFile(TemporaryStream f, string ContentType, string FileName)
		{
			try
			{
				string Extension = InternetContent.GetFileExtension(ContentType);

				f.Position = 0;

				if (!Directory.Exists("Images"))
					Directory.CreateDirectory("Images");

				using FileStream Output = File.Create("Images\\" + FileName + "." + Extension);

				await f.CopyToAsync(Output);
			}
			finally
			{
				f.Dispose();
			}
		}

		[TestMethod]
		public async Task Test_06_ListFiles()
		{
			Assert.IsNotNull(client);

			FileReference[] References = await client.ListFiles();

			foreach (FileReference Ref in References)
				Console.Out.WriteLine(Ref.Id + "\t" + Ref.Purpose + "\t" + Ref.Bytes + "\t" + Ref.FileName);
		}

		[TestMethod]
		public async Task Test_07_UploadJsonLinesFileForFineTuning()
		{
			Assert.IsNotNull(client);

			FileReference Ref = await client.UploadFile(
				"{\"prompt\":\"Was Kilroy here?\",\"completion\":\"Yes, Kilroy was here.\"}",
				"Kilroy.jsonl", Purpose.fine_tune);

			Console.Out.WriteLine(Ref.Id + "\t" + Ref.Purpose + "\t" + Ref.Bytes + "\t" + Ref.FileName);
		}

		[TestMethod]
		public async Task Test_08_GetFileReference()
		{
			Assert.IsNotNull(client);

			FileReference Ref = await client.UploadFile(
				"{\"prompt\":\"Was Kilroy here?\",\"completion\":\"Yes, Kilroy was here.\"}",
				"Kilroy.jsonl", Purpose.fine_tune);

			FileReference Ref2 = await client.GetFileReference(Ref.Id);

			Assert.AreEqual(Ref.Id, Ref2.Id);
			Assert.AreEqual(Ref.Bytes, Ref2.Bytes);
			Assert.AreEqual(Ref.Created, Ref2.Created);
			Assert.AreEqual(Ref.FileName, Ref2.FileName);
			Assert.AreEqual(Ref.Purpose, Ref2.Purpose);
		}

		[TestMethod]
		[Ignore]
		public async Task Test_09_GetFileContent()
		{
			Assert.IsNotNull(client);

			string Content = "{\"prompt\":\"Was Kilroy here?\",\"completion\":\"Yes, Kilroy was here.\"}";
			FileReference Ref = await client.UploadFile(
				Content, "Kilroy.jsonl", Purpose.fine_tune);

			object Obj = await client.GetFileContent(Ref.Id);
			if (Obj is string Content2)
				Assert.AreEqual(Content, Content2);
			else
				Assert.Fail();
		}

		[TestMethod]
		public async Task Test_10_DeleteFile()
		{
			Assert.IsNotNull(client);

			FileReference Ref = await client.UploadFile(
				"{\"prompt\":\"Was Kilroy here?\",\"completion\":\"Yes, Kilroy was here.\"}",
				"Kilroy.jsonl", Purpose.fine_tune);

			await Task.Delay(10000);

			await client.DeleteFile(Ref.Id);
		}

		[TestMethod]
		public async Task Test_11_ChatGPT_Streaming()
		{
			Assert.IsNotNull(client);

			bool Finished = false;

			Message Response = await client.ChatGPT("UnitTest",
				[
					new UserMessage("Can you write a 1000-character poem? If you cannot create a poem, create any text of 1000 characters. It must have at least three paragraphs, with empty rows between paragraphs. Please use Markdown to format the poem so it looks nice. Include a header, some text that is bold, some that is italic and some that is underlined.")
				],
				(sender, e) =>
				{
					Console.Error.Write(e.Diff);

					if (e.Finished)
						Finished = true;

					return Task.CompletedTask;
				}, null);

			Console.Error.WriteLine();
			Console.Error.WriteLine();
			Console.Error.WriteLine();
			Console.Error.WriteLine(Response.Content);

			Assert.IsTrue(Finished);
		}

		[TestMethod]
		public async Task Test_12_ChatGPT_FunctionCall()
		{
			Assert.IsNotNull(client);

			Message Response = await client.ChatGPT("UnitTest",
				[
					new UserMessage("I'm looking for images of Kermit. Can you suggest a representative image?")
				],
				[
					new("ShowImage", "Displays an image to the user.",
						new StringParameter("Url", "URL to the image to show.", true),
						new IntegerParameter("Width","Width of image, in pixels.", false, 0, false, null, false),
						new IntegerParameter("Height","Height of image, in pixels.", false, 0, false, null, false),
						new StringParameter("Alt", "Alternative textual description of image, in cases the image cannot be shown.", false)),
					new("ShowImages", "Displays an array of images to the user.",
						new ArrayParameter("Images", "Array of images to show.", true,
						new ObjectParameter("Image", "Information about an image.", true,
						new StringParameter("Url", "URL to the image to show.", true),
						new IntegerParameter("Width","Width of image, in pixels.", false, 0, false, null, false),
						new IntegerParameter("Height","Height of image, in pixels.", false, 0, false, null, false),
						new StringParameter("Alt", "Alternative textual description of image, in cases the image cannot be shown.", false))))
				]);

			Console.Out.WriteLine(Response.Content);

			Assert.IsNotNull(Response.FunctionName);
			Assert.IsNotNull(Response.FunctionArguments);
		}

		[TestMethod]
		public async Task Test_13_ChatGPT_Streaming_FunctionCalls()
		{
			Assert.IsNotNull(client);

			bool Finished = false;

			Message Response = await client.ChatGPT("UnitTest",
				[
					new UserMessage("I'm looking for images of Kermit. Can you suggest four or five representative images? Please only call provided functions, and do not return any text content.")
				],
				[
					new("ShowImage", "Displays an image to the user.",
						new StringParameter("Url", "URL to the image to show.", true),
						new IntegerParameter("Width","Width of image, in pixels.", false, 0, false, null, false),
						new IntegerParameter("Height","Height of image, in pixels.", false, 0, false, null, false),
						new StringParameter("Alt", "Alternative textual description of image, in cases the image cannot be shown.", false)),
					new("ShowImages", "Displays an array of images to the user.",
						new ArrayParameter("Images", "Array of images to show.", true,
						new ObjectParameter("Image", "Information about an image.", true,
						new StringParameter("Url", "URL to the image to show.", true),
						new IntegerParameter("Width","Width of image, in pixels.", false, 0, false, null, false),
						new IntegerParameter("Height","Height of image, in pixels.", false, 0, false, null, false),
						new StringParameter("Alt", "Alternative textual description of image, in cases the image cannot be shown.", false))))
				],
				(sender, e) =>
				{
					Console.Error.Write(e.Diff);

					if (e.Finished)
						Finished = true;

					return Task.CompletedTask;
				}, null);

			Console.Error.WriteLine();
			Console.Error.WriteLine();
			Console.Error.WriteLine();
			Console.Error.WriteLine(Response.Content);
			Console.Error.WriteLine();
			Console.Error.WriteLine(Response.FunctionName);
			Console.Error.WriteLine(JSON.Encode(Response.FunctionArguments, true));

			Assert.IsTrue(Finished);
			Assert.IsNotNull(Response.FunctionName);
			Assert.IsNotNull(Response.FunctionArguments);
		}

		[TestMethod]
		public async Task Test_14_ChatGPT_FunctionCall2()
		{
			Assert.IsNotNull(client);

			Message Response = await client.ChatGPT("UnitTest",
				[
					new UserMessage("Can you draw a graph of the derivative of the function f(x)=x^3+x^2?")
				],
				[
					new("DrawCurve2D", "Draws a graph of a 2D curve.",
						new ArrayParameter("XAxis", "Array of X-coordinate values.", true,
						new NumberParameter("X", "An X-coordinate value.", true)),
						new ArrayParameter("YAxis", "Array of Y-coordinate values.", true,
						new NumberParameter("Y", "An Y-coordinate value.", true)),
						new EnumerationParameter("Color", "Color of graph.",false,
							"Red", "Green", "Blue", "Orange", "Cyan"))
				]);

			Console.Error.WriteLine();
			Console.Error.WriteLine();
			Console.Error.WriteLine();
			Console.Error.WriteLine(Response.Content);
			Console.Error.WriteLine();
			Console.Error.WriteLine(Response.FunctionName);
			Console.Error.WriteLine(JSON.Encode(Response.FunctionArguments, true));

			Assert.IsNotNull(Response.FunctionName);
			Assert.IsNotNull(Response.FunctionArguments);
		}

	}
}