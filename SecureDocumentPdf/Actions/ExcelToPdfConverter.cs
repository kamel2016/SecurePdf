using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;

namespace VerifDocumentSecure.Actions
{
    public class ExcelToPdfConverter
    {
        /// <summary>
        /// Convertit un classeur Excel en PDF en utilisant l'automatisation de Microsoft Excel.
        /// Nécessite Excel installé.
        /// </summary>
        public static bool ConvertExcelToPdf(string sourceFilePath, string outputPdfPath)
        {
            Excel.Application excelApp = null;
            Excel.Workbook excelWorkbook = null;
            try
            {
                excelApp = new Excel.Application { Visible = false };
                excelApp.DisplayAlerts = false; // Supprime les alertes (ex: "voulez-vous enregistrer les modifications?")

                excelWorkbook = excelApp.Workbooks.Open(sourceFilePath);

                // Exporte le classeur entier en PDF
                excelWorkbook.ExportAsFixedFormat(
                    Type: Excel.XlFixedFormatType.xlTypePDF,
                    Filename: outputPdfPath,
                    Quality: Excel.XlFixedFormatQuality.xlQualityStandard,
                    IncludeDocProperties: true,
                    IgnorePrintAreas: false,
                    OpenAfterPublish: false);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la conversion Excel en PDF : {ex.Message}");
                return false;
            }
            finally
            {
                // Nettoyage des objets COM
                if (excelWorkbook != null)
                {
                    object saveChanges = false;
                    excelWorkbook.Close(saveChanges);
                    Marshal.ReleaseComObject(excelWorkbook);
                }
                if (excelApp != null)
                {
                    excelApp.Quit();
                    Marshal.ReleaseComObject(excelApp);
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static bool ConvertExcelToPdf(IFormFile excelFile, string outputPdfPath)
        {
            Excel.Application excelApp = null;
            Excel.Workbook excelWorkbook = null;

            // Créer un chemin temporaire pour stocker le fichier Excel
            string tempExcelFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(excelFile.FileName));

            try
            {
                // Sauvegarder temporairement le fichier Excel depuis le IFormFile
                using (var stream = new FileStream(tempExcelFilePath, FileMode.Create))
                {
                    excelFile.CopyTo(stream);
                }

                excelApp = new Excel.Application { Visible = false };
                excelApp.DisplayAlerts = false;

                excelWorkbook = excelApp.Workbooks.Open(tempExcelFilePath);

                // Exporter en PDF
                excelWorkbook.ExportAsFixedFormat(
                    Type: Excel.XlFixedFormatType.xlTypePDF,
                    Filename: outputPdfPath,
                    Quality: Excel.XlFixedFormatQuality.xlQualityStandard,
                    IncludeDocProperties: true,
                    IgnorePrintAreas: false,
                    OpenAfterPublish: false);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la conversion Excel en PDF : {ex.Message}");
                return false;
            }
            finally
            {
                // Libération mémoire et nettoyage des objets COM
                if (excelWorkbook != null)
                {
                    object saveChanges = false;
                    excelWorkbook.Close(saveChanges);
                    Marshal.ReleaseComObject(excelWorkbook);
                }

                if (excelApp != null)
                {
                    excelApp.Quit();
                    Marshal.ReleaseComObject(excelApp);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Suppression du fichier temporaire
                if (File.Exists(tempExcelFilePath))
                {
                    File.Delete(tempExcelFilePath);
                }
            }
        }
    }
}