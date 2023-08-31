namespace TAG.Networking.OpenAI.Functions
{
	/// <summary>
	/// String parameter formats. Reference:
	/// https://json-schema.org/understanding-json-schema/reference/string.html
	/// </summary>
	public enum StringParameterFormat
	{
		/// <summary>
		/// "date-time": Date and time together, for example, 2018-11-13T20:20:39+00:00.
		/// </summary>
		DateTime,

		/// <summary>
		/// "time": New in draft 7 Time, for example, 20:20:39+00:00
		/// </summary>
		Time,

		/// <summary>
		/// "date": New in draft 7 Date, for example, 2018-11-13.
		/// </summary>
		Date,

		/// <summary>
		/// "duration": New in draft 2019-09 A duration as defined by the ISO 8601 ABNF for “duration”. For example, P3D expresses a duration of 3 days.
		/// </summary>
		Duration,

		/// <summary>
		/// "email": Internet email address, see RFC 5321, section 4.1.2.
		/// </summary>
		EMail,

		/// <summary>
		/// "idn-email": New in draft 7 The internationalized form of an Internet email address, see RFC 6531. 
		/// </summary>
		InternationalEMail,

		/// <summary>
		/// "hostname": Internet host name, see RFC 1123, section 2.1.
		/// </summary>
		HostName,

		/// <summary>
		/// "idn-hostname": New in draft 7 An internationalized Internet host name, see RFC5890, section 2.3.2.3.
		/// </summary>
		InternationalHostname,

		/// <summary>
		/// "ipv4": IPv4 address, according to dotted-quad ABNF syntax as defined in RFC 2673, section 3.2.
		/// </summary>
		IPv4,

		/// <summary>
		/// "ipv6": IPv6 address, as defined in RFC 2373, section 2.2.
		/// </summary>
		IPv6,

		/// <summary>
		/// "uuid": New in draft 2019-09 A Universally Unique Identifier as defined by RFC 4122. Example: 3e4666bf-d5e5-4aa7-b8ce-cefe41c7568a
		/// </summary>
		Uuid,

		/// <summary>
		/// "uri": A universal resource identifier (URI), according to RFC3986.
		/// </summary>
		Uri,

		/// <summary>
		/// "uri-reference": New in draft 6 A URI Reference (either a URI or a relative-reference), according to RFC3986, section 4.1.
		/// </summary>
		UriReference,

		/// <summary>
		/// "iri": New in draft 7 The internationalized equivalent of a “uri”, according to RFC3987.
		/// </summary>
		Iri,

		/// <summary>
		/// "iri-reference": New in draft 7 The internationalized equivalent of a “uri-reference”, according to RFC3987
		/// </summary>
		IriReference,

		/// <summary>
		/// "uri-template": New in draft 6 A URI Template (of any level) according to RFC6570. If you don’t already know what a URI Template is, you probably don’t need this value.
		/// </summary>
		UriTemplate,

		/// <summary>
		/// "json-pointer": New in draft 6 A JSON Pointer, according to RFC6901. There is more discussion on the use of JSON Pointer within JSON Schema in Structuring a complex schema. Note that this should be used only when the entire string contains only JSON Pointer content, e.g. /foo/bar. JSON Pointer URI fragments, e.g. #/foo/bar/ should use "uri-reference".
		/// </summary>
		JsonPointer,

		/// <summary>
		/// "relative-json-pointer": New in draft 7 A relative JSON pointer.
		/// </summary>
		RelativeJsonPointer,

		/// <summary>
		/// "regex": New in draft 7 A regular expression, which should be valid according to the ECMA 262 dialect.
		/// </summary>
		RegEx
	}
}
