namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Modele pour un transfert de fichier securise
    /// </summary>
    public class FileTransfer
    {
        public string TransferId { get; set; }
        public string FileName { get; set; }
        public string OriginalFileName { get; set; }
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; }

        // Securite
        public string EncryptedFilePath { get; set; }
        public string FileHash { get; set; }
        public string EncryptionKey { get; set; }
        public string AccessToken { get; set; }
        public string PasswordHash { get; set; }

        // Parametres du transfert
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int MaxDownloads { get; set; }
        public int CurrentDownloads { get; set; }

        // Metadonnees
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
        public string RecipientEmail { get; set; }
        public string Message { get; set; }

        // Statut
        public TransferStatus Status { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt || CurrentDownloads >= MaxDownloads;

        // Traçabilite
        public List<DownloadLog> DownloadLogs { get; set; }

        public FileTransfer()
        {
            TransferId = Guid.NewGuid().ToString("N");
            CreatedAt = DateTime.UtcNow;
            Status = TransferStatus.Active;
            CurrentDownloads = 0;
            DownloadLogs = new List<DownloadLog>();
        }
    }

    /// <summary>
    /// Statut d'un transfert
    /// </summary>
    public enum TransferStatus
    {
        Active,
        Expired,
        Deleted,
        Suspended
    }

    /// <summary>
    /// Log de Téléchargement
    /// </summary>
    public class DownloadLog
    {
        public DateTime DownloadedAt { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Requete pour creer un transfert
    /// </summary>
    public class CreateTransferRequest
    {
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
        public string RecipientEmail { get; set; }
        public string Message { get; set; }
        public int ExpirationHours { get; set; }
        public int MaxDownloads { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// Reponse de creation de transfert
    /// </summary>
    public class CreateTransferResponse
    {
        public bool Success { get; set; }
        public string TransferId { get; set; }
        public string AccessToken { get; set; }
        public string ShareUrl { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Requete pour Télécharger un fichier
    /// </summary>
    public class DownloadRequest
    {
        public string TransferId { get; set; }
        public string AccessToken { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// Informations sur un transfert (sans donnees sensibles)
    /// </summary>
    public class TransferInfo
    {
        public string TransferId { get; set; }
        public string FileName { get; set; }
        public long FileSizeBytes { get; set; }
        public string FileSizeFormatted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int MaxDownloads { get; set; }
        public int CurrentDownloads { get; set; }
        public bool IsExpired { get; set; }
        public bool RequiresPassword { get; set; }
        public string SenderName { get; set; }
        public string Message { get; set; }
    }
}