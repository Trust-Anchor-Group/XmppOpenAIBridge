using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TAG.Things.OpenAI;
using Waher.Content;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Content.Markdown.Model.SpanElements;
using Waher.Content.Xml;
using Waher.Runtime.Inventory;
using Waher.Script;
using Waher.Security;
using Waher.Things;

namespace TAG.Content.Markdown.OpenAI
{
	/// <summary>
	/// Generates text from textual descriptions using OpenAI ChatGPT generation API.
	/// </summary>
	public class ChatGptCodeBlock : ICodeContent
	{
		/// <summary>
		/// Generates text from textual descriptions using OpenAI ChatGPT generation API.
		/// </summary>
		public ChatGptCodeBlock()
		{
		}

		/// <summary>
		/// Checks how well the handler supports multimedia content of a given type.
		/// </summary>
		/// <param name="Language">Language.</param>
		/// <returns>How well the handler supports the content.</returns>
		public Grade Supports(string Language)
		{
			return TryParse(Language, out _, out _) ? Grade.Excellent : Grade.NotAtAll;
		}

		private static bool TryParse(string Language, out string NodeId, out string Title)
		{
			NodeId = null;
			Title = null;

			if (!OpenAIModule.Initialized)
				return false;

			int i = Language.IndexOf(':');
			if (i > 0)
			{
				Title = Language.Substring(i + 1).TrimStart();
				Language = Language.Substring(0, i).TrimEnd();
			}
			else
				Title = string.Empty;

			i = Language.IndexOf(',');
			if (i < 0)
				return false;

			NodeId = Language.Substring(i + 1).TrimStart();
			Language = Language.Substring(0, i).TrimEnd().ToLower();

			if (Language != "chatgpt")
				return false;

			return true;
		}

		/// <summary>
		/// Is called on the object when an instance of the element has been created in a document.
		/// </summary>
		/// <param name="Document">Document containing the instance.</param>
		public void Register(MarkdownDocument Document)
		{
			// Do nothing.
		}

		/// <summary>
		/// If HTML is handled.
		/// </summary>
		public bool HandlesHTML => true;

		/// <summary>
		/// If Plain Text is handled.
		/// </summary>
		public bool HandlesPlainText => true;

		/// <summary>
		/// If XAML is handled.
		/// </summary>
		public bool HandlesXAML => true;

		/// <summary>
		/// If LaTeX is handled.
		/// </summary>
		public bool HandlesLaTeX => true;

		/// <summary>
		/// Generates HTML for the markdown element.
		/// </summary>
		/// <param name="Output">HTML will be output here.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> GenerateHTML(StringBuilder Output, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			string Text = await GetText(Language, Rows, false);
			if (!(Text is null))
			{
				this.GenerateHTML(Output, Text);
				return true;
			}

			string Title;
			int i = Language.IndexOf(':');
			if (i > 0)
				Title = Language.Substring(i + 1).Trim();
			else
				Title = null;

			string Id = await OpenAIModule.AsyncHtmlOutput.GenerateStub(MarkdownOutputType.Html, Output, Title);

			Document.QueueAsyncTask(async () =>
			{
				Output = new StringBuilder();

				try
				{
					Text = await GetText(Language, Rows, true);
					if (!(Text is null))
						this.GenerateHTML(Output, Text);
				}
				catch (Exception ex)
				{
					await InlineScript.GenerateHTML(ex, Output, true, new Variables());
				}

				await OpenAIModule.AsyncHtmlOutput.ReportResult(MarkdownOutputType.Html, Id, Output.ToString());
			});

			return true;
		}

		private void GenerateHTML(StringBuilder Output, string Text)
		{
			string[] Paragraphs = Text.Replace("\r\n", "\n").Replace('\r', '\n').Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

			foreach (string Paragraph in Paragraphs)
			{
				Output.Append("<p>");
				Output.Append(XML.HtmlValueEncode(Paragraph));
				Output.AppendLine("</p>");
			}
		}

