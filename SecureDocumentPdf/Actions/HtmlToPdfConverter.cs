using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur HTML vers PDF
    /// </summary>
    public class HtmlToPdfConverter
    {
        public static byte[] ConvertToPdf(IFormFile htmlFile)
        {
            if (htmlFile == null || htmlFile.Length == 0)
                return null;

            try
            {
                using (var reader = new StreamReader(htmlFile.OpenReadStream()))
                {
                    string html = reader.ReadToEnd();
                    return ConvertHtmlStringToPdf(html);
                }
            }
            catch
            {
                return null;
            }
        }

        public static byte[] ConvertHtmlStringToPdf(string html)
        {
            try
            {
                // Nettoyer le HTML des balises et extraire le texte
                var plainText = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ");
                plainText = System.Net.WebUtility.HtmlDecode(plainText);

                var pdfBytes = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);

                        page.Content().Text(plainText)
                            .FontSize(11)
                            .LineHeight(1.5f);
                    });
                }).GeneratePdf();

                return pdfBytes;
            }
            catch
            {
                return null;
            }
        }

        public static Stream ConvertToPdfStream(IFormFile htmlFile)
        {
            byte[] pdfBytes = ConvertToPdf(htmlFile);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
}