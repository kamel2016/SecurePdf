using SecureDocumentPdf.Models;

namespace SecureDocumentPdf.Services.Interface
{
    /// <summary>
    /// Interface pour le service de transfert securise de fichiers
    /// </summary>
    public interface IFileTransferService
    {
        /// <summary>
        /// Cree un nouveau transfert securise
        /// </summary>
        Task<CreateTransferResponse> CreateTransferAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CreateTransferRequest request);

        /// <summary>
        /// Recupere les informations d'un transfert
        /// </summary>
        Task<TransferInfo> GetTransferInfoAsync(string transferId, string accessToken);

        /// <summary>
        /// Telecharge un fichier transfere
        /// </summary>
        Task<(Stream fileStream, string fileName, string contentType)> DownloadFileAsync(
            DownloadRequest request,
            string ipAddress,
            string userAgent);

        /// <summary>
        /// Verifie si un transfert est valide et accessible
        /// </summary>
        Task<bool> ValidateTransferAsync(string transferId, string accessToken, string password = null);

        /// <summary>
        /// Supprime un transfert (par le createur)
        /// </summary>
        Task<bool> DeleteTransferAsync(string transferId, string accessToken);

        /// <summary>
        /// Nettoie les transferts expires
        /// </summary>
        Task CleanupExpiredTransfersAsync();

        /// <summary>
        /// Obtient les statistiques d'un transfert
        /// </summary>
        Task<TransferStatistics> GetTransferStatisticsAsync(string transferId, string accessToken);
    }

    /// <summary>
    /// Statistiques d'un transfert
    /// </summary>
    public class TransferStatistics
    {
        public string TransferId { get; set; }
        public int TotalDownloads { get; set; }
        public int SuccessfulDownloads { get; set; }
        public int FailedDownloads { get; set; }
        public DateTime LastDownloadAt { get; set; }
        public bool IsExpired { get; set; }
        public int RemainingDownloads { get; set; }
    }
}