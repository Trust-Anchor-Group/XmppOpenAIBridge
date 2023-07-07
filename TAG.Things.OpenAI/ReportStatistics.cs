using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Persistence.Filters;
using Waher.Persistence;
using Waher.Runtime.Language;
using Waher.Things;
using Waher.Things.Queries;
using Waher.Runtime.Counters.CounterObjects;
using System.Linq;
using Waher.Script.Functions.Vectors;

namespace TAG.Things.OpenAI
{
	/// <summary>
	/// Generates a report of communication usage.
	/// </summary>
	public class ReportStatistics : ICommand
	{
		private readonly OpenAiXmppExtensionNode node;

		/// <summary>
		/// Generates a report of communication usage.
		/// </summary>
		/// <param name="Node">Node performing the report.</param>
		public ReportStatistics(OpenAiXmppExtensionNode Node)
		{
			this.node = Node;
		}

		/// <summary>
		/// ID of command.
		/// </summary>
		public string CommandID => "Use";

		/// <summary>
		/// Type of command.
		/// </summary>
		public CommandType Type => CommandType.Query;

		/// <summary>
		/// Sort Category, if available.
		/// </summary>
		public string SortCategory => "OpenAI";

		/// <summary>
		/// Sort Key, if available.
		/// </summary>
		public string SortKey => "Use";

		/// <summary>
		/// Gets the name of data source.
		/// </summary>
		/// <param name="Language">Language to use.</param>
		public Task<string> GetNameAsync(Language Language)
		{
			return Language.GetStringAsync(typeof(ReportStatistics), 10, "Statistics...");
		}

		/// <summary>
		/// Gets a confirmation string, if any, of the command. If no confirmation is necessary, null, or the empty string can be returned.
		/// </summary>
		/// <param name="Language">Language to use.</param>
		public Task<string> GetConfirmationStringAsync(Language Language)
		{
			return Task.FromResult(string.Empty);
		}

		/// <summary>
		/// Gets a failure string, if any, of the command. If no specific failure string is available, null, or the empty string can be returned.
		/// </summary>
		/// <param name="Language">Language to use.</param>
		public Task<string> GetFailureStringAsync(Language Language)
		{
			return Task.FromResult(string.Empty);
		}

		/// <summary>
		/// Gets a success string, if any, of the command. If no specific success string is available, null, or the empty string can be returned.
		/// </summary>
		/// <param name="Language">Language to use.</param>
		public Task<string> GetSuccessStringAsync(Language Language)
		{
			return Task.FromResult(string.Empty);
		}

		/// <summary>
		/// If the command can be executed by the caller.
		/// </summary>
		/// <param name="Caller">Information about caller.</param>
		/// <returns>If the command can be executed by the caller.</returns>
		public Task<bool> CanExecuteAsync(RequestOrigin Caller)
		{
			return Task.FromResult(true);
		}

		/// <summary>
		/// Creates a copy of the command object.
		/// </summary>
		/// <returns>Copy of command object.</returns>
		public ICommand Copy()
		{
			return new ReportStatistics(this.node);
		}

		/// <summary>
		/// Executes the command.
		/// </summary>
		public Task ExecuteCommandAsync()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Starts the execution of a query.
		/// </summary>
		/// <param name="Query">Query data receptor.</param>
		/// <param name="Language">Language to use.</param>
		public Task StartQueryExecutionAsync(Query Query, Language Language)
		{
			this.Execute(Query, Language);
			return Task.CompletedTask;
		}

