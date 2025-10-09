using Markdig;

namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur Markdown vers PDF
    /// </summary>
    public class MarkdownToPdfConverter
    {
        public static byte[] ConvertToPdf(IFormFile mdFile)
        {
            if (mdFile == null || mdFile.Length == 0)
                return null;

            try
            {
                using (var reader = new StreamReader(mdFile.OpenReadStream()))
                {
                    string markdown = reader.ReadToEnd();
                    string html = Markdown.ToHtml(markdown);

                    return HtmlToPdfConverter.ConvertHtmlStringToPdf(html);
                }
            }
            catch
            {
                return null;
            }
        }

        public static Stream ConvertToPdfStream(IFormFile mdFile)
        {
            byte[] pdfBytes = ConvertToPdf(mdFile);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
}