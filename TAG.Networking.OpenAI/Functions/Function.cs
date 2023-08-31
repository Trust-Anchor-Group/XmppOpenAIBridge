namespace TAG.Networking.OpenAI.Functions
{
	/// <summary>
	/// Information about a function that OpenAI can call.
	/// </summary>
	public class Function
	{
		/// <summary>
		/// The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// A description of what the function does, used by the model to choose when and how to call the function.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// The parameters the functions accepts, described as a JSON Schema object.
		/// </summary>
		public Parameter[] Parameters { get; set; }
	}
}
