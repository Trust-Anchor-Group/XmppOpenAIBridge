using System.Collections.Generic;

namespace TAG.Networking.OpenAI.Functions
{
	/// <summary>
	/// Information about an enumeration parameter
	/// </summary>
	public class EnumerationParameter : Parameter
	{
		/// <summary>
		/// Information about an enumeration parameter
		/// </summary>
		/// <param name="Name">The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the function does, used by the model to choose when and how to call the function.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="Values">Enuemration values.</param>
		public EnumerationParameter(string Name, string Description, bool Required, params string[] Values)
			: base(Name, Description, Required)
		{
			this.Values = Values;
		}

		/// <summary>
		/// Parameter Type
		/// </summary>
		public override ParameterType Type => ParameterType.String;

		/// <summary>
		/// Enumeration values.
		/// </summary>
		public string[] Values { get; }

		/// <summary>
		/// Generates a JSON object for the parameter information.
		/// </summary>
		/// <returns>JSON object.</returns>
		public override Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> Result = new Dictionary<string, object>()
			{
				{ "type", "string" },
				{ "description", this.Description },
				{ "enum", this.Values }
			};

			return Result;
		}
	}
}
