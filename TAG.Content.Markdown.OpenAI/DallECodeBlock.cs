using SkiaSharp;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TAG.Networking.OpenAI;
using TAG.Things.OpenAI;
using Waher.Content;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Contracts;
using Waher.Content.Markdown.Latex;
using Waher.Content.Markdown.Model;
using Waher.Content.Markdown.Model.CodeContent;
using Waher.Content.Markdown.Rendering;
using Waher.Content.Markdown.Wpf;
using Waher.Content.Markdown.Xamarin;
using Waher.Content.Xml;
using Waher.Runtime.Inventory;
using Waher.Runtime.IO;
using Waher.Runtime.Temporary;
using Waher.Script;
using Waher.Script.Graphs;
using Waher.Security;
using Waher.Things;

namespace TAG.Content.Markdown.OpenAI
{
	/// <summary>
	/// Generates images from textual descriptions using OpenAI image generation
	/// (DALL-E) API.
	/// </summary>
	public class DallECodeBlock : IImageCodeContent, ICodeContentHtmlRenderer, ICodeContentTextRenderer, ICodeContentMarkdownRenderer,
		ICodeContentContractsRenderer, ICodeContentLatexRenderer, ICodeContentWpfXamlRenderer, ICodeContentXamarinFormsXamlRenderer
	{
		/// <summary>
		/// Generates images from textual descriptions.
		/// </summary>
		public DallECodeBlock()
		{
		}

		/// <summary>
		/// Checks how well the handler supports multimedia content of a given type.
		/// </summary>
		/// <param name="Language">Language.</param>
		/// <returns>How well the handler supports the content.</returns>
		public Grade Supports(string Language)
		{
			return TryParse(Language, out _, out _, out _) ? Grade.Excellent : Grade.NotAtAll;
		}

		private static bool TryParse(string Language, out string NodeId, out string Title, out ImageSize? Size)
		{
			NodeId = null;
			Title = null;
			Size = null;

			if (!OpenAIModule.Initialized)
				return false;

			int i = Language.IndexOf(':');
			if (i > 0)
			{
				Title = Language[(i + 1)..].TrimStart();
				Language = Language[..i].TrimEnd();
			}
			else
				Title = string.Empty;

			i = Language.IndexOf(',');
			if (i < 0)
				return false;

			NodeId = Language[(i + 1)..].TrimStart();
			Language = Language[..i].TrimEnd().ToLower();

			switch (Language)
			{
				case "dalle256":
					Size = ImageSize.ImageSize256x256;
					return true;

				case "dalle512":
					Size = ImageSize.ImageSize512x512;
					return true;

				case "dalle1024":
					Size = ImageSize.ImageSize1024x1024;
					return true;

				case "dalle":
					return true;

				default:
					return false;
			}
		}

		/// <summary>
		/// If script is evaluated for this type of code block.
		/// </summary>
		public bool EvaluatesScript => false;

		/// <summary>
		/// Is called on the object when an instance of the element has been created in a document.
		/// </summary>
		/// <param name="Document">Document containing the instance.</param>
		public void Register(MarkdownDocument Document)
		{
			// Do nothing.
		}

		/// <summary>
		/// Generates HTML for the markdown element.
		/// </summary>
		/// <param name="Renderer">HTML will be output here.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> RenderHtml(HtmlRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			StringBuilder Output = Renderer.Output;
			GraphInfo Info = await GetFileName(Language, Rows, OpenAIModule.AsyncHtmlOutput is null);
			if (!(Info is null))
			{
				this.GenerateHTML(Output, Info);
				return true;
			}

			string Title;
			int i = Language.IndexOf(':');
			if (i > 0)
				Title = Language[(i + 1)..].Trim();
			else
				Title = null;

			string Id = await OpenAIModule.AsyncHtmlOutput.GenerateStub(MarkdownOutputType.Html, Output, Title, Document);

			Document.QueueAsyncTask(async (_) =>
			{
				Output = new StringBuilder();

				try
				{
					Info = await GetFileName(Language, Rows, true);
					if (!(Info is null))
						this.GenerateHTML(Output, Info);
				}
				catch (Exception ex)
				{
					await Renderer.RenderObject(ex, true, new Variables());
				}

				await OpenAIModule.AsyncHtmlOutput.ReportResult(MarkdownOutputType.Html, Id, Output.ToString());
			}, null);

			return true;
		}

