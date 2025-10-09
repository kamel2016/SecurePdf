namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur universel qui détecte automatiquement le type de fichier
    /// </summary>
    public class UniversalToPdfConverter
    {
        public static byte[] ConvertToPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            string extension = Path.GetExtension(file.FileName).ToLower();

            return extension switch
            {
                ".docx" => WordToPdfConverter.ConvertToPdf(file),
                ".xlsx" => ExcelToPdfConverter.ConvertToPdf(file),
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" => ImageToPdfConverter.ConvertToPdf(file),
                ".txt" => TextToPdfConverter.ConvertToPdf(file),
                ".rtf" => RtfToPdfConverter.ConvertToPdf(file),
                ".csv" => CsvToPdfConverter.ConvertToPdf(file),
                ".md" or ".markdown" => MarkdownToPdfConverter.ConvertToPdf(file),
                ".html" or ".htm" => HtmlToPdfConverter.ConvertToPdf(file),
                ".xml" => XmlToPdfConverter.ConvertToPdf(file),
                ".json" => JsonToPdfConverter.ConvertToPdf(file),
                ".svg" => SvgToPdfConverter.ConvertToPdf(file),
                ".eml" => EmailToPdfConverter.ConvertToPdf(file),
                ".odt" or ".ods" or ".odp" => OpenDocumentToPdfConverter.ConvertToPdf(file),
                _ => null
            };
        }

        public static Stream ConvertToPdfStream(IFormFile file)
        {
            byte[] pdfBytes = ConvertToPdf(file);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
}