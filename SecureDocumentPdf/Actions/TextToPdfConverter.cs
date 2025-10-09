using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur Texte (.txt) vers PDF (avec QuestPDF)
    /// </summary>
    public class TextToPdfConverter
    {
        public static byte[] ConvertToPdf(IFormFile textFile)
        {
            if (textFile == null || textFile.Length == 0)
                return null;

            try
            {
                using (var reader = new StreamReader(textFile.OpenReadStream()))
                {
                    string textContent = reader.ReadToEnd();

                    var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(40);

                            page.Content().Text(textContent)
                                .FontSize(11)
                                .FontFamily("Courier New");
                        });
                    }).GeneratePdf();

                    return pdfBytes;
                }
            }
            catch
            {
                return null;
            }
        }

        public static Stream ConvertToPdfStream(IFormFile textFile)
        {
            byte[] pdfBytes = ConvertToPdf(textFile);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
}