using System.Collections.Generic;
using TAG.Networking.OpenAI.Functions;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace TAG.Things.OpenAI.ScriptExtensions
{
	/// <summary>
	/// Creates a Function definition for a ChatGPT function callbacks.
	/// </summary>
	public class ChatGptFunction : FunctionMultiVariate
	{
		/// <summary>
		/// Creates a Function definition for a ChatGPT function callbacks.
		/// </summary>
		/// <param name="Name">The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the function does, used by the model to choose when and how to call the function.</param>
		/// <param name="Parameters">Vector of function parameter definitions.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGptFunction(ScriptNode Name, ScriptNode Description, ScriptNode Parameters,
			int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Name, Description, Parameters },
				  new ArgumentType[] { ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Vector },
				  Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(ChatGptFunction);

		/// <summary>
		/// Default argument names.
		/// </summary>
		public override string[] DefaultArgumentNames => new string[] { "Name", "Description", "Parameters" };

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// This method should be used for nodes whose <see cref="IsAsynchronous"/> is false.
		/// </summary>
		/// <param name="Arguments">Arguments.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override IElement Evaluate(IElement[] Arguments, Variables Variables)
		{
			string Name = Arguments[0].AssociatedObjectValue?.ToString() ?? string.Empty;
			string Description = Arguments[1].AssociatedObjectValue?.ToString() ?? string.Empty;
			IVector V = Arguments[2].AssociatedObjectValue as IVector;

			if (V is null)
				throw new ScriptRuntimeException("Expected function parameters to be a vector.", this);

			List<Parameter> Parameters = new List<Parameter>();

			foreach (IElement Parameter in V.VectorElements)
			{
				if (!(Parameter.AssociatedObjectValue is Parameter P))
					throw new ScriptRuntimeException("Each function parameter must be a ChatGPT parameter.", this);

				Parameters.Add(P);
			}

			return new ObjectValue(new Networking.OpenAI.Functions.Function(Name, Description, Parameters.ToArray()));
		}
	}
}
