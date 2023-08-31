using TAG.Networking.OpenAI.Functions;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Operators.Conditional;

namespace TAG.Things.OpenAI.ScriptExtensions
{
	/// <summary>
	/// Creates a Number parameter for a ChatGPT function definition.
	/// </summary>
	public class ChatGptNumber : FunctionMultiVariate
	{
		/// <summary>
		/// Creates a Number parameter for a ChatGPT function definition.
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="MultipleOf">If value is a multiple of this base value.</param>
		/// <param name="MinValue">Minimum length.</param>
		/// <param name="MinValidIncluded">If minimum value is included in the range.</param>
		/// <param name="MaxValue">Maximum length.</param>
		/// <param name="MaxValidIncluded">If maximum value is included in the range.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGptNumber(ScriptNode Name, ScriptNode Description, ScriptNode Required, ScriptNode MultipleOf,
			ScriptNode MinValue, ScriptNode MinValidIncluded, ScriptNode MaxValue, ScriptNode MaxValidIncluded, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Name, Description, Required, MultipleOf, MinValue, MinValidIncluded, MaxValue, MaxValidIncluded },
				  argumentTypes8Scalar, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Creates a Number parameter for a ChatGPT function definition.
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="MinValue">Minimum length.</param>
		/// <param name="MinValidIncluded">If minimum value is included in the range.</param>
		/// <param name="MaxValue">Maximum length.</param>
		/// <param name="MaxValidIncluded">If maximum value is included in the range.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGptNumber(ScriptNode Name, ScriptNode Description, ScriptNode Required,
			ScriptNode MinValue, ScriptNode MinValidIncluded, ScriptNode MaxValue, ScriptNode MaxValidIncluded, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Name, Description, Required, MinValue, MinValidIncluded, MaxValue, MaxValidIncluded },
				  argumentTypes7Scalar, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Creates a Number parameter for a ChatGPT function definition.
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="MultipleOf">If value is a multiple of this base value.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGptNumber(ScriptNode Name, ScriptNode Description, ScriptNode Required, ScriptNode MultipleOf,
			int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Name, Description, Required, MultipleOf },
				  argumentTypes4Scalar, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Creates a Number parameter for a ChatGPT function definition.
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ChatGptNumber(ScriptNode Name, ScriptNode Description, ScriptNode Required,
			int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Name, Description, Required },
				  argumentTypes3Scalar, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(ChatGptNumber);

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
			double? MinValue = null;
			bool MinValueIncluded = false;
			double? MaxValue = null;
			bool MaxValueIncluded = false;
			double? MultipleOf = null;

			switch (Arguments.Length)
			{
				case 3:
				default:
					break;

				case 4:
					object Obj = Arguments[3].AssociatedObjectValue;
					if (!(Obj is null))
						MultipleOf = Expression.ToDouble(Obj);
					break;

				case 7:
					Obj = Arguments[3].AssociatedObjectValue;
					if (!(Obj is null))
						MinValue = Expression.ToDouble(Obj);

					MinValueIncluded = If.ToBoolean(Arguments[4]) ?? false;

					Obj = Arguments[5].AssociatedObjectValue;
					if (!(Obj is null))
						MaxValue = Expression.ToDouble(Obj);

					MaxValueIncluded = If.ToBoolean(Arguments[6]) ?? false;
					break;

				case 8:
					Obj = Arguments[3].AssociatedObjectValue;
					if (!(Obj is null))
						MultipleOf = Expression.ToDouble(Obj);

					Obj = Arguments[4].AssociatedObjectValue;
					if (!(Obj is null))
						MinValue = Expression.ToDouble(Obj);

					MinValueIncluded = If.ToBoolean(Arguments[5]) ?? false;

					Obj = Arguments[6].AssociatedObjectValue;
					if (!(Obj is null))
						MaxValue = Expression.ToDouble(Obj);

					MaxValueIncluded = If.ToBoolean(Arguments[7]) ?? false;
					break;
			}

			return new ObjectValue(new NumberParameter(Name, Description, Required, MinValue, MinValueIncluded, MaxValue, MaxValueIncluded, MultipleOf));
		}


	}
}
