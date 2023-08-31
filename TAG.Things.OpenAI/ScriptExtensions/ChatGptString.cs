using System;
using TAG.Networking.OpenAI.Functions;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Operators.Conditional;

namespace TAG.Things.OpenAI.ScriptExtensions
{
	/// <summary>
	/// Creates a String parameter for a ChatGPT function definition.
	/// </summary>
	public class ChatGptString : FunctionMultiVariate
	{
		/// <summary>
		/// Creates a String parameter for a ChatGPT function definition.
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="MinLength">Minimum length.</param>
		/// <param name="MaxLength">Maximum length.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGptString(ScriptNode Name, ScriptNode Description, ScriptNode Required,
			ScriptNode MinLength, ScriptNode MaxLength, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Name, Description, Required, MinLength, MaxLength },
				  argumentTypes5Scalar, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Creates a String parameter for a ChatGPT function definition.
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="PatternFormat">Regular expression pattern, or string format enumeration value.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGptString(ScriptNode Name, ScriptNode Description, ScriptNode Required,
			ScriptNode PatternFormat, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Name, Description, Required, PatternFormat },
				  argumentTypes4Scalar, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Creates a String parameter for a ChatGPT function definition.
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="PatternFormat">Regular expression pattern, or string format enumeration value.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGptString(ScriptNode Name, ScriptNode Description, ScriptNode Required,
			int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Name, Description, Required },
				  argumentTypes3Scalar, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(ChatGptString);

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
			int? MinLength = null;
			int? MaxLength = null;
			string Pattern = null;
			StringParameterFormat? Format = null;

			switch (Arguments.Length)
			{
				case 3:
				default:
					break;

				case 4:
					Pattern = Arguments[3].AssociatedObjectValue?.ToString() ?? string.Empty;
					if (Enum.TryParse(Pattern, out StringParameterFormat ParsedFormat))
					{
						Format = ParsedFormat;
						Pattern = null;
					}
					else
						Format = null;
					break;

				case 5:
					object Obj = Arguments[3].AssociatedObjectValue;
					if (!(Obj is null))
						MinLength = (int)Expression.ToDouble(Obj);

					Obj = Arguments[4].AssociatedObjectValue;
					if (!(Obj is null))
						MaxLength = (int)Expression.ToDouble(Obj);

					break;
			}

			return new ObjectValue(new StringParameter(Name, Description, Required, MinLength, MaxLength, Pattern, Format));
		}


	}
}
