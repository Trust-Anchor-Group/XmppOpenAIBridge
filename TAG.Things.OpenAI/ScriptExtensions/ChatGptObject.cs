using System.Collections.Generic;
using TAG.Networking.OpenAI.Functions;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Operators.Conditional;

namespace TAG.Things.OpenAI.ScriptExtensions
{
	/// <summary>
	/// Creates an Object parameter for a ChatGPT function definition.
	/// </summary>
	public class ChatGptObject : FunctionMultiVariate
	{
		/// <summary>
		/// Creates an Object parameter for a ChatGPT function definition.
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="Properties">Vector of object property definitions.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGptObject(ScriptNode Name, ScriptNode Description, ScriptNode Required, ScriptNode Properties,
			int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Name, Description, Required, Properties },
				  new ArgumentType[] { ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Vector },
				  Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(ChatGptObject);

		/// <summary>
		/// Default argument names.
		/// </summary>
		public override string[] DefaultArgumentNames => new string[] { "Name", "Description", "Required", "Properties" };

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
			bool Required = If.ToBoolean(Arguments[2]) ?? false;
			IVector V = Arguments[3] as IVector;

			if (V is null)
				throw new ScriptRuntimeException("Expected object properties to be a vector.", this);

			List<Parameter> Properties = new List<Parameter>();

			foreach (IElement Property in V.VectorElements)
			{
				if (!(Property.AssociatedObjectValue is Parameter P))
					throw new ScriptRuntimeException("Each object property must be a ChatGPT parameter.", this);

				Properties.Add(P);
			}

			return new ObjectValue(new ObjectParameter(Name, Description, Required, Properties.ToArray()));
		}
	}
}
