using System.Collections.Generic;

namespace TAG.Networking.OpenAI.Functions
{
	/// <summary>
	/// Information about a function parameter.
	/// </summary>
	public abstract class Parameter
	{
		/// <summary>
		/// Information about a function parameter.
		/// </summary>
		/// <param name="Name">The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the function does, used by the model to choose when and how to call the function.</param>
		/// <param name="Required">If parameter is required or not.</param>
		public Parameter(string Name, string Description, bool Required)
		{
			this.Name = Name;
			this.Description = Description;
			this.Required = Required;
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
		/// If parameter is required or not.
		/// </summary>
		public bool Required { get; }

		/// <summary>
		/// Parameter Type
		/// </summary>
		public abstract ParameterType Type { get; }

		/// <summary>
		/// Generates a JSON object for the parameter information.
		/// </summary>
		/// <returns>JSON object.</returns>
		public abstract Dictionary<string, object> ToJson();
	}
}
