namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Options de securisation d'un PDF
    /// </summary>
    public class PdfSecurityOptions
    {
        // Protection par mot de passe
        public bool EnablePasswordProtection { get; set; }
        public string UserPassword { get; set; }
        public string OwnerPassword { get; set; }

        // Permissions
        public bool AllowPrinting { get; set; } = true;
        public bool AllowCopy { get; set; } = false;
        public bool AllowModifyContents { get; set; } = false;
        public bool AllowModifyAnnotations { get; set; } = false;

        // Filigrane
        public bool EnableWatermark { get; set; }
        public string WatermarkText { get; set; }
        public float WatermarkOpacity { get; set; } = 0.3f;
        public float WatermarkRotation { get; set; } = 45f;

        // Chiffrement avance
        public bool EnableAdvancedEncryption { get; set; }
        public string EncryptionPassword { get; set; }

        // Signature numerique
        public bool EnableDigitalSignature { get; set; }
        public string CertificatePath { get; set; }
        public string CertificatePassword { get; set; }
        public string SignatureReason { get; set; }
        public string SignatureLocation { get; set; }

        // Hash et integrite
        public bool ComputeHash { get; set; } = true;

        // Metadonnees
        public bool RemoveMetadata { get; set; } = true;

        /// <summary>
        /// Valide les options
        /// </summary>
        public (bool isValid, string errorMessage) Validate()
        {
            if (EnablePasswordProtection)
            {
                if (string.IsNullOrWhiteSpace(UserPassword) && string.IsNullOrWhiteSpace(OwnerPassword))
                {
                    return (false, "Au moins un mot de passe (utilisateur ou proprietaire) est requis");
                }
            }

            if (EnableWatermark && string.IsNullOrWhiteSpace(WatermarkText))
            {
                return (false, "Le texte du filigrane est requis quand le filigrane est active");
            }

            if (EnableAdvancedEncryption && string.IsNullOrWhiteSpace(EncryptionPassword))
            {
                return (false, "Le mot de passe de chiffrement est requis");
            }

            if (EnableDigitalSignature)
            {
                if (string.IsNullOrWhiteSpace(CertificatePath))
                {
                    return (false, "Le chemin du certificat est requis pour la signature");
                }
            }

            return (true, null);
        }

        /// <summary>
        /// Configuration par defaut securisee
        /// </summary>
        public static PdfSecurityOptions GetDefaultSecure()
        {
            return new PdfSecurityOptions
            {
                EnablePasswordProtection = false,
                EnableWatermark = false,
                EnableAdvancedEncryption = false,
                EnableDigitalSignature = false,
                ComputeHash = true,
                RemoveMetadata = true,
                AllowPrinting = true,
                AllowCopy = false,
                AllowModifyContents = false,
                AllowModifyAnnotations = false
            };
        }
    }
}