		private void GenerateHTML(StringBuilder Output, GraphInfo Info)
		{
			string FileName = Info.FileName[OpenAIModule.OpenAIContentFolder.Length..].Replace(Path.DirectorySeparatorChar, '/');
			if (!FileName.StartsWith("/"))
				FileName = "/" + FileName;

			Output.Append("<figure>");
			Output.Append("<img src=\"/OpenAI");
			Output.Append(XML.HtmlAttributeEncode(FileName));

			if (!string.IsNullOrEmpty(Info.Title))
			{
				Output.Append("\" alt=\"");
				Output.Append(XML.HtmlAttributeEncode(Info.Title));

				Output.Append("\" title=\"");
				Output.Append(XML.HtmlAttributeEncode(Info.Title));
			}

			Output.Append("\" class=\"aloneUnsized\"/>");

			if (!string.IsNullOrEmpty(Info.Title))
			{
				Output.Append("<figcaption>");
				Output.Append(XML.HtmlValueEncode(Info.Title));
				Output.Append("</figcaption>");
			}

			Output.AppendLine("</figure>");
		}

		private class GraphInfo
		{
			public string FileName;
			public string Title;
			public ImageSize ImageSize;
		}

		/// <summary>
		/// Generates an image, saves it, and returns the file name of the image file.
		/// </summary>
		/// <param name="Language">Language</param>
		/// <param name="Rows">Code Block rows</param>
		/// <param name="GenerateIfNotExists">If a file should be generated, if one is not found.</param>
		/// <returns>File name</returns>
		private static async Task<GraphInfo> GetFileName(string Language, string[] Rows, bool GenerateIfNotExists)
		{
			if (!TryParse(Language, out string NodeId, out string Title, out ImageSize? Size))
				return null;

			INode Node = await OpenAIModule.DefaultSource.GetNodeAsync(new ThingReference(NodeId, OpenAIModule.DefaultSource.SourceID));
			if (!(Node is DallEXmppBridge DalleBridge))
				return null;

			string Description = MarkdownDocument.AppendRows(Rows);
			GraphInfo Result = new GraphInfo()
			{
				Title = Title,
				ImageSize = Size ?? DalleBridge.ImageSize
			};

			string Hash = Hashes.ComputeSHA256HashString(Encoding.UTF8.GetBytes(DalleBridge.ApiKey + Description + Language + Result.ImageSize.ToString()));
			Result.FileName = Path.Combine(OpenAIModule.OpenAIContentFolder, Hash) + ".png";

			if (File.Exists(Result.FileName))
				return Result;

			if (!GenerateIfNotExists)
				return null;

			try
			{
				Uri ImageUri = await DalleBridge.GetImageUri(Description, Result.ImageSize);
				ContentStreamResponse Content = await InternetContent.GetTempStreamAsync(ImageUri);
				if (Content.HasError)
					return null;

				using TemporaryStream f = Content.Encoded;
				int c = (int)Math.Min(int.MaxValue, f.Length);
				byte[] Bin = new byte[c];

				f.Position = 0;
				await f.ReadAsync(Bin, 0, c);

				await Files.WriteAllBytesAsync(Result.FileName, Bin);
			}
			catch (Exception)
			{
				return null;
			}

			return Result;
		}

		/// <summary>
		/// Generates (transportable) Markdown for the markdown element.
		/// </summary>
		/// <param name="Renderer">HTML will be output here.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> RenderMarkdown(MarkdownRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			GraphInfo Info = await GetFileName(Language, Rows, true);
			if (Info?.FileName is null)
				return false;

