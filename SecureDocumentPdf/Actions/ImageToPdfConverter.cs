using PdfSharp.Drawing;

namespace VerifDocumentSecure.Actions
{
    public class ImageToPdfConverter
    {
        public static bool ConvertImageToPdf(IFormFile imageFile, string outputPdfPath)
        {
            try
            {
                // Crée un nouveau document PDF
                PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument();
                document.Info.Title = Path.GetFileNameWithoutExtension(imageFile.FileName);

                // Ajoute une page au document
                PdfSharp.Pdf.PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);

                // Lit le fichier image depuis le stream
                using (var imageStream = imageFile.OpenReadStream())
                {
                    XImage image = XImage.FromStream(imageStream);

                    // Calcule les dimensions pour que l'image s'adapte à la page tout en conservant le ratio
                    double scaleFactor = Math.Min(page.Width.Point / image.PixelWidth, page.Height.Point / image.PixelHeight);
                    double scaledWidth = image.PixelWidth * scaleFactor;
                    double scaledHeight = image.PixelHeight * scaleFactor;

                    // Centre l'image sur la page
                    double x = (page.Width.Point - scaledWidth) / 2;
                    double y = (page.Height.Point - scaledHeight) / 2;

                    // Dessine l'image sur la page
                    gfx.DrawImage(image, x, y, scaledWidth, scaledHeight);
                }

                // Enregistre le document PDF
                document.Save(outputPdfPath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la conversion de l'image en PDF : {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Convertit une image en PDF en utilisant PdfSharp.
        /// Une image par page PDF.
        /// </summary>
        public static bool ConvertImageToPdf(string sourceFilePath, string outputPdfPath)
        {
            try
            {
                // Crée un nouveau document PDF
                PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument();
                document.Info.Title = Path.GetFileNameWithoutExtension(sourceFilePath);

                // Ajoute une page au document
                PdfSharp.Pdf.PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);

                // Charge l'image
                XImage image = XImage.FromFile(sourceFilePath);

                // Calcule les dimensions pour que l'image s'adapte à la page tout en conservant le ratio
                double scaleFactor = Math.Min(page.Width.Point / image.PixelWidth, page.Height.Point / image.PixelHeight);
                double scaledWidth = image.PixelWidth * scaleFactor;
                double scaledHeight = image.PixelHeight * scaleFactor;

                // Centre l'image sur la page
                double x = (page.Width.Point - scaledWidth) / 2;
                double y = (page.Height.Point - scaledHeight) / 2;

                // Dessine l'image sur la page
                gfx.DrawImage(image, x, y, scaledWidth, scaledHeight);

                // Enregistre le document PDF
                document.Save(outputPdfPath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la conversion de l'image en PDF : {ex.Message}");
                return false;
            }
        }
    }
}