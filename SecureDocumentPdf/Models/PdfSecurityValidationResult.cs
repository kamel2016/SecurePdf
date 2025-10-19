namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Résultat de la validation de sécurité d'un PDF
    /// </summary>
    public class PdfSecurityValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> SecurityIssues { get; set; } = new List<string>();

        // Informations sur le document
        public string DocumentId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }

        // Vérifications de sécurité
        public bool HashVerified { get; set; }
        public string ExpectedHash { get; set; }
        public string ActualHash { get; set; }

        public bool SignatureVerified { get; set; }
        public string SignerName { get; set; }
        public DateTime? SignatureDate { get; set; }

        public bool IntegrityVerified { get; set; }
        public bool IsCorrupted { get; set; }

        // Permissions du document
        public PdfPermissionsInfo Permissions { get; set; }

        // Métadonnées de sécurité
        public bool HasWatermark { get; set; }
        public string WatermarkText { get; set; }
        public bool IsEncrypted { get; set; }
        public string EncryptionLevel { get; set; }

        // Token de lecture (si validé)
        public string ViewToken { get; set; }
        public DateTime TokenExpiration { get; set; }

        // Classification
        public string Classification { get; set; } // CONFIDENTIEL, PUBLIC, etc.

        public PdfSecurityValidationResult()
        {
            Permissions = new PdfPermissionsInfo();
        }
    }
}