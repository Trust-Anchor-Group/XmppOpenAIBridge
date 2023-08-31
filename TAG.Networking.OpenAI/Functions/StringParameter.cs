using System.Collections.Generic;

namespace TAG.Networking.OpenAI.Functions
{
	/// <summary>
	/// Information about a string parameter
	/// </summary>
	public class StringParameter : Parameter
	{
		/// <summary>
		/// Information about a string parameter
		/// </summary>
		/// <param name="Name">The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the function does, used by the model to choose when and how to call the function.</param>
		public StringParameter(string Name, string Description)
			: this(Name, Description, null, null, null, null)
		{
		}

		/// <summary>
		/// Information about a string parameter
		/// </summary>
		/// <param name="Name">The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the function does, used by the model to choose when and how to call the function.</param>
		/// <param name="MinLength">Minimum length.</param>
		/// <param name="MaxLength">Maximum length.</param>
		public StringParameter(string Name, string Description, int? MinLength, int? MaxLength)
			: this(Name, Description, MinLength, MaxLength, null, null)
		{
		}

		/// <summary>
		/// Information about a string parameter
		/// </summary>
		/// <param name="Name">The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the function does, used by the model to choose when and how to call the function.</param>
		/// <param name="Pattern">Regular expression.</param>
		public StringParameter(string Name, string Description, string Pattern)
			: this(Name, Description, null, null, Pattern, null)
		{
		}

		/// <summary>
		/// Information about a string parameter
		/// </summary>
		/// <param name="Name">The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the function does, used by the model to choose when and how to call the function.</param>
		/// <param name="Format">String parameter format.</param>
		public StringParameter(string Name, string Description, StringParameterFormat? Format)
			: this(Name, Description, null, null, null, Format)
		{
		}

		/// <summary>
		/// Information about a string parameter
		/// </summary>
		/// <param name="Name">The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, 
		/// with a maximum length of 64.</param>
		/// <param name="Description">A description of what the function does, used by the model to choose when and how to call the function.</param>
		/// <param name="MinLength">Minimum length.</param>
		/// <param name="MaxLength">Maximum length.</param>
		/// <param name="Pattern">Regular expression.</param>
		/// <param name="Format">String parameter format.</param>
		public StringParameter(string Name, string Description, int? MinLength, int? MaxLength, string Pattern, StringParameterFormat? Format)
			: base(Name, Description)
		{
			this.MinLength = MinLength;
			this.MaxLength = MaxLength;
			this.Pattern = Pattern;
			this.Format = Format;
		}

		/// <summary>
		/// Parameter Type
		/// </summary>
		public override ParameterType Type => ParameterType.String;

		/// <summary>
		/// Minimum length.
		/// </summary>
		public int? MinLength { get; }

		/// <summary>
		/// Maximum length.
		/// </summary>
		public int? MaxLength { get; }

		/// <summary>
		/// Regular expression pattern.
		/// </summary>
		public string Pattern { get; }

		/// <summary>
		/// String paramter format.
		/// </summary>
		public StringParameterFormat? Format { get; }

		/// <summary>
		/// Generates a JSON object for the parameter information.
		/// </summary>
		/// <returns>JSON object.</returns>
		public override Dictionary<string, object> ToJson()
		{
			Dictionary<string, object> Result = new Dictionary<string, object>()
			{
				{ "type", "string" },
				{ "description", this.Description }
			};

			if (this.MinLength.HasValue)
				Result["minLength"] = this.MinLength.Value;

			if (this.MaxLength.HasValue)
				Result["maxLength"] = this.MaxLength.Value;

			if (!string.IsNullOrEmpty(this.Pattern))
				Result["pattern"] = this.Pattern;

			if (this.Format.HasValue)
			{
				switch (this.Format.Value)
				{
					case StringParameterFormat.DateTime:
						Result["format"] = "date-time";
						break;

					case StringParameterFormat.Time:
						Result["format"] = "time";
						break;

					case StringParameterFormat.Date:
						Result["format"] = "date";
						break;

					case StringParameterFormat.Duration:
						Result["format"] = "duration";
						break;

					case StringParameterFormat.EMail:
						Result["format"] = "email";
						break;

					case StringParameterFormat.InternationalEMail:
						Result["format"] = "idn-email";
						break;

					case StringParameterFormat.HostName:
						Result["format"] = "hostname";
						break;

					case StringParameterFormat.InternationalHostname:
						Result["format"] = "idn-hostname";
						break;

					case StringParameterFormat.IPv4:
						Result["format"] = "ipv4";
						break;

					case StringParameterFormat.IPv6:
						Result["format"] = "ipv6";
						break;

					case StringParameterFormat.Uuid:
						Result["format"] = "uuid";
						break;

					case StringParameterFormat.Uri:
						Result["format"] = "uri";
						break;

					case StringParameterFormat.UriReference:
						Result["format"] = "uri-reference";
						break;

					case StringParameterFormat.Iri:
						Result["format"] = "iri";
						break;

					case StringParameterFormat.IriReference:
						Result["format"] = "iri-reference";
						break;

					case StringParameterFormat.UriTemplate:
						Result["format"] = "uri-template";
						break;

					case StringParameterFormat.JsonPointer:
						Result["format"] = "json-pointer";
						break;

					case StringParameterFormat.RelativeJsonPointer:
						Result["format"] = "relative-json-pointer";
						break;

					case StringParameterFormat.RegEx:
						Result["format"] = "regex";
						break;
				}
			}

			return Result;
		}
	}
}
