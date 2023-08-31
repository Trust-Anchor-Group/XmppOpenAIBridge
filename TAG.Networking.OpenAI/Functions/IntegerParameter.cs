using System.Collections.Generic;

namespace TAG.Networking.OpenAI.Functions
{
	/// <summary>
	/// Information about an integer parameter
	/// </summary>
	public class IntegerParameter : Parameter
	{
		/// <summary>
		/// Information about an integer parameter
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		public IntegerParameter(string Name, string Description, bool Required)
			: this(Name, Description, Required, null, false, null, false, null)
		{
		}

		/// <summary>
		/// Information about an integer parameter
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="MultipleOf">If values are a multiple of this base value.</param>
		public IntegerParameter(string Name, string Description, bool Required, int? MultipleOf)
			: this(Name, Description, Required, null, false, null, false, MultipleOf)
		{
		}

		/// <summary>
		/// Information about an integer parameter
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="MinValue">Minimum value.</param>
		/// <param name="MinValueInclusive">If minimum value is included in the range.</param>
		/// <param name="MaxValue">Maximum value.</param>
		/// <param name="MaxValueInclusive">If maximum value is included in the range.</param>
		public IntegerParameter(string Name, string Description, bool Required, int? MinValue, bool MinValueInclusive,
			int? MaxValue, bool MaxValueInclusive)
			: this(Name, Description, Required, MinValue, MinValueInclusive, MaxValue, MaxValueInclusive, null)
		{
		}

		/// <summary>
		/// Information about an integer parameter
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="MinValue">Minimum value.</param>
		/// <param name="MinValueInclusive">If minimum value is included in the range.</param>
		/// <param name="MaxValue">Maximum value.</param>
		/// <param name="MaxValueInclusive">If maximum value is included in the range.</param>
		/// <param name="MultipleOf">If values are a multiple of this base value.</param>
		public IntegerParameter(string Name, string Description, bool Required, int? MinValue, bool MinValueInclusive,
			int? MaxValue, bool MaxValueInclusive, int? MultipleOf)
			: base(Name, Description, Required)
		{
			this.MinValue = MinValue;
			this.MinValueInclusive = MinValueInclusive;
			this.MaxValue = MaxValue;
			this.MaxValueInclusive = MaxValueInclusive;
			this.MultipleOf = MultipleOf;
		}

		/// <summary>
		/// Parameter Type
		/// </summary>
		public override ParameterType Type => ParameterType.Integer;

		/// <summary>
		/// Minimum length.
		/// </summary>
		public int? MinValue { get; }

		/// <summary>
		/// If range includes the minimum value.
		/// </summary>
		public bool MinValueInclusive { get; }

		/// <summary>
		/// Maximum length.
		/// </summary>
		public int? MaxValue { get; }

		/// <summary>
		/// If range includes the maximum value.
		/// </summary>
		public bool MaxValueInclusive { get; }

		/// <summary>
		/// If values are multiples of this base value.
		/// </summary>
		public int? MultipleOf { get; }

		/// <summary>
		/// Generates a JSON object for the parameter information.
		/// </summary>
		/// <returns>JSON object.</returns>
		public override Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> Result = new Dictionary<string, object>()
			{
				{ "type", "integer" },
				{ "description", this.Description }
			};

			if (this.MinValue.HasValue)
			{
				if (this.MinValueInclusive)
					Result["minimum"] = this.MinValue.Value;
				else
					Result["exclusiveMinimum"] = this.MinValue.Value;
			}

			if (this.MaxValue.HasValue)
			{
				if (this.MaxValueInclusive)
					Result["maximum"] = this.MaxValue.Value;
				else
					Result["exclusiveMaximum"] = this.MaxValue.Value;
			}

			if (this.MultipleOf.HasValue)
				Result["multipleOf"] = this.MultipleOf.Value;

			return Result;
		}
	}
}
