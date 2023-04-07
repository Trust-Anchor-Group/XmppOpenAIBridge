using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using TAG.Networking.OpenAI.Files;
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
			Log.Terminate();
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

		[ClassCleanup]
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

			KeyValuePair<string, TemporaryStream> P = await InternetContent.GetTempStreamAsync(ImageUri);

			await CopyAndDisposeFile(P.Value, P.Key, "Test_04");
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

				KeyValuePair<string, TemporaryStream> P = await InternetContent.GetTempStreamAsync(Uri);

				await CopyAndDisposeFile(P.Value, P.Key, "Test_05_" + i.ToString());

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

			await Task.Delay(1000);

			await client.DeleteFile(Ref.Id);
		}

	}
}