		/// <summary>
		/// Generates an image, saves it, and returns the file name of the image file.
		/// </summary>
		/// <param name="Language">Language</param>
		/// <param name="Rows">Code Block rows</param>
		/// <param name="GenerateIfNotExists">If a file should be generated, if one is not found.</param>
		/// <returns>File name</returns>
		private static async Task<string> GetText(string Language, string[] Rows, bool GenerateIfNotExists)
		{
			string Description = MarkdownDocument.AppendRows(Rows);

			if (!TryParse(Language, out string NodeId, out string _))
				return Description;

			INode Node = await OpenAIModule.DefaultSource.GetNodeAsync(new ThingReference(NodeId, OpenAIModule.DefaultSource.SourceID));
			if (!(Node is ChatGPTXmppBridge ChatGptBridge))
				return Description;

			string Hash = Hashes.ComputeSHA256HashString(Encoding.UTF8.GetBytes(ChatGptBridge.ApiKey + Description + Language));
			string FileName = Path.Combine(OpenAIModule.OpenAIContentFolder, Hash) + ".txt";
			string Text;

			if (File.Exists(FileName))
				return await Resources.ReadAllTextAsync(FileName);

			if (!GenerateIfNotExists)
				return null;

			try
			{
				Text = await ChatGptBridge.GetText(Description);

				await Resources.WriteAllTextAsync(FileName, Text, Encoding.UTF8);
			}
			catch (Exception)
			{
				return Description;
			}

			return Text;
		}

		/// <summary>
		/// Generates Plain Text for the markdown element.
		/// </summary>
		/// <param name="Output">HTML will be output here.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> GeneratePlainText(StringBuilder Output, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			string Text = await GetText(Language, Rows, true);

			Output.AppendLine(Text);
			Output.AppendLine();

			return true;
		}

		/// <summary>
		/// Generates WPF XAML for the markdown element.
		/// </summary>
		/// <param name="Output">XAML will be output here.</param>
		/// <param name="TextAlignment">Alignment of text in element.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> GenerateXAML(XmlWriter Output, TextAlignment TextAlignment, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			string Text = await GetText(Language, Rows, true);

			Output.WriteStartElement("TextBlock");
			Output.WriteAttributeString("TextWrapping", "Wrap");
			Output.WriteAttributeString("Margin", Document.Settings.XamlSettings.ParagraphMargins);
			if (TextAlignment != TextAlignment.Left)
				Output.WriteAttributeString("TextAlignment", TextAlignment.ToString());

			Output.WriteValue(Text);

			Output.WriteEndElement();

			return true;
		}

		/// <summary>
		/// Generates Xamarin.Forms XAML for the markdown element.
		/// </summary>
		/// <param name="Output">XAML will be output here.</param>
		/// <param name="State">Xamarin Forms XAML Rendering State.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> GenerateXamarinForms(XmlWriter Output, XamarinRenderingState State, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			string Text = await GetText(Language, Rows, true);

			Output.WriteStartElement("ContentView");
			Output.WriteAttributeString("Padding", Document.Settings.XamlSettings.ParagraphMargins);

			switch (State.TextAlignment)
			{
				case TextAlignment.Center:
					Output.WriteAttributeString("HorizontalOptions", "Center");
					break;

				case TextAlignment.Left:
					Output.WriteAttributeString("HorizontalOptions", "Start");
					break;

				case TextAlignment.Right:
					Output.WriteAttributeString("HorizontalOptions", "End");
					break;
			}

			Output.WriteStartElement("Label");
			Output.WriteAttributeString("Text", Text);
			Output.WriteEndElement();

			Output.WriteEndElement();

			return true;
		}

		/// <summary>
		/// Generates LaTeX text for the markdown element.
		/// </summary>
		/// <param name="Output">LaTeX will be output here.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> GenerateLaTeX(StringBuilder Output, string[] Rows, string Language, int Indent,
			MarkdownDocument Document)
		{
			string Text = await GetText(Language, Rows, true);

			Output.AppendLine(Text);
			Output.AppendLine();

			return true;
		}

	}
}
