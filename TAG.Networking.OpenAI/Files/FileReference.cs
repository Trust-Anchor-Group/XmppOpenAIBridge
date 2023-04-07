using System;
using System.Collections.Generic;
using Waher.Content;

namespace TAG.Networking.OpenAI.Files
{
	/// <summary>
	/// Contains a reference to a file.
	/// </summary>
	public class FileReference
	{
		/// <summary>
		/// ID of file.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Number of files.
		/// </summary>
		public int Bytes { get; set; }

		/// <summary>
		/// When file was created.
		/// </summary>
		public DateTime Created { get; set; }

		/// <summary>
		/// Name of file.
		/// </summary>
		public string FileName { get; set; }

		/// <summary>
		/// For what purpose the file is processed.
		/// </summary>
		public string Purpose { get; set; }

		/// <summary>
		/// Tries to parse a file reference object from JSON.
		/// </summary>
		/// <param name="Item">Untyped object.</param>
		/// <param name="Result">Parsed object.</param>
		/// <returns>If successful.</returns>
		public static bool TryParse(object Item, out FileReference Result)
		{
			Result = null;

			if (!(Item is Dictionary<string, object> Obj) ||
				!Obj.TryGetValue("object", out Item) ||
				!(Item is string Object) ||
				Object != "file")
			{
				return false;
			}

			Result = new FileReference();

			if (Obj.TryGetValue("id", out Item) && Item is string Id)
				Result.Id = Id;

			if (Obj.TryGetValue("bytes", out Item) && Item is int Bytes)
				Result.Bytes = Bytes;

			if (Obj.TryGetValue("created_at", out Item) && Item is int CreatedAt)
				Result.Created = JSON.UnixEpoch.AddSeconds(CreatedAt);

			if (Obj.TryGetValue("filename", out Item) && Item is string FileName)
				Result.FileName = FileName;

			if (Obj.TryGetValue("purpose", out Item) && Item is string Purpose)
				Result.Purpose = Purpose;

			return true;
		}
	}
}
