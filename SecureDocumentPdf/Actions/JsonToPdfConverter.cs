using Newtonsoft.Json.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur JSON vers PDF
    /// </summary>
    public class JsonToPdfConverter
    {
        public static byte[] ConvertToPdf(IFormFile jsonFile)
        {
            if (jsonFile == null || jsonFile.Length == 0)
                return null;

            try
            {
                using (var reader = new StreamReader(jsonFile.OpenReadStream()))
                {
                    string jsonContent = reader.ReadToEnd();

                    // Formater le JSON
                    var parsedJson = JToken.Parse(jsonContent);
                    string formattedJson = parsedJson.ToString(Newtonsoft.Json.Formatting.Indented);

                    var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(40);

                            page.Content().Text(formattedJson)
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

        public static Stream ConvertToPdfStream(IFormFile jsonFile)
        {
            byte[] pdfBytes = ConvertToPdf(jsonFile);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
}