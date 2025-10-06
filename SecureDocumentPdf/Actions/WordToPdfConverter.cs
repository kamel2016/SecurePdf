using Microsoft.Office.Interop.Word;
using System.Runtime.InteropServices;

namespace VerifDocumentSecure.Actions
{
    public class WordToPdfConverter
    {
        public static bool ConvertToPdf(string inputFilePath, string outputDirectory)
        {
            if (!File.Exists(inputFilePath))
                return false;

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            Application wordApp = null;
            Document wordDoc = null;

            try
            {
                wordApp = new Application { Visible = false };
                object missing = Type.Missing;
                object readOnly = true;
                object inputPath = inputFilePath;

                wordDoc = wordApp.Documents.Open(ref inputPath, ref missing, ref readOnly,
                                                 ref missing, ref missing, ref missing,
                                                 ref missing, ref missing, ref missing,
                                                 ref missing, ref missing, ref missing,
                                                 ref missing, ref missing, ref missing, ref missing);

                string outputFileName = Path.GetFileNameWithoutExtension(inputFilePath) + ".pdf";
                string outputPath = Path.Combine(outputDirectory, outputFileName);

                wordDoc.ExportAsFixedFormat(outputPath, WdExportFormat.wdExportFormatPDF);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (wordDoc != null)
                {
                    wordDoc.Close(false);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordDoc);
                }

                if (wordApp != null)
                {
                    wordApp.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public static bool ConvertToPdf(IFormFile wordFile, string outputDirectory)
        {
            if (wordFile == null || wordFile.Length == 0)
                return false;

            // Créer un fichier temporaire pour le Word
            string tempWordPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(wordFile.FileName));
            try
            {
                using (var stream = new FileStream(tempWordPath, FileMode.Create))
                {
                    wordFile.CopyTo(stream);
                }

                // Créer le répertoire de sortie si nécessaire
                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                string outputPdfPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(wordFile.FileName) + ".pdf");

                // Supprimer le PDF existant s’il est déjà là
                if (File.Exists(outputPdfPath))
                    File.Delete(outputPdfPath);

                Application wordApp = null;
                Document wordDoc = null;

                try
                {
                    wordApp = new Application { Visible = false };
                    object missing = Type.Missing;
                    object readOnly = true;
                    object inputPath = tempWordPath;

                    wordDoc = wordApp.Documents.Open(ref inputPath, ref missing, ref readOnly,
                                                     ref missing, ref missing, ref missing,
                                                     ref missing, ref missing, ref missing,
                                                     ref missing, ref missing, ref missing,
                                                     ref missing, ref missing, ref missing, ref missing);

                    wordDoc.ExportAsFixedFormat(outputPdfPath, WdExportFormat.wdExportFormatPDF);
                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    if (wordDoc != null)
                    {
                        wordDoc.Close(false);
                        Marshal.ReleaseComObject(wordDoc);
                    }

                    if (wordApp != null)
                    {
                        wordApp.Quit();
                        Marshal.ReleaseComObject(wordApp);
                    }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                // Nettoyer le fichier Word temporaire
                if (File.Exists(tempWordPath))
                    File.Delete(tempWordPath);
            }
        }
    }
}