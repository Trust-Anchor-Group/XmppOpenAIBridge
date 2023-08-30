using System.Threading.Tasks;
using TAG.Networking.OpenAI.Messages;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Operators.Conditional;
using Waher.Things;
using Waher.Things.Metering;

namespace TAG.Things.OpenAI.ScriptExtensions
{
	/// <summary>
	/// Provides access to Open AI ChatGPT chat completion API.
	/// </summary>
	public class ChatGpt : FunctionMultiVariate
	{
		/// <summary>
		/// Provides access to Open AI ChatGPT chat completion API.
		/// </summary>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGpt(ScriptNode Instruction, ScriptNode Sender, ScriptNode Text, ScriptNode History, ScriptNode Preview,
			int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Instruction, Sender, Text, History, Preview }, argumentTypes5Scalar, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => throw new System.NotImplementedException();

		/// <summary>
		/// Default argument names.
		/// </summary>
		public override string[] DefaultArgumentNames => new string[] { "Instruction", "Sender", "Text", "History", "Preview" };

		/// <summary>
		/// If the node (or its decendants) include asynchronous evaluation. Asynchronous nodes should be evaluated using
		/// <see cref="EvaluateAsync(Variables)"/>.
		/// </summary>
		public override bool IsAsynchronous => true;

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// This method should be used for nodes whose <see cref="IsAsynchronous"/> is false.
		/// </summary>
		/// <param name="Arguments">Arguments.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override IElement Evaluate(IElement[] Arguments, Variables Variables)
		{
			return this.EvaluateAsync(Arguments, Variables).Result;
		}

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// This method should be used for nodes whose <see cref="IsAsynchronous"/> is false.
		/// </summary>
		/// <param name="Arguments">Arguments.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override async Task<IElement> EvaluateAsync(IElement[] Arguments, Variables Variables)
		{
			if (!Waher.IoTGateway.ScriptExtensions.Functions.GetNode.TryGetDataSource(MeteringTopology.SourceID, out IDataSource Source))
				throw new ScriptRuntimeException(MeteringTopology.SourceID + " data source not found.", this);

			INode Node = await Source.GetNodeAsync(new ThingReference("ChatGPT"));
			if (Node is null)
				throw new ScriptRuntimeException("ChatGPT node not found in data source " + MeteringTopology.SourceID + ".", this);

			if (!(Node is ChatGPTXmppBridge ChatGpt))
				throw new ScriptRuntimeException("ChatGPT node not of expected type.", this);

			if (string.IsNullOrEmpty(ChatGpt.ApiKey))
				throw new ScriptRuntimeException("ChatGPT node not configured with API key.", this);

			string Instruction = Arguments[0].AssociatedObjectValue?.ToString() ?? string.Empty;
			string Sender = Arguments[1].AssociatedObjectValue?.ToString() ?? string.Empty;
			string Text = Arguments[2].AssociatedObjectValue?.ToString() ?? string.Empty;
			bool History = If.ToBoolean(Arguments[3]) ?? throw new ScriptRuntimeException("Expected Boolean History argument (4th)", this);
			bool Preview = If.ToBoolean(Arguments[4]) ?? throw new ScriptRuntimeException("Expected Boolean Preview argument (5th)", this);

			Message Response = await ChatGpt.ChatQueryWithHistory(Sender, Text, Instruction, History, (sender, e) =>
			{
				if (Preview && Variables.HandlesPreview)
					Variables.Preview(this.Expression, new StringValue(e.Total));

				return Task.CompletedTask;
			}, null);

			return new StringValue(Response.Content);
		}
	}
}
