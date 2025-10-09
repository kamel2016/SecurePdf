using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur Excel vers PDF (avec ClosedXML + QuestPDF)
    /// </summary>
    public class ExcelToPdfConverter
    {
        public static byte[] ConvertToPdf(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
                return null;

            try
            {
                using (var stream = new MemoryStream())
                {
                    excelFile.CopyTo(stream);
                    stream.Position = 0;

                    using (var workbook = new XLWorkbook(stream))
                    {
                        var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
                        {
                            foreach (var worksheet in workbook.Worksheets)
                            {
                                container.Page(page =>
                                {
                                    page.Size(PageSizes.A4.Landscape());
                                    page.Margin(20);

                                    page.Content().Column(column =>
                                    {
                                        column.Item().Text(worksheet.Name)
                                            .FontSize(16)
                                            .Bold();

                                        column.Item().PaddingTop(10);

                                        // Créer un tableau avec les données
                                        var usedRange = worksheet.RangeUsed();
                                        if (usedRange != null)
                                        {
                                            column.Item().Table(table =>
                                            {
                                                var colCount = usedRange.ColumnCount();

                                                // ✅ Définir les colonnes une seule fois
                                                table.ColumnsDefinition(columns =>
                                                {
                                                    for (int i = 0; i < colCount; i++)
                                                    {
                                                        columns.RelativeColumn();
                                                    }
                                                });

                                                // Ajouter les lignes
                                                foreach (var row in usedRange.Rows())
                                                {
                                                    foreach (var cell in row.Cells())
                                                    {
                                                        table.Cell().Border(1).Padding(5)
                                                            .Text(cell.GetString())
                                                            .FontSize(9);
                                                    }
                                                }
                                            });

                                        }
                                    });
                                });
                            }
                        }).GeneratePdf();

                        return pdfBytes;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static Stream ConvertToPdfStream(IFormFile excelFile)
        {
            byte[] pdfBytes = ConvertToPdf(excelFile);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
}