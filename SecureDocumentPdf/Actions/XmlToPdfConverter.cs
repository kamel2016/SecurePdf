using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Xml.Linq;

namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur XML vers PDF
    /// </summary>
    public class XmlToPdfConverter
    {
        public static byte[] ConvertToPdf(IFormFile xmlFile)
        {
            if (xmlFile == null || xmlFile.Length == 0)
                return null;

            try
            {
                using (var reader = new StreamReader(xmlFile.OpenReadStream()))
                {
                    string xmlContent = reader.ReadToEnd();

                    // Formater le XML
                    var doc = XDocument.Parse(xmlContent);
                    string formattedXml = doc.ToString();

                    var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(40);

                            page.Content().Text(formattedXml)
                                .FontSize(9)
                                .FontFamily("Courier New")
                                .LineHeight(1.3f);
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

        public static Stream ConvertToPdfStream(IFormFile xmlFile)
        {
            byte[] pdfBytes = ConvertToPdf(xmlFile);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
}