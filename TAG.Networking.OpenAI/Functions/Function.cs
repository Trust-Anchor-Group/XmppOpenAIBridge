using System.Collections.Generic;

namespace TAG.Networking.OpenAI.Functions
{
	/// <summary>
	/// Information about a function that OpenAI can call.
	/// </summary>
	public class Function
	{
		/// <summary>
		/// Information about a function that OpenAI can call.
		/// </summary>
		/// <param name="Name">Function name</param>
		/// <param name="Description">Function description</param>
		/// <param name="Parameters">Function parameters</param>
		public Function(string Name, string Description, params Parameter[] Parameters)
		{
			this.Name = Name;
			this.Description = Description;
			this.Parameters = Parameters;
		}

		/// <summary>
		/// The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// A description of what the function does, used by the model to choose when and how to call the function.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// The parameters the functions accepts, described as a JSON Schema object.
		/// </summary>
		public Parameter[] Parameters { get; }

		/// <summary>
		/// Generates a JSON object for the function information.
		/// </summary>
		/// <returns>JSON object.</returns>
		public Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> Properties = new Dictionary<string, object>();
			List<string> Required = new List<string>();

			foreach (Parameter P in this.Parameters)
			{
				Properties[P.Name] = P.ToJson();

				if (P.Required)
					Required.Add(P.Name);
			}

			Dictionary<string, object> Result = new Dictionary<string, object>()
			{
				{ "name", this.Name },
				{ "description", this.Description },
				{ "parameters", new Dictionary<string, object>()
					{
						{ "type", "object" },
						{ "properties", Properties },
						{ "required", Required.ToArray() }
					}
				}
			};

			return Result;
		}
	}
}
