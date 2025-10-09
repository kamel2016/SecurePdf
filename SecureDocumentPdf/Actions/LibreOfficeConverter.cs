namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur utilisant LibreOffice (pour tous les formats Office)
    /// Nécessite LibreOffice installé sur le serveur
    /// </summary>
    public class LibreOfficeConverter
    {
        private static string _libreOfficePath = "/usr/bin/libreoffice"; // Linux
        // Pour Windows: "C:\\Program Files\\LibreOffice\\program\\soffice.exe"

        public static void SetLibreOfficePath(string path)
        {
            _libreOfficePath = path;
        }

        public static byte[] ConvertToPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            string tempInputPath = null;
            string tempOutputDir = null;

            try
            {
                // Créer un fichier temporaire
                tempInputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
                tempOutputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempOutputDir);

                using (var stream = new FileStream(tempInputPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                // Exécuter LibreOffice en ligne de commande
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _libreOfficePath,
                        Arguments = $"--headless --convert-to pdf --outdir \"{tempOutputDir}\" \"{tempInputPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit(30000); // Timeout 30 secondes

                // Lire le PDF généré
                string pdfFileName = Path.GetFileNameWithoutExtension(tempInputPath) + ".pdf";
                string pdfPath = Path.Combine(tempOutputDir, pdfFileName);

                if (File.Exists(pdfPath))
                {
                    return File.ReadAllBytes(pdfPath);
                }

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempInputPath))
                        File.Delete(tempInputPath);

                    if (Directory.Exists(tempOutputDir))
                        Directory.Delete(tempOutputDir, true);
                }
                catch { }
            }
        }

        public static Stream ConvertToPdfStream(IFormFile file)
        {
            byte[] pdfBytes = ConvertToPdf(file);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
}