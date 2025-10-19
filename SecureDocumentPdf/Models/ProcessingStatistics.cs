// ========================================
// Models/UploadResult.cs
// ========================================
namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Résultat du processus de sécurisation PDF
    /// Contient toutes les informations sur le traitement effectué
    /// </summary>
    public class UploadResult
    {
        /// <summary>
        /// Indique si le traitement a réussi
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message principal à afficher à l'utilisateur
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Chemin relatif du PDF sécurisé (pour téléchargement)
        /// </summary>
        public string? SecuredPdfPath { get; set; }

        /// <summary>
        /// Chemin relatif du fichier de preuve JSON
        /// </summary>
        public string? ProofFilePath { get; set; }

        /// <summary>
        /// Détails de l'erreur si le traitement a échoué
        /// </summary>
        public string? ErrorDetails { get; set; }

        public string QRCodePath { get; set; }
        public string AuditLogPath { get; set; }
        public string DocumentId { get; set; }
        public string BiometricSignature { get; set; }
        public Dictionary<int, string> PageHashes { get; set; }
        public List<string> HashChain { get; set; }
        public string IpAddress { get; set; }
        public DateTime? ExpirationDate { get; set; }
        /// <summary>
        /// Liste des étapes de traitement effectuées avec succès
        /// </summary>
        public List<string> ProcessingSteps { get; set; } = new();

        /// <summary>
        /// Hash SHA256 du fichier original
        /// </summary>
        public string? OriginalHash { get; set; }

        /// <summary>
        /// Hash SHA256 du fichier traité
        /// </summary>
        public string? ProcessedHash { get; set; }

        /// <summary>
        /// Date et heure UTC du traitement
        /// </summary>
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Taille du fichier en bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Nom du fichier original
        /// </summary>
        public string? OriginalFileName { get; set; }

        /// <summary>
        /// Durée du traitement en secondes
        /// </summary>
        public double ProcessingDurationSeconds { get; set; }

        /// <summary>
        /// NOUVEAU : Indique si le PDF est protégé par mot de passe
        /// </summary>
        public bool IsPasswordProtected { get; set; }

        /// <summary>
        /// NOUVEAU : Informations sur la protection appliquée (sans révéler le mot de passe)
        /// </summary>
        public string? ProtectionInfo { get; set; }

        /// <summary>
        /// Niveau de sécurité appliqué
        /// </summary>
        public string SecurityLevel => IsPasswordProtected ? "Maximum - Chiffrement 128-bit" : "Standard";

        /// <summary>
        /// Taille du fichier formatée (KB, MB, GB)
        /// </summary>
        public string FileSizeFormatted
        {
            get
            {
                if (FileSizeBytes == 0) return "0 Bytes";

                string[] sizes = { "Bytes", "KB", "MB", "GB" };
                double len = FileSizeBytes;
                int order = 0;

                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }

                return $"{len:0.##} {sizes[order]}";
            }
        }

        /// <summary>
        /// Résumé des mesures de sécurité appliquées
        /// </summary>
        public List<string> SecurityMeasuresSummary => new()
        {
            "✅ Nettoyage des métadonnées",
            "✅ Watermark de sécurité",
            "✅ Signature numérique PAdES",
            IsPasswordProtected ? "🔒 Protection par mot de passe (Chiffrement 128-bit)" : "⚠️ Pas de protection par mot de passe",
            "✅ Horodatage RFC3161",
            "✅ Validation d'intégrité (SHA-256)"
        };
    }
}


// ========================================
// Models/PdfUploadRequest.cs
// ========================================
namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Modèle pour la requête d'upload de PDF
    /// </summary>
    public class PdfUploadRequest
    {
        /// <summary>
        /// Fichier PDF à traiter
        /// </summary>
        public IFormFile? PdfFile { get; set; }

        /// <summary>
        /// Nom de l'utilisateur qui effectue l'upload
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Options de traitement
        /// </summary>
        public PdfProcessingOptions? Options { get; set; }
    }
}


// ========================================
// Models/PdfProcessingOptions.cs
// ========================================
namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Options de configuration pour le traitement PDF
    /// </summary>
    public class PdfProcessingOptions
    {
        /// <summary>
        /// Activer le nettoyage des métadonnées (défaut: true)
        /// </summary>
        public bool EnableMetadataCleanup { get; set; } = true;

        /// <summary>
        /// Activer l'application du watermark (défaut: true)
        /// </summary>
        public bool EnableWatermark { get; set; } = true;

        /// <summary>
        /// Activer la signature numérique (défaut: true)
        /// </summary>
        public bool EnableDigitalSignature { get; set; } = true;

        /// <summary>
        /// Activer l'horodatage (défaut: true)
        /// </summary>
        public bool EnableTimestamp { get; set; } = true;

        /// <summary>
        /// Activer la génération d'images de prévisualisation (défaut: true)
        /// </summary>
        public bool EnablePreviewGeneration { get; set; } = true;

        /// <summary>
        /// Texte personnalisé pour le watermark
        /// </summary>
        public string? CustomWatermarkText { get; set; }

        /// <summary>
        /// Qualité de compression du PDF (1-100)
        /// </summary>
        public int CompressionQuality { get; set; } = 90;
    }
}


