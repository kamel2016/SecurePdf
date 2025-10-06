using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

namespace VerifDocumentSecure.Actions
{
    public class TextToPdfConverter
    {
        public static bool ConvertTextToPdf(IFormFile inputTextFile, string outputPdfFile)
        {
            try
            {
                // 1. Créer le dossier de destination s'il n'existe pas
                string outputDirectory = Path.GetDirectoryName(outputPdfFile);
                if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // 2. Lire tout le contenu du fichier texte depuis le flux IFormFile
                string textContent;
                using (var reader = new StreamReader(inputTextFile.OpenReadStream()))
                {
                    textContent = reader.ReadToEnd();
                }
                //string tempPptFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(pptFile.FileName));

                // 3. Créer le document PDF
                using (PdfWriter writer = new PdfWriter(Path.Combine(outputPdfFile, inputTextFile.FileName.Split(".")[0].ToString() + ".pdf")))
                using (iText.Kernel.Pdf.PdfDocument pdfDoc = new PdfDocument(writer))
                using (Document document = new Document(pdfDoc))
                {
                    // 4. Configurer le document
                    document.SetMargins(50, 50, 50, 50); // marges de 50 points

                    // 5. Diviser le texte en lignes pour préserver le formatage
                    string[] lines = textContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

                    // 6. Ajouter chaque ligne au PDF
                    foreach (string line in lines)
                    {
                        Paragraph paragraph = new Paragraph(string.IsNullOrEmpty(line) ? " " : line);
                        paragraph.SetFontSize(11);
                        paragraph.SetMarginBottom(2);
                        document.Add(paragraph);
                    }
                }

                Console.WriteLine($"✅ Conversion réussie: '{inputTextFile.FileName}' → '{Path.GetFileName(outputPdfFile)}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de la conversion: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Convertit un fichier texte (.txt) en fichier PDF
        /// </summary>
        /// <param name="inputTextFile">Chemin complet vers le fichier .txt à convertir</param>
        /// <param name="outputPdfFile">Chemin complet où sauvegarder le fichier PDF</param>
        /// <returns>true si la conversion réussit, false sinon</returns>
        public static bool ConvertTextToPdf(string inputTextFile, string outputPdfFile)
        {
            try
            {
                // 1. Vérifier que le fichier texte existe
                if (!File.Exists(inputTextFile))
                {
                    Console.WriteLine($"❌ Erreur: Le fichier '{inputTextFile}' n'existe pas.");
                    return false;
                }

                // 2. Créer le dossier de destination s'il n'existe pas
                string outputDirectory = Path.GetDirectoryName(outputPdfFile);
                if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // 3. Lire tout le contenu du fichier texte
                string textContent = File.ReadAllText(inputTextFile);

                // 4. Créer le document PDF
                using (PdfWriter writer = new PdfWriter(outputPdfFile))
                using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(writer))
                using (Document document = new Document(pdfDoc))
                {
                    // 5. Configurer le document
                    document.SetMargins(50, 50, 50, 50); // marges de 50 points

                    // 6. Diviser le texte en lignes pour préserver le formatage
                    string[] lines = textContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

                    // 7. Ajouter chaque ligne au PDF
                    foreach (string line in lines)
                    {
                        // Créer un paragraphe pour chaque ligne
                        Paragraph paragraph = new Paragraph(string.IsNullOrEmpty(line) ? " " : line);

                        // Configurer le style du paragraphe
                        paragraph.SetFontSize(11);
                        paragraph.SetMarginBottom(2); // petit espacement entre les lignes

                        // Ajouter le paragraphe au document
                        document.Add(paragraph);
                    }
                }

                // 8. Confirmer le succès
                Console.WriteLine($"✅ Conversion réussie: '{Path.GetFileName(inputTextFile)}' → '{Path.GetFileName(outputPdfFile)}'");
                return true;
            }
            catch (Exception ex)
            {
                // 9. Gérer les erreurs
                Console.WriteLine($"❌ Erreur lors de la conversion: {ex.Message}");
                return false;
            }
        }
    }
}