using System.Collections.Generic;

namespace TAG.Networking.OpenAI.Functions
{
	/// <summary>
	/// Information about an array parameter
	/// </summary>
	public class ArrayParameter : Parameter
	{
		/// <summary>
		/// Information about an array parameter
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="ItemParameter">Item parameter.</param>
		public ArrayParameter(string Name, string Description, bool Required, Parameter ItemParameter)
			: base(Name, Description, Required)
		{
			this.ItemParameter = ItemParameter;
		}

		/// <summary>
		/// Parameter Type
		/// </summary>
		public override ParameterType Type => ParameterType.Array;

		/// <summary>
		/// Item parameter
		/// </summary>
		public Parameter ItemParameter { get; }

		/// <summary>
		/// Generates a JSON object for the parameter information.
		/// </summary>
		/// <returns>JSON object.</returns>
		public override Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> Result = new Dictionary<string, object>()
			{
				{ "type", "array" },
				{ "items", this.ItemParameter.ToJson() }
			};

			return Result;
		}
	}
}
