using System;
using System.IO;
using System.Threading.Tasks;
using Waher.Content.Markdown;
using Waher.Events;
using Waher.IoTGateway;
using Waher.Runtime.Inventory;
using Waher.Runtime.Timing;
using Waher.Things;

namespace TAG.Content.Markdown.OpenAI
{
	/// <summary>
	/// Module that controls the life-cycle of the OpenAI integration.
	/// </summary>
	public class OpenAIModule : IModule
	{
		private static readonly Random rnd = new Random();
		private static IMarkdownAsynchronousOutput asyncHtmlOutput = null;

		/// <summary>
		/// Module that controls the life-cycle of the OpenAI integration.
		/// </summary>
		public OpenAIModule()
		{
		}

		/// <summary>
		/// If the OpenAI module has been initialized and started properly.
		/// </summary>
		public static bool Initialized
		{
			get;
			private set;
		}

		/// <summary>
		/// OpenAI Content folder.
		/// </summary>
		public static string OpenAIContentFolder
		{
			get;
			internal set;
		}

		/// <summary>
		/// If the module owns the <see cref="Scheduler"/>.
		/// </summary>
		public static bool SchedulerOwnership
		{
			get;
			internal set;
		}

		public static IDataSource DefaultSource
		{
			get;
			internal set;
		}

		/// <summary>
		/// Interface for outputting asynchronously generated HTML to clients.
		/// </summary>
		public static IMarkdownAsynchronousOutput AsyncHtmlOutput => asyncHtmlOutput;

		/// <summary>
		/// Starts the module.
		/// </summary>
		public Task Start()
		{
			Initialized = false;

			if (!Types.TryGetModuleParameter("Sources", out object Obj) ||
				!(Obj is IDataSource[] Sources) ||
				!Types.TryGetModuleParameter("DefaultSource", out Obj) ||
				!(Obj is string DefaultSourceID))
			{
				return Task.CompletedTask;
			}

			asyncHtmlOutput = Types.FindBest<IMarkdownAsynchronousOutput, MarkdownOutputType>(MarkdownOutputType.Html);
			DefaultSource = null;

			foreach (IDataSource Source in Sources)
			{
				if (Source.SourceID == DefaultSourceID)
				{
					DefaultSource = Source;
					break;
				}
			}

			if (DefaultSource is null)
				return Task.CompletedTask;

			OpenAIContentFolder = Path.Combine(Gateway.RootFolder, "OpenAI");

			if (!Directory.Exists(OpenAIContentFolder))
				Directory.CreateDirectory(OpenAIContentFolder);

			Initialized = true;

			DeleteOldFiles(TimeSpan.FromDays(7));

			return Task.CompletedTask;
		}

		/// <summary>
		/// Stops the module.
		/// </summary>
		public Task Stop()
		{
			Initialized = false;

			return Task.CompletedTask;
		}

		private static void DeleteOldFiles(object P)
		{
			if (P is TimeSpan MaxAge)
				DeleteOldFiles(MaxAge, true);
		}

		private static void DeleteOldFiles(TimeSpan MaxAge, bool Reschedule)
		{
			DateTime Limit = DateTime.Now - MaxAge;
			int Count = 0;

			foreach (string FileName in Directory.GetFiles(OpenAIContentFolder, "*.*"))
			{
				if (File.GetLastAccessTime(FileName) < Limit)
				{
					try
					{
						File.Delete(FileName);
						Count++;
					}
					catch (Exception ex)
					{
						Log.Error("Unable to delete old file: " + ex.Message, FileName);
					}
				}
			}

			if (Count > 0)
				Log.Informational(Count.ToString() + " old file(s) deleted.", OpenAIContentFolder);

			if (Reschedule)
			{
				DateTime TP;

				lock (rnd)
				{
					TP = DateTime.Now.AddDays(rnd.NextDouble() * 2);
				}

				Gateway.ScheduleEvent(DeleteOldFiles, TP, MaxAge);
			}
		}

	}
}
