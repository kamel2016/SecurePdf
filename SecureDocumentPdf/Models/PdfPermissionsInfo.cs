namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Informations sur les permissions du PDF
    /// </summary>
    public class PdfPermissionsInfo
    {
        public bool AllowPrinting { get; set; }
        public bool AllowCopy { get; set; }
        public bool AllowModifyContents { get; set; }
        public bool AllowModifyAnnotations { get; set; }
        public bool AllowFillIn { get; set; }
        public bool AllowScreenReaders { get; set; }
        public bool AllowAssembly { get; set; }
        public bool AllowDegradedPrinting { get; set; }

        /// <summary>
        /// Vérifie si le document est en lecture seule
        /// </summary>
        public bool IsReadOnly => !AllowModifyContents && !AllowModifyAnnotations;

        /// <summary>
        /// Vérifie si toutes les permissions sont désactivées (ultra-sécurisé)
        /// </summary>
        public bool IsFullyRestricted => !AllowPrinting && !AllowCopy && !AllowModifyContents;
    }
}