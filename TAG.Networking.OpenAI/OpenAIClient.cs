using System;

namespace TAG.Networking.OpenAI
{
	public class OpenAIClient
	{
		private readonly string apiKey;

		public OpenAIClient(string ApiKey)
		{
			this.apiKey = ApiKey;
		}
	}
}