		private async void Execute(Query Query, Language Language)
		{
			try
			{
				await Query.Start();
				await Query.SetTitle(await Language.GetStringAsync(typeof(ReportStatistics), 11, "OpenAI Communication Statistics") + " - " + this.node.NodeId);

				Dictionary<string, long> Rx = new Dictionary<string, long>();
				Dictionary<string, long> Tx = new Dictionary<string, long>();
				SortedDictionary<string, bool> Users = new SortedDictionary<string, bool>();
				Dictionary<string, long> Counters;
				string s;

				foreach (RuntimeCounter Counter in await Database.Find<RuntimeCounter>(
					new FilterFieldLikeRegEx("Key", Database.WildcardToRegex(this.node.NodeId + ".*", "*"))))
				{
					s = Counter.Key;
					if (!s.StartsWith(this.node.NodeId + "."))
						continue;

					s = s.Substring(this.node.NodeId.Length);

					if (s.EndsWith(".Rx"))
						Counters = Rx;
					else if (s.EndsWith(".Tx"))
						Counters = Tx;
					else
						continue;

					s = s.Substring(0, s.Length - 2);

					if (s.Length == 1)
						s = string.Empty;
					else
						s = s.Substring(1, s.Length - 2);
					
					if (!string.IsNullOrEmpty(s))
						Users[s] = true;

					Counters[s] = Counter.Counter;
				}

				string Header = await Language.GetStringAsync(typeof(ReportStatistics), 12, "Total number of characters");
				await Query.BeginSection(Header);
				await Query.NewTable("Total", Header, new Column[]
				{
					new Column("Direction", await Language.GetStringAsync(typeof(ReportStatistics), 13, "Direction"),
						null, null, null, null, ColumnAlignment.Left, null),
					new Column("Characters", await Language.GetStringAsync(typeof(ReportStatistics), 14, "#Characters"),
						null, null, null, null, ColumnAlignment.Right, null)
				});

				if (Rx.TryGetValue(string.Empty, out long Count))
				{
					Rx.Remove(string.Empty);

					await Query.NewRecords("Total", new Record(new object[]
					{
						await Language.GetStringAsync(typeof(ReportStatistics), 15, "Received from users, sent to OpenAI"),
						Count
					}));
				}

				if (Tx.TryGetValue(string.Empty, out Count))
				{
					Tx.Remove(string.Empty);

					await Query.NewRecords("Total", new Record(new object[]
					{
						await Language.GetStringAsync(typeof(ReportStatistics), 16, "Received from OpenAI, returned to users"),
						Count
					}));
				}

				await Query.TableDone("Total");
				await Query.EndSection();

				Header = await Language.GetStringAsync(typeof(ReportStatistics), 17, "Number of characters per user");
				await Query.BeginSection(Header);
				await Query.NewTable("PerUser", Header, new Column[]
				{
					new Column("User", await Language.GetStringAsync(typeof(ReportStatistics), 18, "User"),
						null, null, null, null, ColumnAlignment.Left, null),
					new Column("Rx", await Language.GetStringAsync(typeof(ReportStatistics), 19, "From User"),
						null, null, null, null, ColumnAlignment.Right, null),
					new Column("Tx", await Language.GetStringAsync(typeof(ReportStatistics), 20, "To User"),
						null, null, null, null, ColumnAlignment.Right, null)
				});

				List<Record> Records = new List<Record>();

				foreach (KeyValuePair<string, bool> P in Users)
				{
					if (Rx.TryGetValue(P.Key, out long RxCount))
						Rx.Remove(P.Key);
					else
						RxCount = 0;

					if (Tx.TryGetValue(P.Key, out long TxCount))
						Tx.Remove(P.Key);
					else
						TxCount = 0;

					Records.Add(new Record(new object[]
					{
						P.Key,
						RxCount,
						TxCount
					}));
				}

				foreach (KeyValuePair<string, long> P in Rx)
				{
					Records.Add(new Record(new object[]
					{
						P.Key,
						P.Value,
						null
					}));
				}

				foreach (KeyValuePair<string, long> P in Tx)
				{
					Records.Add(new Record(new object[]
					{
						P.Key,
						null,
						P.Value
					}));
				}

				await Query.NewRecords("PerUser", Records.ToArray());
				await Query.TableDone("PerUser");
				await Query.EndSection();
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
				await Query.LogMessage(QueryEventType.Exception, QueryEventLevel.Major, ex.Message);
			}
			finally
			{
				await Query.Done();
			}
		}
	}
}