// ========================================
// Models/PdfMetadata.cs
// ========================================
namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Métadonnées extraites ou nettoyées du PDF
    /// </summary>
    public class PdfMetadata
    {
        /// <summary>
        /// Titre du document
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Auteur du document
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Sujet du document
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Mots-clés
        /// </summary>
        public string? Keywords { get; set; }

        /// <summary>
        /// Créateur (application)
        /// </summary>
        public string? Creator { get; set; }

        /// <summary>
        /// Producteur (logiciel de génération)
        /// </summary>
        public string? Producer { get; set; }

        /// <summary>
        /// Date de création originale
        /// </summary>
        public DateTime? CreationDate { get; set; }

        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime? ModificationDate { get; set; }

        /// <summary>
        /// Nombre de pages
        /// </summary>
        public int PageCount { get; set; }

        /// <summary>
        /// Version PDF
        /// </summary>
        public string? PdfVersion { get; set; }
    }
}


// ========================================
// Models/ProofFile.cs
// ========================================
namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Modèle pour le fichier de preuve JSON
    /// </summary>
    public class ProofFile
    {
        /// <summary>
        /// Nom du fichier PDF sécurisé
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Nom de l'utilisateur ayant effectué le traitement
        /// </summary>
        public string ProcessedBy { get; set; } = string.Empty;

        /// <summary>
        /// Date et heure UTC du traitement
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// Hash SHA256 du fichier original
        /// </summary>
        public string OriginalHash { get; set; } = string.Empty;

        /// <summary>
        /// Hash SHA256 du fichier traité
        /// </summary>
        public string ProcessedHash { get; set; } = string.Empty;

        /// <summary>
        /// Liste des mesures de sécurité appliquées
        /// </summary>
        public List<string> SecurityMeasures { get; set; } = new();

        /// <summary>
        /// Horodatage RFC3161
        /// </summary>
        public string Timestamp { get; set; } = string.Empty;

        /// <summary>
        /// Version du fichier de preuve
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Application ayant généré le fichier
        /// </summary>
        public string Application { get; set; } = "PDF Security App";

        /// <summary>
        /// Étapes de traitement effectuées
        /// </summary>
        public List<string> ProcessingSteps { get; set; } = new();

        /// <summary>
        /// Informations sur le certificat de signature (si applicable)
        /// </summary>
        public CertificateInfo? Certificate { get; set; }
    }

    /// <summary>
    /// Informations sur le certificat utilisé pour la signature
    /// </summary>
    public class CertificateInfo
    {
        public string Subject { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
    }
}


// ========================================
// Models/ErrorViewModel.cs
// ========================================
namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Modèle pour la page d'erreur
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// ID de la requête pour le traçage
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Indique si l'ID de requête doit être affiché
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        /// <summary>
        /// Message d'erreur à afficher
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Détails techniques de l'erreur
        /// </summary>
        public string? ErrorDetails { get; set; }

        /// <summary>
        /// Code d'erreur HTTP
        /// </summary>
        public int StatusCode { get; set; }
    }
}


// ========================================
// Models/ValidationResult.cs
// ========================================
namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Résultat de validation d'un fichier PDF
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Le fichier est-il valide ?
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Liste des erreurs de validation
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Liste des avertissements
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Taille du fichier en bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Type MIME détecté
        /// </summary>
        public string? DetectedMimeType { get; set; }

        /// <summary>
        /// Extension du fichier
        /// </summary>
        public string? FileExtension { get; set; }
    }
}


// ========================================
// Models/ProcessingStatistics.cs
// ========================================
namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Statistiques de traitement pour monitoring
    /// </summary>
    public class ProcessingStatistics
    {
        /// <summary>
        /// Nombre total de PDFs traités
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// Nombre de succès
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Nombre d'échecs
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// Durée moyenne de traitement (secondes)
        /// </summary>
        public double AverageProcessingTime { get; set; }

        /// <summary>
        /// Taille totale des fichiers traités (bytes)
        /// </summary>
        public long TotalBytesProcessed { get; set; }

        /// <summary>
        /// Date de la dernière opération
        /// </summary>
        public DateTime? LastProcessedAt { get; set; }

        /// <summary>
        /// Utilisateur le plus actif
        /// </summary>
        public string? MostActiveUser { get; set; }
    }
}