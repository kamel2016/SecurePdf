using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur Image vers PDF (avec ImageSharp + QuestPDF)
    /// </summary>
    public class ImageToPdfConverter
    {
        public static byte[] ConvertToPdf(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            try
            {
                using (var ms = new MemoryStream())
                {
                    imageFile.CopyTo(ms);
                    ms.Position = 0;

                    using (var image = SixLabors.ImageSharp.Image.Load(ms))
                    {
                        var imageBytes = ms.ToArray();

                        var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
                        {
                            container.Page(page =>
                            {
                                // Adapter la taille de la page à l'image
                                float aspectRatio = (float)image.Width / image.Height;

                                if (aspectRatio > 1) // Paysage
                                    page.Size(PageSizes.A4.Landscape());
                                else
                                    page.Size(PageSizes.A4);

                                page.Margin(0);

                                page.Content().Image(imageBytes).FitArea();
                            });
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

        public static Stream ConvertToPdfStream(IFormFile imageFile)
        {
            byte[] pdfBytes = ConvertToPdf(imageFile);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
}