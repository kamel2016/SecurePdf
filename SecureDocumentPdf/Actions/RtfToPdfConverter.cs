namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur RTF vers PDF
    /// </summary>
    public class RtfToPdfConverter
    {
        public static byte[] ConvertToPdf(IFormFile rtfFile)
        {
            if (rtfFile == null || rtfFile.Length == 0)
                return null;

            try
            {
                using (var reader = new StreamReader(rtfFile.OpenReadStream()))
                {
                    string rtfContent = reader.ReadToEnd();
                    string htmlContent = RtfPipe.Rtf.ToHtml(rtfContent);

                    return HtmlToPdfConverter.ConvertHtmlStringToPdf(htmlContent);
                }
            }
            catch
            {
                return null;
            }
        }

        public static Stream ConvertToPdfStream(IFormFile rtfFile)
        {
            byte[] pdfBytes = ConvertToPdf(rtfFile);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
}