using System;
using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using Word = Microsoft.Office.Interop.Word;
using Excel = Microsoft.Office.Interop.Excel;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;
using Office = Microsoft.Office.Core;
using System.Runtime.InteropServices; // Pour libérer les objets COM
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;
using PdfSharp.Fonts;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using PdfSharp.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Office.Interop.Word;


namespace VerifDocumentSecure.Actions
{
    public class FileToPdfConverter
    {
        #region New Code

        public static bool ConvertToPdf(IFormFile sourceFile, string outputPdfPath)
        {
            string baseDirectory = @"C:\Images\";
            string secureDirectory = Path.Combine(baseDirectory, "SecurePdf");

            if (sourceFile == null || sourceFile.Length == 0)
            {
                Console.WriteLine("Erreur : Le fichier source est null ou vide.");
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

            // Obtenir l'extension du fichier source
            string fileExtension = Path.GetExtension(sourceFile.FileName)?.ToLower();

            Console.WriteLine($"Tentative de conversion de '{sourceFile.FileName}' (Type : {fileExtension}) en PDF...");

            bool conversionSuccessful = false;

            // Certaines conversions nécessitent un chemin physique => on crée un fichier temporaire
            string tempSourcePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + fileExtension);

            try
            {
                // Sauvegarder temporairement le fichier uploadé sur disque si besoin
                using (var stream = new FileStream(tempSourcePath, FileMode.Create))
                {
                    sourceFile.CopyTo(stream);
                }

                switch (fileExtension)
                {
                    case ".doc":
                    case ".docx":
                        conversionSuccessful = WordToPdfConverter.ConvertToPdf(sourceFile, outputPdfPath);
                        break;
                    case ".xls":
                    case ".xlsx":
                        conversionSuccessful = ExcelToPdfConverter.ConvertExcelToPdf(tempSourcePath, Path.Combine(outputPdfPath, Path.ChangeExtension(sourceFile.FileName, ".pdf")));
                        break;
                    case ".ppt":
                    case ".pptx":
                        conversionSuccessful = PptToPdfConverter.ConvertToPdf(tempSourcePath, Path.Combine(outputPdfPath, Path.ChangeExtension(sourceFile.FileName, ".pdf")));
                        break;
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".bmp":
                    case ".gif":
                        conversionSuccessful = ImageToPdfConverter.ConvertImageToPdf(tempSourcePath, Path.Combine(outputPdfPath, Path.ChangeExtension(sourceFile.FileName, ".pdf")));
                        break;
                    case ".txt":
                        // Pour le txt, on peut lire directement depuis IFormFile, donc on peut créer une méthode ConvertTextToPdf avec IFormFilef
                        conversionSuccessful = TextToPdfConverter.ConvertTextToPdf(sourceFile, outputPdfPath);
                        break;
                    default:
                        Console.WriteLine($"Erreur : Le type de fichier '{fileExtension}' n'est pas supporté pour la conversion en PDF par ce code.");
                        break;
                }

                if (conversionSuccessful)
                {
                    PdfSecurityManager.SecurePdf(outputPdfPath, secureDirectory, "pwd123", "log123");
                    Console.WriteLine($"Conversion réussie : '{sourceFile.FileName}' a été converti en '{outputPdfPath}'.");
                }
                else
                {
                    Console.WriteLine($"Échec de la conversion de '{sourceFile.FileName}'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur pendant la conversion : {ex.Message}");
                return false;
            }
            finally
            {
                // Supprimer le fichier temporaire
                if (File.Exists(tempSourcePath))
                {
                    try
                    {
                        File.Delete(tempSourcePath);
                    }
                    catch { }
                }
            }

            return conversionSuccessful;
        }
        #endregion
        #region Code déporté dans d'autres classes
        //private static bool ConvertImageToPdf(IFormFile imageFile, string outputPdfPath)
        //{
        //    try
        //    {
        //        // Crée un nouveau document PDF
        //        PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument();
        //        document.Info.Title = Path.GetFileNameWithoutExtension(imageFile.FileName);

        //        // Ajoute une page au document
        //        PdfSharp.Pdf.PdfPage page = document.AddPage();
        //        XGraphics gfx = XGraphics.FromPdfPage(page);

        //        // Lit le fichier image depuis le stream
        //        using (var imageStream = imageFile.OpenReadStream())
        //        {
        //            XImage image = XImage.FromStream(imageStream);

        //            // Calcule les dimensions pour que l'image s'adapte à la page tout en conservant le ratio
        //            double scaleFactor = Math.Min(page.Width.Point / image.PixelWidth, page.Height.Point / image.PixelHeight);
        //            double scaledWidth = image.PixelWidth * scaleFactor;
        //            double scaledHeight = image.PixelHeight * scaleFactor;

        //            // Centre l'image sur la page
        //            double x = (page.Width.Point - scaledWidth) / 2;
        //            double y = (page.Height.Point - scaledHeight) / 2;

        //            // Dessine l'image sur la page
        //            gfx.DrawImage(image, x, y, scaledWidth, scaledHeight);
        //        }

        //        // Enregistre le document PDF
        //        document.Save(outputPdfPath);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Erreur lors de la conversion de l'image en PDF : {ex.Message}");
        //        return false;
        //    }
        //}

        //private static bool ConvertWordToPdf(IFormFile wordFile, string outputPdfPath)
        //{
        //    Word.Application wordApp = null;
        //    Word.Document wordDoc = null;

        //    // Créer un chemin temporaire pour enregistrer le fichier IFormFile
        //    string tempWordFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(wordFile.FileName));

        //    try
        //    {
        //        // Sauvegarder temporairement le fichier téléchargé
        //        using (var stream = new FileStream(tempWordFilePath, FileMode.Create))
        //        {
        //            wordFile.CopyTo(stream);
        //        }

        //        wordApp = new Word.Application { Visible = false };
        //        object missing = Type.Missing;
        //        object readOnly = false;
        //        object doNotAddToRecentFiles = true;
        //        object filePath = tempWordFilePath;

        //        wordDoc = wordApp.Documents.Open(
        //            ref filePath,
        //            ConfirmConversions: ref missing,
        //            ReadOnly: ref readOnly,
        //            AddToRecentFiles: ref doNotAddToRecentFiles);

        //        wordDoc.ExportAsFixedFormat(
        //            OutputFileName: outputPdfPath,
        //            ExportFormat: Word.WdExportFormat.wdExportFormatPDF);

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Erreur lors de la conversion Word en PDF : {ex.Message}");
        //        return false;
        //    }
        //    finally
        //    {
        //        object missing = Type.Missing;

        //        if (wordDoc != null)
        //        {
        //            object saveChanges = false;
        //            wordDoc.Close(ref saveChanges, ref missing, ref missing);
        //            Marshal.ReleaseComObject(wordDoc);
        //        }

        //        if (wordApp != null)
        //        {
        //            wordApp.Quit(ref missing, ref missing, ref missing);
        //            Marshal.ReleaseComObject(wordApp);
        //        }

        //        // Nettoyage
        //        GC.Collect();
        //        GC.WaitForPendingFinalizers();

        //        // Supprimer le fichier temporaire
        //        if (File.Exists(tempWordFilePath))
        //        {
        //            File.Delete(tempWordFilePath);
        //        }
        //    }
        //}

        //private static bool ConvertExcelToPdf(IFormFile excelFile, string outputPdfPath)
        //{
        //    Excel.Application excelApp = null;
        //    Excel.Workbook excelWorkbook = null;

        //    // Créer un chemin temporaire pour stocker le fichier Excel
        //    string tempExcelFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(excelFile.FileName));

        //    try
        //    {
        //        // Sauvegarder temporairement le fichier Excel depuis le IFormFile
        //        using (var stream = new FileStream(tempExcelFilePath, FileMode.Create))
        //        {
        //            excelFile.CopyTo(stream);
        //        }

        //        excelApp = new Excel.Application { Visible = false };
        //        excelApp.DisplayAlerts = false;

        //        excelWorkbook = excelApp.Workbooks.Open(tempExcelFilePath);

        //        // Exporter en PDF
        //        excelWorkbook.ExportAsFixedFormat(
        //            Type: Excel.XlFixedFormatType.xlTypePDF,
        //            Filename: outputPdfPath,
        //            Quality: Excel.XlFixedFormatQuality.xlQualityStandard,
        //            IncludeDocProperties: true,
        //            IgnorePrintAreas: false,
        //            OpenAfterPublish: false);

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Erreur lors de la conversion Excel en PDF : {ex.Message}");
        //        return false;
        //    }
        //    finally
        //    {
        //        // Libération mémoire et nettoyage des objets COM
        //        if (excelWorkbook != null)
        //        {
        //            object saveChanges = false;
        //            excelWorkbook.Close(saveChanges);
        //            Marshal.ReleaseComObject(excelWorkbook);
        //        }

        //        if (excelApp != null)
        //        {
        //            excelApp.Quit();
        //            Marshal.ReleaseComObject(excelApp);
        //        }

        //        GC.Collect();
        //        GC.WaitForPendingFinalizers();

        //        // Suppression du fichier temporaire
        //        if (File.Exists(tempExcelFilePath))
        //        {
        //            File.Delete(tempExcelFilePath);
        //        }
        //    }
        //}

        //private static bool ConvertPowerPointToPdf(IFormFile pptFile, string outputPdfPath)
        //{
        //    PowerPoint.Application pptApp = null;
        //    PowerPoint.Presentation pptPresentation = null;

        //    // Chemin temporaire pour stocker le fichier PowerPoint
        //    string tempPptFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(pptFile.FileName));

        //    try
        //    {
        //        // Enregistre le fichier temporairement
        //        using (var stream = new FileStream(tempPptFilePath, FileMode.Create))
        //        {
        //            pptFile.CopyTo(stream);
        //        }

        //        pptApp = new PowerPoint.Application { Visible = Office.MsoTriState.msoFalse };

        //        pptPresentation = pptApp.Presentations.Open(
        //            FileName: tempPptFilePath,
        //            WithWindow: Office.MsoTriState.msoFalse,
        //            Untitled: Office.MsoTriState.msoFalse,
        //            ReadOnly: Office.MsoTriState.msoTrue
        //        );

        //        pptPresentation.ExportAsFixedFormat(
        //            Path: outputPdfPath,
        //            FixedFormatType: PowerPoint.PpFixedFormatType.ppFixedFormatTypePDF,
        //            Intent: PowerPoint.PpFixedFormatIntent.ppFixedFormatIntentPrint,
        //            FrameSlides: Office.MsoTriState.msoFalse,
        //            DocStructureTags: true,
        //            BitmapMissingFonts: true,
        //            IncludeDocProperties: true
        //        );

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Erreur lors de la conversion PowerPoint en PDF : {ex.Message}");
        //        return false;
        //    }
        //    finally
        //    {
        //        // Libère les objets COM
        //        if (pptPresentation != null)
        //        {
        //            pptPresentation.Close();
        //            Marshal.ReleaseComObject(pptPresentation);
        //        }
        //        if (pptApp != null)
        //        {
        //            pptApp.Quit();
        //            Marshal.ReleaseComObject(pptApp);
        //        }

        //        GC.Collect();
        //        GC.WaitForPendingFinalizers();

        //        // Supprime le fichier temporaire
        //        if (File.Exists(tempPptFilePath))
        //        {
        //            File.Delete(tempPptFilePath);
        //        }
        //    }
        //}

        //public static bool ConvertTextToPdf(IFormFile inputTextFile, string outputPdfFile)
        //{
        //    try
        //    {
        //        // 1. Créer le dossier de destination s'il n'existe pas
        //        string outputDirectory = Path.GetDirectoryName(outputPdfFile);
        //        if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        //        {
        //            Directory.CreateDirectory(outputDirectory);
        //        }

        //        // 2. Lire tout le contenu du fichier texte depuis le flux IFormFile
        //        string textContent;
        //        using (var reader = new StreamReader(inputTextFile.OpenReadStream()))
        //        {
        //            textContent = reader.ReadToEnd();
        //        }
        //        //string tempPptFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(pptFile.FileName));

        //        // 3. Créer le document PDF
        //        using (PdfWriter writer = new PdfWriter(Path.Combine(outputPdfFile, inputTextFile.FileName.Split(".")[0].ToString() + ".pdf")))
        //        using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(writer))
        //        using (Document document = new Document(pdfDoc))
        //        {
        //            // 4. Configurer le document
        //            document.SetMargins(50, 50, 50, 50); // marges de 50 points

        //            // 5. Diviser le texte en lignes pour préserver le formatage
        //            string[] lines = textContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

        //            // 6. Ajouter chaque ligne au PDF
        //            foreach (string line in lines)
        //            {
        //                Paragraph paragraph = new Paragraph(string.IsNullOrEmpty(line) ? " " : line);
        //                paragraph.SetFontSize(11);
        //                paragraph.SetMarginBottom(2);
        //                document.Add(paragraph);
        //            }
        //        }

        //        Console.WriteLine($"✅ Conversion réussie: '{inputTextFile.FileName}' → '{Path.GetFileName(outputPdfFile)}'");
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Erreur lors de la conversion: {ex.Message}");
        //        return false;
        //    }
        //}


        //#endregion

        //#region Old Code
        ///// <summary>
        ///// Tente de convertir un fichier de n'importe quel type supporté (Word, Excel, PowerPoint, Images) en PDF.
        ///// Nécessite Microsoft Office installé pour les documents Office.
        ///// Nécessite PdfSharp pour les images.
        ///// </summary>
        ///// <param name="sourceFilePath">Le chemin complet du fichier source.</param>
        ///// <param name="outputPdfPath">Le chemin complet où le fichier PDF de sortie sera enregistré.</param>
        ///// <returns>True si la conversion a réussi, False sinon.</returns>
        //public static bool ConvertToPdf(string sourceFilePath, string outputPdfPath)
        //{
        //    string baseDirectory = @"C:\Images\";
        //    string secureDirectory = Path.Combine(baseDirectory, "SecurePdf");

        //    // Vérifie si le fichier source existe
        //    if (!File.Exists(sourceFilePath))
        //    {
        //        Console.WriteLine($"Erreur : Le fichier source '{sourceFilePath}' n'existe pas.");
        //        return false;
        //    }

        //    // Assure que le répertoire de sortie existe
        //    string outputDirectory = Path.GetDirectoryName(outputPdfPath);
        //    if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        //    {
        //        try
        //        {
        //            Directory.CreateDirectory(outputDirectory);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Erreur lors de la création du répertoire de sortie '{outputDirectory}' : {ex.Message}");
        //            return false;
        //        }
        //    }

        //    // Obtient l'extension du fichier source
        //    string fileExtension = Path.GetExtension(sourceFilePath)?.ToLower();

        //    Console.WriteLine($"Tentative de conversion de '{sourceFilePath}' (Type : {fileExtension}) en PDF...");

        //    bool conversionSuccessful = false;

        //    switch (fileExtension)
        //    {
        //        case ".doc":
        //        case ".docx":
        //            conversionSuccessful = ConvertWordToPdf(sourceFilePath, outputPdfPath);
        //            PdfSecurityManager.SecurePdf(outputPdfPath, secureDirectory, "pwd123", "log123");
        //            break;
        //        case ".xls":
        //        case ".xlsx":
        //            conversionSuccessful = ConvertExcelToPdf(sourceFilePath, outputPdfPath);
        //            PdfSecurityManager.SecurePdf(outputPdfPath, secureDirectory, "pwd123", "log123");
        //            break;
        //        case ".ppt":
        //        case ".pptx":
        //            conversionSuccessful = PptToPdfConverter.ConvertToPdf(sourceFilePath, outputPdfPath);
        //            PdfSecurityManager.SecurePdf(outputPdfPath, secureDirectory, "pwd123", "log123");
        //            break;
        //        case ".jpg":
        //        case ".jpeg":
        //        case ".png":
        //        case ".bmp":
        //        case ".gif": // PdfSharp supporte ces formats d'image
        //            conversionSuccessful = ConvertImageToPdf(sourceFilePath, outputPdfPath);
        //            PdfSecurityManager.SecurePdf(outputPdfPath, secureDirectory, "pwd123", "log123");
        //            break;
        //        case ".txt":
        //            conversionSuccessful = ConvertTextToPdf(sourceFilePath, outputPdfPath);
        //            PdfSecurityManager.SecurePdf(outputPdfPath, secureDirectory, "pwd123", "log123");
        //            break;

        //        default:
        //            Console.WriteLine($"Erreur : Le type de fichier '{fileExtension}' n'est pas supporté pour la conversion en PDF par ce code.");
        //            break;
        //    }

        //    if (conversionSuccessful)
        //    {
        //        Console.WriteLine($"Conversion réussie : '{sourceFilePath}' a été converti en '{outputPdfPath}'.");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"Échec de la conversion de '{sourceFilePath}'.");
        //    }

        //    return conversionSuccessful;
        //}

        ///// <summary>
        ///// Convertit une image en PDF en utilisant PdfSharp.
        ///// Une image par page PDF.
        ///// </summary>
        //private static bool ConvertImageToPdf(string sourceFilePath, string outputPdfPath)
        //{
        //    try
        //    {
        //        // Crée un nouveau document PDF
        //        PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument();
        //        document.Info.Title = Path.GetFileNameWithoutExtension(sourceFilePath);

        //        // Ajoute une page au document
        //        PdfSharp.Pdf.PdfPage page = document.AddPage();
        //        XGraphics gfx = XGraphics.FromPdfPage(page);

        //        // Charge l'image
        //        XImage image = XImage.FromFile(sourceFilePath);

        //        // Calcule les dimensions pour que l'image s'adapte à la page tout en conservant le ratio
        //        double scaleFactor = Math.Min(page.Width.Point / image.PixelWidth, page.Height.Point / image.PixelHeight);
        //        double scaledWidth = image.PixelWidth * scaleFactor;
        //        double scaledHeight = image.PixelHeight * scaleFactor;

        //        // Centre l'image sur la page
        //        double x = (page.Width.Point - scaledWidth) / 2;
        //        double y = (page.Height.Point - scaledHeight) / 2;

        //        // Dessine l'image sur la page
        //        gfx.DrawImage(image, x, y, scaledWidth, scaledHeight);

        //        // Enregistre le document PDF
        //        document.Save(outputPdfPath);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Erreur lors de la conversion de l'image en PDF : {ex.Message}");
        //        return false;
        //    }
        //}

        //// --- Fonctions de conversion spécifiques aux types de fichiers ---

        ///// <summary>
        ///// Convertit un document Word en PDF en utilisant l'automatisation de Microsoft Word.
        ///// Nécessite Word installé.
        ///// </summary>
        ////private static bool ConvertWordToPdf(string sourceFilePath, string outputPdfPath)
        ////{
        ////    Word.Application wordApp = null;
        ////    Word.Document wordDoc = null;
        ////    try
        ////    {
        ////        wordApp = new Word.Application { Visible = false };
        ////        object missing = Type.Missing;
        ////        object readOnly = false; // Ouvrir en mode non-lecture seule
        ////        object doNotAddToRecentFiles = true;

        ////        object openAndRepair = true;
        ////        object password = missing;

        ////        wordDoc = wordApp.Documents.Open(
        ////            FileName: sourceFilePath,
        ////            ConfirmConversions: ref missing,
        ////            ReadOnly: ref readOnly,
        ////            AddToRecentFiles: ref doNotAddToRecentFiles,
        ////            PasswordDocument: ref password,
        ////            OpenAndRepair: ref openAndRepair);

        ////        wordDoc.ExportAsFixedFormat(
        ////            OutputFileName: outputPdfPath,
        ////            ExportFormat: Word.WdExportFormat.wdExportFormatPDF);

        ////        return true;
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        Console.WriteLine($"Erreur lors de la conversion Word en PDF : {ex.Message}");
        ////        return false;
        ////    }
        ////    finally
        ////    {
        ////        Object missing = null;
        ////        // Nettoyage des objets COM
        ////        if (wordDoc != null)
        ////        {
        ////            object saveChanges = false;
        ////            wordDoc.Close(ref saveChanges, ref missing, ref missing);
        ////            Marshal.ReleaseComObject(wordDoc);
        ////        }
        ////        if (wordApp != null)
        ////        {
        ////            wordApp.Quit(ref missing, ref missing, ref missing);
        ////            Marshal.ReleaseComObject(wordApp);
        ////        }
        ////        GC.Collect();
        ////        GC.WaitForPendingFinalizers();
        ////    }
        ////}


        //private static bool ConvertWordToPdf(string sourceFilePath, string outputPdfPath)
        //{
        //    Word.Application wordApp = null;
        //    Word.Document wordDoc = null;

        //    try
        //    {
        //        // Vérifie si le fichier est en lecture seule et le rend modifiable
        //        FileInfo fileInfo = new FileInfo(sourceFilePath);
        //        if (fileInfo.IsReadOnly)
        //        {
        //            fileInfo.IsReadOnly = false;
        //        }

        //        wordApp = new Word.Application { Visible = false };
        //        object missing = Type.Missing;
        //        object readOnly = false;
        //        object isVisible = false;
        //        object doNotAddToRecentFiles = true;
        //        object openAndRepair = true;
        //        object password = missing;
        //        object fileName = sourceFilePath;

        //        // Ouvre le document Word
        //        wordDoc = wordApp.Documents.Open(
        //            ref fileName,
        //            ref missing,
        //            ref readOnly,
        //            ref missing,
        //            ref password,
        //            ref missing,
        //            ref missing,
        //            ref missing,
        //            ref missing,
        //            ref missing,
        //            ref isVisible,
        //            ref openAndRepair,
        //            ref missing,
        //            ref doNotAddToRecentFiles,
        //            ref missing,
        //            ref missing);

        //        // Exporte en PDF
        //        wordDoc.ExportAsFixedFormat(
        //            outputPdfPath,
        //            Word.WdExportFormat.wdExportFormatPDF,
        //            OpenAfterExport: false,
        //            OptimizeFor: Word.WdExportOptimizeFor.wdExportOptimizeForPrint,
        //            Range: Word.WdExportRange.wdExportAllDocument,
        //            Item: Word.WdExportItem.wdExportDocumentContent,
        //            IncludeDocProps: true,
        //            KeepIRM: true,
        //            CreateBookmarks: Word.WdExportCreateBookmarks.wdExportCreateHeadingBookmarks,
        //            DocStructureTags: true,
        //            BitmapMissingFonts: true,
        //            UseISO19005_1: false);

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Erreur lors de la conversion Word en PDF : {ex.Message}");
        //        return false;
        //    }
        //    finally
        //    {
        //        // Nettoyage des ressources COM
        //        if (wordDoc != null)
        //        {
        //            wordDoc.Close(false);
        //            Marshal.ReleaseComObject(wordDoc);
        //        }
        //        if (wordApp != null)
        //        {
        //            wordApp.Quit();
        //            Marshal.ReleaseComObject(wordApp);
        //        }

        //        GC.Collect();
        //        GC.WaitForPendingFinalizers();
        //    }
        //}

        ///// <summary>
        ///// Convertit un classeur Excel en PDF en utilisant l'automatisation de Microsoft Excel.
        ///// Nécessite Excel installé.
        ///// </summary>
        //private static bool ConvertExcelToPdf(string sourceFilePath, string outputPdfPath)
        //{
        //    Excel.Application excelApp = null;
        //    Excel.Workbook excelWorkbook = null;
        //    try
        //    {
        //        excelApp = new Excel.Application { Visible = false };
        //        excelApp.DisplayAlerts = false; // Supprime les alertes (ex: "voulez-vous enregistrer les modifications?")

        //        excelWorkbook = excelApp.Workbooks.Open(sourceFilePath);

        //        // Exporte le classeur entier en PDF
        //        excelWorkbook.ExportAsFixedFormat(
        //            Type: Excel.XlFixedFormatType.xlTypePDF,
        //            Filename: outputPdfPath,
        //            Quality: Excel.XlFixedFormatQuality.xlQualityStandard,
        //            IncludeDocProperties: true,
        //            IgnorePrintAreas: false,
        //            OpenAfterPublish: false);

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Erreur lors de la conversion Excel en PDF : {ex.Message}");
        //        return false;
        //    }
        //    finally
        //    {
        //        // Nettoyage des objets COM
        //        if (excelWorkbook != null)
        //        {
        //            object saveChanges = false;
        //            excelWorkbook.Close(saveChanges);
        //            Marshal.ReleaseComObject(excelWorkbook);
        //        }
        //        if (excelApp != null)
        //        {
        //            excelApp.Quit();
        //            Marshal.ReleaseComObject(excelApp);
        //        }
        //        GC.Collect();
        //        GC.WaitForPendingFinalizers();
        //    }
        //}

        ///// <summary>
        ///// Convertit une présentation PowerPoint en PDF en utilisant l'automatisation de Microsoft PowerPoint.
        ///// Nécessite PowerPoint installé.
        ///// </summary>
        //private static bool ConvertPowerPointToPdf(string sourceFilePath, string outputPdfPath)
        //{
        //    PowerPoint.Application pptApp = null;
        //    PowerPoint.Presentation pptPresentation = null;
        //    try
        //    {
        //        pptApp = new PowerPoint.Application { Visible = Office.MsoTriState.msoFalse };

        //        pptPresentation = pptApp.Presentations.Open(
        //            FileName: sourceFilePath,
        //            WithWindow: Office.MsoTriState.msoFalse, // Ne pas ouvrir avec une fenêtre visible
        //            Untitled: Office.MsoTriState.msoFalse,
        //            ReadOnly: Office.MsoTriState.msoTrue // Ouvrir en lecture seule
        //        );

        //        pptPresentation.ExportAsFixedFormat(
        //            Path: outputPdfPath,
        //            FixedFormatType: PowerPoint.PpFixedFormatType.ppFixedFormatTypePDF,
        //            Intent: PowerPoint.PpFixedFormatIntent.ppFixedFormatIntentPrint, // Correction ici
        //            FrameSlides: Office.MsoTriState.msoFalse,
        //            //HandoutLayout: PowerPoint.PpPrintHandoutOrder.ppPrintHandoutVerticalFirst,
        //            DocStructureTags: true,
        //            BitmapMissingFonts: true,
        //            IncludeDocProperties: true
        //        // Supprimez cette ligne car le paramètre n'est pas attendu ici
        //        // PptConverterActionAfterPublish: PowerPoint.PpConverterActionAfterPublish.ppConverterActioinNone
        //        );

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Erreur lors de la conversion PowerPoint en PDF : {ex.Message}");
        //        return false;
        //    }
        //    finally
        //    {
        //        // Nettoyage des objets COM
        //        if (pptPresentation != null)
        //        {
        //            pptPresentation.Close();
        //            Marshal.ReleaseComObject(pptPresentation);
        //        }
        //        if (pptApp != null)
        //        {
        //            pptApp.Quit();
        //            Marshal.ReleaseComObject(pptApp);
        //        }
        //        GC.Collect();
        //        GC.WaitForPendingFinalizers();
        //    }
        //}

        ///// <summary>
        ///// Convertit un fichier texte (.txt) en fichier PDF
        ///// </summary>
        ///// <param name="inputTextFile">Chemin complet vers le fichier .txt à convertir</param>
        ///// <param name="outputPdfFile">Chemin complet où sauvegarder le fichier PDF</param>
        ///// <returns>true si la conversion réussit, false sinon</returns>
        //public static bool ConvertTextToPdf(string inputTextFile, string outputPdfFile)
        //{
        //    try
        //    {
        //        // 1. Vérifier que le fichier texte existe
        //        if (!File.Exists(inputTextFile))
        //        {
        //            Console.WriteLine($"❌ Erreur: Le fichier '{inputTextFile}' n'existe pas.");
        //            return false;
        //        }

        //        // 2. Créer le dossier de destination s'il n'existe pas
        //        string outputDirectory = Path.GetDirectoryName(outputPdfFile);
        //        if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        //        {
        //            Directory.CreateDirectory(outputDirectory);
        //        }

        //        // 3. Lire tout le contenu du fichier texte
        //        string textContent = File.ReadAllText(inputTextFile);

        //        // 4. Créer le document PDF
        //        using (PdfWriter writer = new PdfWriter(outputPdfFile))
        //        using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(writer))
        //        using (Document document = new Document(pdfDoc))
        //        {
        //            // 5. Configurer le document
        //            document.SetMargins(50, 50, 50, 50); // marges de 50 points

        //            // 6. Diviser le texte en lignes pour préserver le formatage
        //            string[] lines = textContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

        //            // 7. Ajouter chaque ligne au PDF
        //            foreach (string line in lines)
        //            {
        //                // Créer un paragraphe pour chaque ligne
        //                Paragraph paragraph = new Paragraph(string.IsNullOrEmpty(line) ? " " : line);

        //                // Configurer le style du paragraphe
        //                paragraph.SetFontSize(11);
        //                paragraph.SetMarginBottom(2); // petit espacement entre les lignes

        //                // Ajouter le paragraphe au document
        //                document.Add(paragraph);
        //            }
        //        }

        //        // 8. Confirmer le succès
        //        Console.WriteLine($"✅ Conversion réussie: '{Path.GetFileName(inputTextFile)}' → '{Path.GetFileName(outputPdfFile)}'");
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        // 9. Gérer les erreurs
        //        Console.WriteLine($"❌ Erreur lors de la conversion: {ex.Message}");
        //        return false;
        //    }
        //}

        ///// <summary>
        ///// Version avec options personnalisables
        ///// </summary>
        ///// <param name="inputTextFile">Fichier texte d'entrée</param>
        ///// <param name="outputPdfFile">Fichier PDF de sortie</param>
        ///// <param name="fontSize">Taille de la police (défaut: 11)</param>
        ///// <param name="addTitle">Ajouter le nom du fichier comme titre (défaut: true)</param>
        ///// <returns>true si succès, false sinon</returns>
        //public static bool ConvertTextToPdf(string inputTextFile, string outputPdfFile, float fontSize = 11f, bool addTitle = true)
        //{
        //    try
        //    {
        //        if (!File.Exists(inputTextFile))
        //        {
        //            Console.WriteLine($"❌ Erreur: Le fichier '{inputTextFile}' n'existe pas.");
        //            return false;
        //        }

        //        string outputDirectory = Path.GetDirectoryName(outputPdfFile);
        //        if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        //        {
        //            Directory.CreateDirectory(outputDirectory);
        //        }

        //        string textContent = File.ReadAllText(inputTextFile);

        //        using (PdfWriter writer = new PdfWriter(outputPdfFile))
        //        using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(writer))
        //        using (Document document = new Document(pdfDoc))
        //        {
        //            document.SetMargins(50, 50, 50, 50);

        //            // Ajouter un titre si demandé
        //            if (addTitle)
        //            {
        //                string fileName = Path.GetFileNameWithoutExtension(inputTextFile);
        //                Paragraph title = new Paragraph(fileName)
        //                    .SetFontSize(fontSize + 4)
        //                    //.SetBold()
        //                    .SetTextAlignment(TextAlignment.CENTER)
        //                    .SetMarginBottom(20);
        //                document.Add(title);
        //            }

        //            string[] lines = textContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

        //            foreach (string line in lines)
        //            {
        //                Paragraph paragraph = new Paragraph(string.IsNullOrEmpty(line) ? " " : line)
        //                    .SetFontSize(fontSize)
        //                    .SetMarginBottom(2);

        //                document.Add(paragraph);
        //            }
        //        }

        //        Console.WriteLine($"✅ Conversion réussie avec titre={addTitle}, taille={fontSize}");
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Erreur lors de la conversion: {ex.Message}");
        //        return false;
        //    }
        //}
        //#endregion
        #endregion
    }
}