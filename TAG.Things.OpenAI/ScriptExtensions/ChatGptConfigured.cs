using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Things;

namespace TAG.Things.OpenAI.ScriptExtensions
{
	/// <summary>
	/// Checks if Chat GPT is configured.
	/// </summary>
	public class ChatGptConfigured : FunctionZeroVariables
	{
		/// <summary>
		/// Checks if Chat GPT is configured.
		/// </summary>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGptConfigured(int Start, int Length, Expression Expression)
			: base(Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(ChatGptConfigured);

		/// <summary>
		/// If the node (or its decendants) include asynchronous evaluation. Asynchronous nodes should be evaluated using
		/// <see cref="EvaluateAsync(Variables)"/>.
		/// </summary>
		public override bool IsAsynchronous => true;

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// This method should be used for nodes whose <see cref="IsAsynchronous"/> is false.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override IElement Evaluate(Variables Variables)
		{
			return this.EvaluateAsync(Variables).Result;
		}

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// This method should be used for nodes whose <see cref="IsAsynchronous"/> is true.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override async Task<IElement> EvaluateAsync(Variables Variables)
		{
			if (!Waher.IoTGateway.ScriptExtensions.Functions.GetNode.TryGetDataSource("MeteringTopology", out IDataSource Source))
				return BooleanValue.False;

			INode Node = await Source.GetNodeAsync(new ThingReference("ChatGPT"));
			if (Node is null)
				return BooleanValue.False;

			if (!(Node is ChatGPTXmppBridge ChatGpt))
				return BooleanValue.False;

			if (string.IsNullOrEmpty(ChatGpt.ApiKey))
				return BooleanValue.False;

			return BooleanValue.True;
		}
	}
}
