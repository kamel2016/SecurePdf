//// Alias pour les namespaces COM d'Office et PowerPoint
////using PdfSharp.Drawing;
//using PdfSharpCore.Pdf;
//using PdfSharpCore.Drawing;
//using SkiaSharp;

using System;
using System.IO;
using System.Runtime.InteropServices; // Pour Marshal.ReleaseComObject et le nettoyage COM
using System.Reflection; // Pour Type.Missing

// Alias pour les namespaces COM d'Office et PowerPoint
using PowerPoint = Microsoft.Office.Interop.PowerPoint;
using Office = Microsoft.Office.Core;

namespace VerifDocumentSecure.Actions
{
    /// <summary>
    /// Fournit des fonctionnalités pour convertir des fichiers PowerPoint en PDF
    /// en utilisant l'automatisation de Microsoft PowerPoint (COM Interop).
    ///
    /// IMPORTANT : Cette approche nécessite que Microsoft PowerPoint soit installé
    /// sur la machine où ce code est exécuté. Elle n'est pas recommandée
    /// pour les applications serveur en production en raison de problèmes
    /// de stabilité, de sécurité et de performance.
    /// </summary>
    public class PptToPdfConverter
    {
        /// <summary>
        /// Convertit un fichier PowerPoint (.ppt ou .pptx) en PDF.
        /// </summary>
        /// <param name="sourceFilePath">Le chemin complet du fichier PowerPoint source.</param>
        /// <param name="outputPdfPath">Le chemin complet où le fichier PDF de sortie sera enregistré.</param>
        /// <returns>True si la conversion a réussi, False sinon.</returns>
        public static bool ConvertToPdf(string sourceFilePath, string outputPdfPath)
        {
            // Vérifie si le fichier source existe
            if (!File.Exists(sourceFilePath))
            {
                Console.WriteLine($"Erreur : Le fichier source '{sourceFilePath}' n'existe pas.");
                return false;
            }

            // Vérifie l'extension du fichier source
            var fileExtension = Path.GetExtension(sourceFilePath)?.ToLowerInvariant();
            if (fileExtension != ".ppt" && fileExtension != ".pptx")
            {
                Console.WriteLine($"Erreur : Le fichier source '{sourceFilePath}' n'est pas un fichier PowerPoint valide (.ppt ou .pptx).");
                return false;
            }

            // Assure que le répertoire de sortie existe
            string outputDirectory = Path.GetDirectoryName(outputPdfPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                try
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la création du répertoire de sortie '{outputDirectory}' : {ex.Message}");
                    return false;
                }
            }

            Console.WriteLine($"Tentative de conversion de '{sourceFilePath}' en PDF via PowerPoint COM Interop...");

            Microsoft.Office.Interop.PowerPoint.Application pptApp = null;
            PowerPoint.Presentation pptPresentation = null;
            bool conversionSuccessful = false;

