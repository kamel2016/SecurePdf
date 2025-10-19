using SecureDocumentPdf.Models;
using SecureDocumentPdf.Services.Interface;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace SecureDocumentPdf.Services
{
    /// <summary>
    /// Service de transfert securise de fichiers
    /// </summary>
    public class FileTransferService : IFileTransferService
    {
        private readonly ILogger<FileTransferService> _logger;
        private readonly string _transferStoragePath;

        // Stockage en memoire (en production, utiliser une base de donnees)
        private static readonly ConcurrentDictionary<string, FileTransfer> _transfers = new();

        public FileTransferService(ILogger<FileTransferService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _transferStoragePath = Path.Combine(env.WebRootPath, "transfers");
            Directory.CreateDirectory(_transferStoragePath);
        }

        /// <summary>
        /// Cree un nouveau transfert securise
        /// </summary>
        public async Task<CreateTransferResponse> CreateTransferAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CreateTransferRequest request)
        {
            try
            {
                // Validation
                if (fileStream == null || fileStream.Length == 0)
                {
                    return new CreateTransferResponse
                    {
                        Success = false,
                        ErrorMessage = "Fichier vide ou invalide"
                    };
                }

                // Limiter la taille (2 GB max)
                if (fileStream.Length > 2L * 1024 * 1024 * 1024)
                {
                    return new CreateTransferResponse
                    {
                        Success = false,
                        ErrorMessage = "Fichier trop volumineux (max 2 GB)"
                    };
                }

                // Creer le transfert
                var transfer = new FileTransfer
                {
                    OriginalFileName = fileName,
                    FileName = $"{Guid.NewGuid():N}_{SanitizeFileName(fileName)}",
                    FileSizeBytes = fileStream.Length,
                    ContentType = contentType,
                    SenderEmail = request.SenderEmail,
                    SenderName = request.SenderName,
                    RecipientEmail = request.RecipientEmail,
                    Message = request.Message,
                    ExpiresAt = DateTime.UtcNow.AddHours(request.ExpirationHours),
                    MaxDownloads = request.MaxDownloads > 0 ? request.MaxDownloads : int.MaxValue,
                    AccessToken = GenerateSecureToken()
                };

                // Generer une cle de chiffrement unique
                transfer.EncryptionKey = GenerateEncryptionKey();

                // Chiffrer et sauvegarder le fichier
                var encryptedPath = Path.Combine(_transferStoragePath, $"{transfer.TransferId}.enc");
                await EncryptAndSaveFileAsync(fileStream, encryptedPath, transfer.EncryptionKey);
                transfer.EncryptedFilePath = encryptedPath;

                // Calculer le hash du fichier original
                fileStream.Position = 0;
                transfer.FileHash = await ComputeHashAsync(fileStream);

                // Hash du mot de passe si fourni
                if (!string.IsNullOrEmpty(request.Password))
                {
                    transfer.PasswordHash = HashPassword(request.Password);
                }

                // Sauvegarder le transfert
                _transfers[transfer.TransferId] = transfer;

                _logger.LogInformation($"Transfert cree : {transfer.TransferId}, Fichier : {fileName}, Taille : {fileStream.Length} bytes");

                var shareUrl = $"/Transfer?transferId={transfer.TransferId}&token={transfer.AccessToken}";
                return new CreateTransferResponse
                {
                    Success = true,
                    TransferId = transfer.TransferId,
                    AccessToken = transfer.AccessToken,
                    ShareUrl = shareUrl,
                    ExpiresAt = transfer.ExpiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la creation du transfert");
                return new CreateTransferResponse
                {
                    Success = false,
                    ErrorMessage = $"Erreur : {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Recupere les informations d'un transfert
        /// </summary>
        public async Task<TransferInfo> GetTransferInfoAsync(string transferId, string accessToken)
        {
            await Task.CompletedTask;

            if (!_transfers.TryGetValue(transferId, out var transfer))
            {
                return null;
            }

            if (transfer.AccessToken != accessToken)
            {
                _logger.LogWarning($"Token invalide pour le transfert {transferId}");
                return null;
            }

            return new TransferInfo
            {
                TransferId = transfer.TransferId,
                FileName = transfer.OriginalFileName,
                FileSizeBytes = transfer.FileSizeBytes,
                FileSizeFormatted = FormatFileSize(transfer.FileSizeBytes),
                CreatedAt = transfer.CreatedAt,
                ExpiresAt = transfer.ExpiresAt,
                MaxDownloads = transfer.MaxDownloads,
                CurrentDownloads = transfer.CurrentDownloads,
                IsExpired = transfer.IsExpired,
                RequiresPassword = !string.IsNullOrEmpty(transfer.PasswordHash),
                SenderName = transfer.SenderName,
                Message = transfer.Message
            };
        }

        /// <summary>
        /// Télécharge un fichier transfere
        /// </summary>
        public async Task<(Stream fileStream, string fileName, string contentType)> DownloadFileAsync(
            DownloadRequest request,
            string ipAddress,
            string userAgent)
        {
            if (!_transfers.TryGetValue(request.TransferId, out var transfer))
            {
                throw new InvalidOperationException("Transfert introuvable");
            }

            // Verifier le token
            if (transfer.AccessToken != request.AccessToken)
            {
                LogDownloadAttempt(transfer, ipAddress, userAgent, false, "Token invalide");
                throw new UnauthorizedAccessException("Token invalide");
            }

            // Verifier l'expiration
            if (transfer.IsExpired)
            {
                LogDownloadAttempt(transfer, ipAddress, userAgent, false, "Transfert expire");
                throw new InvalidOperationException("Ce transfert a expire");
            }

            // Verifier le mot de passe si requis
            if (!string.IsNullOrEmpty(transfer.PasswordHash))
            {
                if (string.IsNullOrEmpty(request.Password))
                {
                    LogDownloadAttempt(transfer, ipAddress, userAgent, false, "Mot de passe requis");
                    throw new UnauthorizedAccessException("Mot de passe requis");
                }

                if (!VerifyPassword(request.Password, transfer.PasswordHash))
                {
                    LogDownloadAttempt(transfer, ipAddress, userAgent, false, "Mot de passe incorrect");
                    throw new UnauthorizedAccessException("Mot de passe incorrect");
                }
            }

            // Dechiffrer le fichier
            var decryptedStream = await DecryptFileAsync(transfer.EncryptedFilePath, transfer.EncryptionKey);

            // Incrementer le compteur de Téléchargements
            transfer.CurrentDownloads++;
            LogDownloadAttempt(transfer, ipAddress, userAgent, true, null);

            _logger.LogInformation($"Téléchargement reussi : {transfer.TransferId}, Téléchargements : {transfer.CurrentDownloads}/{transfer.MaxDownloads}");

            return (decryptedStream, transfer.OriginalFileName, transfer.ContentType);
        }

        /// <summary>
        /// Valide un transfert
        /// </summary>
        public async Task<bool> ValidateTransferAsync(string transferId, string accessToken, string password = null)
        {
            await Task.CompletedTask;

            if (!_transfers.TryGetValue(transferId, out var transfer))
            {
                return false;
            }

            if (transfer.AccessToken != accessToken)
            {
                return false;
            }

            if (transfer.IsExpired)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(transfer.PasswordHash) && !string.IsNullOrEmpty(password))
            {
                return VerifyPassword(password, transfer.PasswordHash);
            }

            return true;
        }

        /// <summary>
        /// Supprime un transfert
        /// </summary>
        public async Task<bool> DeleteTransferAsync(string transferId, string accessToken)
        {
            await Task.CompletedTask;

            if (!_transfers.TryGetValue(transferId, out var transfer))
            {
                return false;
            }

            if (transfer.AccessToken != accessToken)
            {
                return false;
            }

            // Supprimer le fichier chiffre
            try
            {
                if (File.Exists(transfer.EncryptedFilePath))
                {
                    File.Delete(transfer.EncryptedFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur suppression fichier : {transfer.EncryptedFilePath}");
            }

            // Supprimer du dictionnaire
            _transfers.TryRemove(transferId, out _);

            _logger.LogInformation($"Transfert supprime : {transferId}");
            return true;
        }

        /// <summary>
        /// Nettoie les transferts expires
        /// </summary>
        public async Task CleanupExpiredTransfersAsync()
        {
            await Task.Run(() =>
            {
                var expiredTransfers = _transfers.Values.Where(t => t.IsExpired).ToList();

                foreach (var transfer in expiredTransfers)
                {
                    try
                    {
                        if (File.Exists(transfer.EncryptedFilePath))
                        {
                            File.Delete(transfer.EncryptedFilePath);
                        }

                        _transfers.TryRemove(transfer.TransferId, out _);
                        _logger.LogInformation($"Transfert expire nettoye : {transfer.TransferId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Erreur nettoyage transfert : {transfer.TransferId}");
                    }
                }
            });
        }

        /// <summary>
        /// Obtient les statistiques d'un transfert
        /// </summary>
        public async Task<TransferStatistics> GetTransferStatisticsAsync(string transferId, string accessToken)
        {
            await Task.CompletedTask;

            if (!_transfers.TryGetValue(transferId, out var transfer))
            {
                return null;
            }

            if (transfer.AccessToken != accessToken)
            {
                return null;
            }

            var successful = transfer.DownloadLogs.Count(l => l.Success);
            var failed = transfer.DownloadLogs.Count(l => !l.Success);

            return new TransferStatistics
            {
                TransferId = transfer.TransferId,
                TotalDownloads = transfer.DownloadLogs.Count,
                SuccessfulDownloads = successful,
                FailedDownloads = failed,
                LastDownloadAt = transfer.DownloadLogs.Any()
                    ? transfer.DownloadLogs.Max(l => l.DownloadedAt)
                    : DateTime.MinValue,
                IsExpired = transfer.IsExpired,
                RemainingDownloads = transfer.MaxDownloads - transfer.CurrentDownloads
            };
        }

        #region Methodes privees

        private async Task EncryptAndSaveFileAsync(Stream inputStream, string outputPath, string encryptionKey)
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(encryptionKey);
            aes.GenerateIV();

            using var fileStream = File.Create(outputPath);

            // Ecrire l'IV au debut du fichier
            await fileStream.WriteAsync(aes.IV, 0, aes.IV.Length);

            using var cryptoStream = new CryptoStream(fileStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            await inputStream.CopyToAsync(cryptoStream);
        }

        private async Task<Stream> DecryptFileAsync(string encryptedPath, string encryptionKey)
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(encryptionKey);

            using var fileStream = File.OpenRead(encryptedPath);

            // Lire l'IV
            var iv = new byte[16];
            await fileStream.ReadAsync(iv, 0, iv.Length);
            aes.IV = iv;

            var decryptedStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(fileStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                await cryptoStream.CopyToAsync(decryptedStream);
            }

            decryptedStream.Position = 0;
            return decryptedStream;
        }

        private string GenerateEncryptionKey()
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            return Convert.ToBase64String(aes.Key);
        }

        private string GenerateSecureToken()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        private async Task<string> ComputeHashAsync(Stream stream)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "SALT_SECRET"));
            return Convert.ToBase64String(hashBytes);
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            var computedHash = HashPassword(password);
            return computedHash == passwordHash;
        }

        private void LogDownloadAttempt(FileTransfer transfer, string ipAddress, string userAgent, bool success, string errorMessage)
        {
            transfer.DownloadLogs.Add(new DownloadLog
            {
                DownloadedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = success,
                ErrorMessage = errorMessage
            });
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars));
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        #endregion
    }
}