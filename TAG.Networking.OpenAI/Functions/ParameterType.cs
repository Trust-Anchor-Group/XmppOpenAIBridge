namespace TAG.Networking.OpenAI.Functions
{
	/// <summary>
	/// Function parameter type. Corresponds to JSON Schema types, defined in:
	/// https://json-schema.org/understanding-json-schema/
	/// </summary>
	public enum ParameterType
	{
		/// <summary>
		/// String parameter.
		/// </summary>
		String,

		/// <summary>
		/// Integer parameter
		/// </summary>
		Integer,

		/// <summary>
		/// Number parameter
		/// </summary>
		Number,

		/// <summary>
		/// Object parameter
		/// </summary>
		Object,

		/// <summary>
		/// Boolean parameter
		/// </summary>
		Boolean,

		/// <summary>
		/// Null parameter
		/// </summary>
		Null
	}
}
