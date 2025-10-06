using System.Runtime.InteropServices;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;
using Office = Microsoft.Office.Core;

namespace VerifDocumentSecure.Actions
{
    public class PowerPointToPdfConverter
    {
        public static bool ConvertPowerPointToPdf(IFormFile pptFile, string outputPdfPath)
        {
            PowerPoint.Application pptApp = null;
            PowerPoint.Presentation pptPresentation = null;

            // Chemin temporaire pour stocker le fichier PowerPoint
            string tempPptFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(pptFile.FileName));

            try
            {
                // Enregistre le fichier temporairement
                using (var stream = new FileStream(tempPptFilePath, FileMode.Create))
                {
                    pptFile.CopyTo(stream);
                }

                pptApp = new PowerPoint.Application { Visible = Office.MsoTriState.msoFalse };

                pptPresentation = pptApp.Presentations.Open(
                    FileName: tempPptFilePath,
                    WithWindow: Office.MsoTriState.msoFalse,
                    Untitled: Office.MsoTriState.msoFalse,
                    ReadOnly: Office.MsoTriState.msoTrue
                );

                pptPresentation.ExportAsFixedFormat(
                    Path: outputPdfPath,
                    FixedFormatType: PowerPoint.PpFixedFormatType.ppFixedFormatTypePDF,
                    Intent: PowerPoint.PpFixedFormatIntent.ppFixedFormatIntentPrint,
                    FrameSlides: Office.MsoTriState.msoFalse,
                    DocStructureTags: true,
                    BitmapMissingFonts: true,
                    IncludeDocProperties: true
                );

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la conversion PowerPoint en PDF : {ex.Message}");
                return false;
            }
            finally
            {
                // Libère les objets COM
                if (pptPresentation != null)
                {
                    pptPresentation.Close();
                    Marshal.ReleaseComObject(pptPresentation);
                }
                if (pptApp != null)
                {
                    pptApp.Quit();
                    Marshal.ReleaseComObject(pptApp);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Supprime le fichier temporaire
                if (File.Exists(tempPptFilePath))
                {
                    File.Delete(tempPptFilePath);
                }
            }
        }

        /// <summary>
        /// Convertit une présentation PowerPoint en PDF en utilisant l'automatisation de Microsoft PowerPoint.
        /// Nécessite PowerPoint installé.
        /// </summary>
        private static bool ConvertPowerPointToPdf(string sourceFilePath, string outputPdfPath)
        {
            PowerPoint.Application pptApp = null;
            PowerPoint.Presentation pptPresentation = null;
            try
            {
                pptApp = new PowerPoint.Application { Visible = Office.MsoTriState.msoFalse };

                pptPresentation = pptApp.Presentations.Open(
                    FileName: sourceFilePath,
                    WithWindow: Office.MsoTriState.msoFalse, // Ne pas ouvrir avec une fenêtre visible
                    Untitled: Office.MsoTriState.msoFalse,
                    ReadOnly: Office.MsoTriState.msoTrue // Ouvrir en lecture seule
                );

                pptPresentation.ExportAsFixedFormat(
                    Path: outputPdfPath,
                    FixedFormatType: PowerPoint.PpFixedFormatType.ppFixedFormatTypePDF,
                    Intent: PowerPoint.PpFixedFormatIntent.ppFixedFormatIntentPrint, // Correction ici
                    FrameSlides: Office.MsoTriState.msoFalse,
                    //HandoutLayout: PowerPoint.PpPrintHandoutOrder.ppPrintHandoutVerticalFirst,
                    DocStructureTags: true,
                    BitmapMissingFonts: true,
                    IncludeDocProperties: true
                // Supprimez cette ligne car le paramètre n'est pas attendu ici
                // PptConverterActionAfterPublish: PowerPoint.PpConverterActionAfterPublish.ppConverterActioinNone
                );

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la conversion PowerPoint en PDF : {ex.Message}");
                return false;
            }
            finally
            {
                // Nettoyage des objets COM
                if (pptPresentation != null)
                {
                    pptPresentation.Close();
                    Marshal.ReleaseComObject(pptPresentation);
                }
                if (pptApp != null)
                {
                    pptApp.Quit();
                    Marshal.ReleaseComObject(pptApp);
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}