using CsvHelper;
using CsvHelper.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Formats.Asn1;
using System.Globalization;

namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur CSV vers PDF
    /// </summary>
    public class CsvToPdfConverter
    {
        public static byte[] ConvertToPdf(IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
                return null;

            try
            {
                using (var reader = new StreamReader(csvFile.OpenReadStream()))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = ",",
                    BadDataFound = null
                }))
                {
                    var records = csv.GetRecords<dynamic>().ToList();

                    if (records.Count == 0)
                        return null;

                    var headers = ((IDictionary<string, object>)records[0]).Keys.ToList();

                    var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4.Landscape());
                            page.Margin(20);

                            page.Content().Column(column =>
                            {
                                column.Item().Text("CSV Data")
                                    .FontSize(16)
                                    .Bold();

                                column.Item().PaddingTop(10);

                                column.Item().Table(table =>
                                {
                                    // Définir les colonnes
                                    table.ColumnsDefinition(columns =>
                                    {
                                        foreach (var header in headers)
                                        {
                                            columns.RelativeColumn();
                                        }
                                    });

                                    // En-têtes
                                    foreach (var header in headers)
                                    {
                                        table.Cell().Border(1).Padding(5)
                                            .Background("#2196F3")
                                            .Text(header)
                                            .FontColor("#FFFFFF")
                                            .FontSize(10)
                                            .Bold();
                                    }

                                    // Données
                                    foreach (var record in records)
                                    {
                                        var dict = (IDictionary<string, object>)record;
                                        foreach (var header in headers)
                                        {
                                            table.Cell().Border(1).Padding(5)
                                                .Text(dict[header]?.ToString() ?? "")
                                                .FontSize(9);
                                        }
                                    }
                                });
                            });
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

        public static Stream ConvertToPdfStream(IFormFile csvFile)
        {
            byte[] pdfBytes = ConvertToPdf(csvFile);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
}