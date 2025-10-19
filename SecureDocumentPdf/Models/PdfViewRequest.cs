namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Modèle pour la demande de visualisation
    /// </summary>
    public class PdfViewRequest
    {
        public string DocumentId { get; set; }
        public string FileName { get; set; }
        public byte[] FileData { get; set; }
        public string ExpectedHash { get; set; }
        public string UserPassword { get; set; }
    }
}