using QuestPDF.Fluent;
using QuestPDF.Helpers;
using Svg;

namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur SVG vers PDF
    /// </summary>
    public class SvgToPdfConverter
    {
        public static byte[] ConvertToPdf(IFormFile svgFile)
        {
            if (svgFile == null || svgFile.Length == 0)
                return null;

            try
            {
                using (var stream = new MemoryStream())
                {
                    svgFile.CopyTo(stream);
                    stream.Position = 0;

                    var svgDocument = SvgDocument.Open<SvgDocument>(stream);
                    using (var bitmap = svgDocument.Draw())
                    {
                        using (var ms = new MemoryStream())
                        {
                            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            ms.Position = 0;

                            var imageBytes = ms.ToArray();

                            var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
                            {
                                container.Page(page =>
                                {
                                    page.Size(PageSizes.A4);
                                    page.Margin(20);

                                    page.Content().Image(imageBytes).FitArea();
                                });
                            }).GeneratePdf();

                            return pdfBytes;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static Stream ConvertToPdfStream(IFormFile svgFile)
        {
            byte[] pdfBytes = ConvertToPdf(svgFile);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
}