namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Modèle pour la réponse de visualisation
    /// </summary>
    public class PdfViewResponse
    {
        public bool CanView { get; set; }
        public string ViewToken { get; set; }
        public string ErrorMessage { get; set; }
        public PdfSecurityValidationResult ValidationResult { get; set; }
        public string PdfDataUrl { get; set; } // Base64 pour affichage
    }
}