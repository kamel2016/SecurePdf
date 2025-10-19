namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Resultat de la securisation
    /// </summary>
    public class PdfSecurityResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public byte[] SecuredPdfData { get; set; }
        public string FileHash { get; set; }
        public PdfSecurityOptions AppliedOptions { get; set; }
        public DateTime SecuredAt { get; set; }
        public long OriginalSize { get; set; }
        public long SecuredSize { get; set; }
    }
}