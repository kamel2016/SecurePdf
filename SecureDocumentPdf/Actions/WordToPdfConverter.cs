using Microsoft.Office.Interop.Word;
using NPOI.XWPF.UserModel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Runtime.InteropServices;

namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur Word vers PDF (avec NPOI + QuestPDF)
    /// </summary>
    public class WordToPdfConverter
    {
        public static byte[] ConvertToPdf(IFormFile wordFile)
        {
            if (wordFile == null || wordFile.Length == 0)
                return null;

            try
            {
                using (var stream = new MemoryStream())
                {
                    wordFile.CopyTo(stream);
                    stream.Position = 0;

                    XWPFDocument doc = new XWPFDocument(stream);

                    var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(40);

                            page.Content().Column(column =>
                            {
                                foreach (var paragraph in doc.Paragraphs)
                                {
                                    var text = paragraph.Text;
                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        var textStyle = column.Item().Text(text);

                                        // Appliquer le style si bold
                                        if (paragraph.Runs.Any(r => r.IsBold))
                                            textStyle.Bold();

                                        textStyle.FontSize(11);
                                    }
                                    column.Item().PaddingBottom(5);
                                }
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

        public static Stream ConvertToPdfStream(IFormFile wordFile)
        {
            byte[] pdfBytes = ConvertToPdf(wordFile);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
    //public class WordToPdfConverter
    //{
    //    /// <summary>
    //    /// Convertit un fichier Word uploadé depuis l'IHM en PDF et retourne le PDF sous forme de bytes
    //    /// </summary>
    //    public static byte[] ConvertToPdf(IFormFile wordFile)
    //    {
    //        if (wordFile == null || wordFile.Length == 0)
    //            return null;

    //        string tempWordPath = null;
    //        string tempPdfPath = null;
    //        Application wordApp = null;
    //        Document wordDoc = null;

    //        try
    //        {
    //            // Créer un fichier temporaire pour le Word uploadé
    //            tempWordPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(wordFile.FileName)}");

    //            using (var stream = new FileStream(tempWordPath, FileMode.Create))
    //            {
    //                wordFile.CopyTo(stream);
    //            }

    //            // Créer un chemin temporaire pour le PDF
    //            tempPdfPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");

    //            // Ouvrir Word et convertir
    //            wordApp = new Application { Visible = false };
    //            object missing = Type.Missing;
    //            object readOnly = true;
    //            object inputPath = tempWordPath;

    //            wordDoc = wordApp.Documents.Open(ref inputPath, ref missing, ref readOnly,
    //                                             ref missing, ref missing, ref missing,
    //                                             ref missing, ref missing, ref missing,
    //                                             ref missing, ref missing, ref missing,
    //                                             ref missing, ref missing, ref missing, ref missing);

    //            wordDoc.ExportAsFixedFormat(tempPdfPath, WdExportFormat.wdExportFormatPDF);

    //            // Lire le PDF en mémoire
    //            byte[] pdfBytes = File.ReadAllBytes(tempPdfPath);
    //            return pdfBytes;
    //        }
    //        catch
    //        {
    //            return null;
    //        }
    //        finally
    //        {
    //            // Fermer les objets COM
    //            if (wordDoc != null)
    //            {
    //                wordDoc.Close(false);
    //                Marshal.ReleaseComObject(wordDoc);
    //            }

    //            if (wordApp != null)
    //            {
    //                wordApp.Quit();
    //                Marshal.ReleaseComObject(wordApp);
    //            }

    //            GC.Collect();
    //            GC.WaitForPendingFinalizers();

    //            // Nettoyer les fichiers temporaires
    //            try
    //            {
    //                if (File.Exists(tempWordPath))
    //                    File.Delete(tempWordPath);

    //                if (File.Exists(tempPdfPath))
    //                    File.Delete(tempPdfPath);
    //            }
    //            catch
    //            {
    //                // Ignorer les erreurs de nettoyage
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// Convertit un fichier Word uploadé depuis l'IHM en PDF et retourne un Stream
    //    /// Utile pour les réponses HTTP directes
    //    /// </summary>
    //    public static Stream ConvertToPdfStream(IFormFile wordFile)
    //    {
    //        byte[] pdfBytes = ConvertToPdf(wordFile);

    //        if (pdfBytes == null)
    //            return null;

    //        return new MemoryStream(pdfBytes);
    //    }
    //}
}
