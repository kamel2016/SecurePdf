namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur OpenDocument (ODT, ODS, ODP) vers PDF
    /// Utilise LibreOffice - doit être installé sur le serveur
    /// </summary>
    public class OpenDocumentToPdfConverter
    {
        private static string _libreOfficePath = "/usr/bin/libreoffice"; // Linux
        // Pour Windows: "C:\\Program Files\\LibreOffice\\program\\soffice.exe"

        public static void SetLibreOfficePath(string path)
        {
            _libreOfficePath = path;
        }

        public static byte[] ConvertToPdf(IFormFile odFile)
        {
            if (odFile == null || odFile.Length == 0)
                return null;

            string tempInputPath = null;
            string tempOutputDir = null;

            try
            {
                tempInputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(odFile.FileName)}");
                tempOutputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempOutputDir);

                using (var stream = new FileStream(tempInputPath, FileMode.Create))
                {
                    odFile.CopyTo(stream);
                }

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
                process.WaitForExit(30000);

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

        public static Stream ConvertToPdfStream(IFormFile odFile)
        {
            byte[] pdfBytes = ConvertToPdf(odFile);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
}