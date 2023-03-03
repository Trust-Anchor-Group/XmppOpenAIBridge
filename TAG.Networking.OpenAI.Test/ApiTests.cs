using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using TAG.Networking.OpenAI.Messages;
using Waher.Events;
using Waher.Events.Console;
using Waher.Networking.Sniffers;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Runtime.Inventory;
using Waher.Runtime.Inventory.Loader;
using Waher.Runtime.Settings;

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

			Message Response = await client.ChatGPT(
				new UserMessage("What is the OpenAI mission?"));

			Console.Out.WriteLine(Response.Content);
		}
	}
}