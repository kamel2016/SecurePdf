using SecureDocumentPdf.Models;

namespace SecureDocumentPdf.Services.Interface
{
    /// <summary>
    /// Définit les opérations de validation de sécurité pour les fichiers PDF.
    /// </summary>
    public interface IPdfSecurityValidator
    {
        /// <summary>
        /// Valide toutes les sécurités d’un PDF (intégrité, permissions, chiffrement, etc.).
        /// </summary>
        Task<PdfSecurityValidationResult> ValidateAsync(PdfViewRequest request);

        /// <summary>
        /// Vérifie uniquement l’intégrité (hash) du PDF.
        /// </summary>
        Task<bool> VerifyIntegrityAsync(byte[] pdfData, string expectedHash);

        /// <summary>
        /// Vérifie la signature numérique du PDF.
        /// </summary>
        Task<bool> VerifySignatureAsync(byte[] pdfData);

        /// <summary>
        /// Extrait les permissions d’accès et d’impression du PDF.
        /// </summary>
        Task<PdfPermissionsInfo> ExtractPermissionsAsync(byte[] pdfData, string password = null);

        /// <summary>
        /// Génère un jeton sécurisé pour la visualisation temporaire d’un document PDF.
        /// </summary>
        string GenerateViewToken(string documentId, int expirationMinutes = 60);

        /// <summary>
        /// Valide un jeton de visualisation pour s’assurer qu’il est encore valide et correspond au document.
        /// </summary>
        bool ValidateViewToken(string token, string documentId);
    }
}