using System.Collections.Generic;

namespace TAG.Networking.OpenAI.Functions
{
	/// <summary>
	/// Information about an object parameter
	/// </summary>
	public class ObjectParameter : Parameter
	{
		/// <summary>
		/// Information about an object parameter
		/// </summary>
		/// <param name="Name">The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the function does, used by the model to choose when and how to call the function.</param>
		/// <param name="Required">If parameter is required or not.</param>
		/// <param name="Properties">Object properties.</param>
		public ObjectParameter(string Name, string Description, bool Required, params Parameter[] Properties)
			: base(Name, Description, Required)
		{
			this.Properties = Properties;
		}

		/// <summary>
		/// Parameter Type
		/// </summary>
		public override ParameterType Type => ParameterType.Object;

		/// <summary>
		/// Object properties
		/// </summary>
		public Parameter[] Properties { get; }

		/// <summary>
		/// Generates a JSON object for the parameter information.
		/// </summary>
		/// <returns>JSON object.</returns>
		public override Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> Properties = new Dictionary<string, object>();
			List<string> Required = new List<string>();

			foreach (Parameter P in this.Properties)
			{
				Properties[P.Name] = P.ToJson();

				if (P.Required)
					Required.Add(P.Name);
			}

			Dictionary<string, object> Result = new Dictionary<string, object>()
			{
				{ "type", "object" },
				{ "description", this.Description },
				{ "properties", Properties },
				{ "required", Required.ToArray() }
			};

			return Result;
		}
	}
}