			return await ImageContent.GenerateMarkdownFromFile(Renderer.Output, Info.FileName, Info.Title);
		}

		/// <summary>
		/// Generates Plain Text for the markdown element.
		/// </summary>
		/// <param name="Renderer">HTML will be output here.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public Task<bool> RenderText(TextRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			StringBuilder Output = Renderer.Output;

			foreach (string Row in Rows)
				Output.AppendLine(Row);

			return Task.FromResult(true);
		}

		/// <summary>
		/// Generates WPF XAML for the markdown element.
		/// </summary>
		/// <param name="Renderer">XAML will be output here.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> RenderWpfXaml(WpfXamlRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			GraphInfo Info = await GetFileName(Language, Rows, true);
			if (Info?.FileName is null)
				return false;

			XmlWriter Output = Renderer.XmlOutput;

			Output.WriteStartElement("Image");
			Output.WriteAttributeString("Source", Info.FileName);
			Output.WriteAttributeString("Stretch", "None");

			if (!string.IsNullOrEmpty(Info.Title))
				Output.WriteAttributeString("ToolTip", Info.Title);

			Output.WriteEndElement();

			return true;
		}

		/// <summary>
		/// Generates Xamarin.Forms XAML for the markdown element.
		/// </summary>
		/// <param name="Renderer">XAML will be output here.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> RenderXamarinFormsXaml(XamarinFormsXamlRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			GraphInfo Info = await GetFileName(Language, Rows, true);
			if (Info?.FileName is null)
				return false;

			XmlWriter Output = Renderer.XmlOutput;

			Output.WriteStartElement("Image");
			Output.WriteAttributeString("Source", Info.FileName);
			Output.WriteEndElement();

			return true;
		}

		/// <summary>
		/// Generates LaTeX text for the markdown element.
		/// </summary>
		/// <param name="Renderer">LaTeX will be output here.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> RenderLatex(LatexRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			GraphInfo Info = await GetFileName(Language, Rows, true);
			if (Info?.FileName is null)
				return false;

			StringBuilder Output = Renderer.Output;

			Output.AppendLine("\\begin{figure}[h]");
			Output.AppendLine("\\centering");

			Output.Append("\\fbox{\\includegraphics{");
			Output.Append(Info.FileName.Replace('\\', '/'));
			Output.AppendLine("}}");

			if (!string.IsNullOrEmpty(Info.Title))
			{
				Output.Append("\\caption{");
				Output.Append(LatexRenderer.EscapeLaTeX(Info.Title));
				Output.AppendLine("}");
			}

			Output.AppendLine("\\end{figure}");
			Output.AppendLine();

			return true;
		}

		/// <summary>
		/// Generates an image of the contents.
		/// </summary>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>Image, if successful, null otherwise.</returns>
		public async Task<PixelInformation> GenerateImage(string[] Rows, string Language, MarkdownDocument Document)
		{
			GraphInfo Info = await GetFileName(Language, Rows, true);
			if (Info?.FileName is null)
				return null;

			byte[] Data = await Files.ReadAllBytesAsync(Info.FileName);

			using SKBitmap Bitmap = SKBitmap.Decode(Data);
			
			return new PixelInformationPng(Data, Bitmap.Width, Bitmap.Height);
		}

		/// <summary>
		/// Generates Smart Contract XML for the markdown element.
		/// </summary>
		/// <param name="Renderer">Smart Contract XML will be output here.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> RenderContractXml(ContractsRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			try
			{
				GraphInfo Info = await GetFileName(Language, Rows, true);
				if (Info?.FileName is null)
					return false;

				if (!InternetContent.TryGetContentType(Path.GetExtension(Info.FileName), out string ContentType))
					return false;

				byte[] Bin = File.ReadAllBytes(Info.FileName);
				string Base64 = Convert.ToBase64String(Bin);
				int Width, Height;

				switch (Info.ImageSize)
				{
					case ImageSize.ImageSize256x256:
						Width = Height = 256;
						break;

					case ImageSize.ImageSize512x512:
						Width = Height = 512;
						break;

					case ImageSize.ImageSize1024x1024:
						Width = Height = 1024;
						break;

					default:
						object Obj = await InternetContent.DecodeAsync(ContentType, Bin, null);
						if (!(Obj is SKImage Image))
						{
							if (Obj is IDisposable Disposable)
								Disposable.Dispose();

							return false;
						}

						Width = Image.Width;
						Height = Image.Height;
						Image.Dispose();
						break;
				}

				XmlWriter Output = Renderer.XmlOutput;

				Output.WriteStartElement("imageStandalone");
				Output.WriteAttributeString("contentType", ContentType);
				Output.WriteAttributeString("width", Width.ToString());
				Output.WriteAttributeString("height", Height.ToString());

				Output.WriteStartElement("binary");
				Output.WriteValue(Base64);
				Output.WriteEndElement();

				Output.WriteStartElement("caption");
				if (string.IsNullOrEmpty(Info.Title))
					Output.WriteElementString("text", "Image");
				else
					Output.WriteElementString("text", Info.Title);

				Output.WriteEndElement();
				Output.WriteEndElement();

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

	}
}
