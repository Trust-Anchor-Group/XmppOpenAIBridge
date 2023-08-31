using System.Collections.Generic;

namespace TAG.Networking.OpenAI.Functions
{
	/// <summary>
	/// Information about a Boolean parameter
	/// </summary>
	public class BooleanParameter : Parameter
	{
		/// <summary>
		/// Information about a Boolean parameter
		/// </summary>
		/// <param name="Name">The name of the parameter. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the parameter means.</param>
		/// <param name="Required">If parameter is required or not.</param>
		public BooleanParameter(string Name, string Description, bool Required)
			: base(Name, Description, Required)
		{
		}

		/// <summary>
		/// Parameter Type
		/// </summary>
		public override ParameterType Type => ParameterType.Boolean;

		/// <summary>
		/// Generates a JSON object for the parameter information.
		/// </summary>
		/// <returns>JSON object.</returns>
		public override Dictionary<string, object> ToJson()
		{
			return new Dictionary<string, object>()
			{
				{ "type", "boolean" },
				{ "description", this.Description }
			};
		}
	}
}