            try
            {
                // Crée une nouvelle instance de l'application PowerPoint
                pptApp = new PowerPoint.Application
                {
                    // Rend l'application PowerPoint invisible
                    // Important pour l'automatisation silencieuse
                    Visible = Office.MsoTriState.msoTrue
                };

                // Ouvre la présentation en lecture seule et sans fenêtre visible
                pptPresentation = pptApp.Presentations.Open(
                    FileName: sourceFilePath,
                    WithWindow: Office.MsoTriState.msoFalse,
                    Untitled: Office.MsoTriState.msoFalse,
                    ReadOnly: Office.MsoTriState.msoTrue
                );


                // Version minimaliste qui fonctionne dans la plupart des cas
                pptPresentation.ExportAsFixedFormat(
                    outputPdfPath,
                    PowerPoint.PpFixedFormatType.ppFixedFormatTypePDF
                );

                //// Exporte la présentation au format PDF
                //pptPresentation.ExportAsFixedFormat(
                //     Path: outputPdfPath,
                //     FixedFormatType: PowerPoint.PpFixedFormatType.ppFixedFormatTypePDF,
                //     Intent: PowerPoint.PpFixedFormatIntent.ppFixedFormatIntentPrint,
                //     FrameSlides: Office.MsoTriState.msoFalse,
                //     HandoutOrder: PowerPoint.PpPrintHandoutOrder.ppPrintHandoutVerticalFirst,
                //     OutputType: PowerPoint.PpPrintOutputType.ppPrintOutputSlides,
                //     PrintHiddenSlides: Office.MsoTriState.msoFalse,
                //     PrintRange: null,
                //     RangeType: PowerPoint.PpPrintRangeType.ppPrintAll,
                //     SlideShowName: null,
                //     IncludeDocProperties: false,
                //     KeepIRMSettings: true,
                //     DocStructureTags: true,
                //     BitmapMissingFonts: true,
                //     UseISO19005_1: false,
                //     ExternalExporter: null
                // );

                conversionSuccessful = true;
                Console.WriteLine($"Conversion réussie : '{sourceFilePath}' a été converti en '{outputPdfPath}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Une erreur est survenue lors de la conversion : {ex.Message}");
                Console.WriteLine($"Détails de l'erreur : {ex.ToString()}"); // Pour un diagnostic complet
                conversionSuccessful = false;
            }
            finally
            {
                // --- Nettoyage des objets COM ---
                // C'est CRUCIAL pour éviter les fuites de mémoire et les processus PowerPoint fantômes.

                // Ferme la présentation si elle a été ouverte
                if (pptPresentation != null)
                {
                    // Ne pas enregistrer les modifications
                    pptPresentation.Close();
                    // Libère l'objet COM de la mémoire
                    Marshal.ReleaseComObject(pptPresentation);
                    pptPresentation = null; // Définir à null après la libération
                }

                // Quitte l'application PowerPoint si elle a été démarrée
                if (pptApp != null)
                {
                    pptApp.Quit();
                    // Libère l'objet COM de la mémoire
                    Marshal.ReleaseComObject(pptApp);
                    pptApp = null; // Définir à null après la libération
                }

                // Force le garbage collection pour s'assurer que tous les objets COM sont nettoyés
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return conversionSuccessful;
        }

        //public static bool ConvertToPdf(string sourceFilePath, string outputPdfPath)
        //{
        //    // Vérifie si le fichier source existe
        //    if (!File.Exists(sourceFilePath))
        //    {
        //        Console.WriteLine($"Erreur : Le fichier source '{sourceFilePath}' n'existe pas.");
        //        return false;
        //    }

        //    // Vérifie l'extension du fichier
        //    var fileExtension = Path.GetExtension(sourceFilePath)?.ToLowerInvariant();
        //    if (fileExtension != ".pptx")
        //    {
        //        Console.WriteLine($"Erreur : Le fichier '{sourceFilePath}' n'est pas un fichier PowerPoint (.pptx).");
        //        return false;
        //    }

        //    // Crée le répertoire de sortie si nécessaire
        //    var outputDirectory = Path.GetDirectoryName(outputPdfPath);
        //    if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        //    {
        //        try
        //        {
        //            Directory.CreateDirectory(outputDirectory);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Erreur lors de la création du répertoire : {ex.Message}");
        //            return false;
        //        }
        //    }

        //    Console.WriteLine($"Conversion de '{sourceFilePath}' en PDF via rendu image...");

        //    try
        //    {
        //        // Simule la lecture des slides (ici on génère des images factices)
        //        // Dans une version avancée, tu pourrais parser le contenu XML avec OpenXML SDK
        //        int slideCount = 5; // Exemple : 5 slides fictifs

        //        var pdf = new PdfDocument();

        //        for (int i = 1; i <= slideCount; i++)
        //        {
        //            // Crée une image blanche avec texte "Slide X"
        //            using var bitmap = new SKBitmap(1280, 720);
        //            using var canvas = new SKCanvas(bitmap);
        //            canvas.Clear(SKColors.White);

        //            var paint = new SKPaint
        //            {
        //                Color = SKColors.DarkSlateGray,
        //                TextSize = 48,
        //                IsAntialias = true
        //            };

        //            canvas.DrawText($"Diapositive {i}", 100, 360, paint);
        //            canvas.Flush();

        //            using var image = SKImage.FromBitmap(bitmap);
        //            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        //            using var ms = new MemoryStream();
        //            data.SaveTo(ms);
        //            ms.Position = 0;

        //            var page = pdf.AddPage();
        //            page.Width = 1280;
        //            page.Height = 720;

        //            using var gfx = XGraphics.FromPdfPage(page);
        //            using var img = XImage.FromStream(() => ms);
        //            gfx.DrawImage(img, 0, 0, page.Width, page.Height);
        //        }

        //        pdf.Save(outputPdfPath);
        //        Console.WriteLine($"✅ Conversion réussie : PDF enregistré à '{outputPdfPath}'");
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Erreur lors de la conversion : {ex.Message}");
        //        return false;
        //    }
        //}
    }
}