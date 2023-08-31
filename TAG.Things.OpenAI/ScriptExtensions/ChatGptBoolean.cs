using TAG.Networking.OpenAI.Functions;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Operators.Conditional;

namespace TAG.Things.OpenAI.ScriptExtensions
{
	/// <summary>
	/// Creates a Boolean parameter for a ChatGPT function definition.
	/// </summary>
	public class ChatGptBoolean : FunctionMultiVariate
	{
		/// <summary>
		/// Creates a Boolean parameter for a ChatGPT function definition.
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGptBoolean(ScriptNode Name, ScriptNode Description, ScriptNode Required,
			int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Name, Description, Required },
				  argumentTypes3Scalar, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(ChatGptBoolean);

		/// <summary>
		/// Default argument names.
		/// </summary>
		public override string[] DefaultArgumentNames => new string[] { "Name", "Description", "Required" };

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

			return new ObjectValue(new BooleanParameter(Name, Description, Required));
		}
	}
}
