using PdfSharp.Pdf.IO;
using SecureDocumentPdf.Models;
using SecureDocumentPdf.Services.Interface;
using System.Security.Cryptography;
using System.Text;

namespace SecureDocumentPdf.Services
{
    /// <summary>
    /// Service de validation de sécurité des PDF
    /// </summary>
    public class PdfSecurityValidator : IPdfSecurityValidator
    {
        private readonly ILogger<PdfSecurityValidator> _logger;
        private const string SECRET_KEY = "VOTRE_CLE_SECRETE_ICI_CHANGEZ_MOI"; // À changer en production

        public PdfSecurityValidator(ILogger<PdfSecurityValidator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Valide toutes les sécurités d'un PDF
        /// </summary>
        public async Task<PdfSecurityValidationResult> ValidateAsync(PdfViewRequest request)
        {
            var result = new PdfSecurityValidationResult
            {
                DocumentId = request.DocumentId,
                FileName = request.FileName,
                FileSize = request.FileData?.Length ?? 0,
                IsValid = true
            };

            try
            {
                // 1. Vérification basique
                if (request.FileData == null || request.FileData.Length == 0)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Fichier PDF vide ou invalide";
                    result.SecurityIssues.Add("Aucune donnée de fichier fournie");
                    return result;
                }

                // 2. Vérifier que c'est bien un PDF
                if (!IsPdfFile(request.FileData))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Le fichier n'est pas un PDF valide";
                    result.SecurityIssues.Add("Format de fichier incorrect");
                    result.IsCorrupted = true;
                    return result;
                }

                // 3. Vérifier l'intégrité (hash)
                if (!string.IsNullOrEmpty(request.ExpectedHash))
                {
                    result.ExpectedHash = request.ExpectedHash;
                    result.ActualHash = await ComputeHashAsync(request.FileData);
                    result.HashVerified = string.Equals(
                        result.ExpectedHash,
                        result.ActualHash,
                        StringComparison.OrdinalIgnoreCase);

                    if (!result.HashVerified)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = "Le document a été modifié (hash invalide)";
                        result.SecurityIssues.Add("L'empreinte du fichier ne correspond pas");
                        result.IntegrityVerified = false;
                        return result;
                    }

                    result.IntegrityVerified = true;
                }

                // 4. Vérifier la structure du PDF (pas corrompu)
                var corruptionCheck = await CheckPdfCorruptionAsync(request.FileData, request.UserPassword);
                result.IsCorrupted = corruptionCheck.IsCorrupted;

                if (result.IsCorrupted)
                {
                    result.IsValid = false;
                    result.ErrorMessage = corruptionCheck.ErrorMessage;
                    result.SecurityIssues.Add("Le fichier PDF est corrompu ou endommagé");
                    return result;
                }

                // 5. Extraire les permissions
                result.Permissions = await ExtractPermissionsAsync(request.FileData, request.UserPassword);

                // 6. Détecter le chiffrement
                result.IsEncrypted = await IsEncryptedAsync(request.FileData);
                if (result.IsEncrypted)
                {
                    result.EncryptionLevel = "AES-128 ou supérieur";
                }

                // 7. Générer token de visualisation si tout est OK
                if (result.IsValid)
                {
                    result.ViewToken = GenerateViewToken(request.DocumentId, 60);
                    result.TokenExpiration = DateTime.UtcNow.AddMinutes(60);
                }

                _logger.LogInformation($"Validation PDF réussie pour {request.FileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la validation du PDF {request.FileName}");
                result.IsValid = false;
                result.ErrorMessage = $"Erreur technique: {ex.Message}";
                result.SecurityIssues.Add($"Exception: {ex.GetType().Name}");
            }

            return result;
        }

        /// <summary>
        /// Vérifie uniquement l'intégrité (hash)
        /// </summary>
        public async Task<bool> VerifyIntegrityAsync(byte[] pdfData, string expectedHash)
        {
            if (string.IsNullOrEmpty(expectedHash))
                return true;

            string actualHash = await ComputeHashAsync(pdfData);
            return string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Vérifie la signature numérique
        /// </summary>
        public async Task<bool> VerifySignatureAsync(byte[] pdfData)
        {
            return await Task.Run(() =>
            {
                // PdfSharp ne supporte pas la vérification de signature
                // Retourner false par défaut
                return false;
            });
        }

        /// <summary>
        /// Extrait les permissions du PDF
        /// </summary>
        public async Task<PdfPermissionsInfo> ExtractPermissionsAsync(byte[] pdfData, string password = null)
        {
            return await Task.Run(() =>
            {
                var permissions = new PdfPermissionsInfo();

                try
                {
                    using (var ms = new MemoryStream(pdfData))
                    {
                        var document = PdfReader.Open(ms, password, PdfDocumentOpenMode.InformationOnly);

                        if (document.SecuritySettings != null)
                        {
                            var settings = document.SecuritySettings;
                            permissions.AllowPrinting = settings.PermitFullQualityPrint;
                            permissions.AllowCopy = settings.PermitExtractContent;
                            permissions.AllowModifyContents = settings.PermitModifyDocument;
                            permissions.AllowModifyAnnotations = settings.PermitAnnotations;
                            permissions.AllowFillIn = settings.PermitFormsFill;
                            permissions.AllowScreenReaders = false;
                            permissions.AllowAssembly = settings.PermitAssembleDocument;
                            permissions.AllowDegradedPrinting = settings.PermitPrint;
                        }
                        else
                        {
                            // Pas de sécurité = toutes permissions activées
                            permissions.AllowPrinting = true;
                            permissions.AllowCopy = true;
                            permissions.AllowModifyContents = true;
                            permissions.AllowModifyAnnotations = true;
                            permissions.AllowFillIn = true;
                            permissions.AllowScreenReaders = true;
                            permissions.AllowAssembly = true;
                            permissions.AllowDegradedPrinting = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erreur extraction permissions, utilisation valeurs par défaut");
                }

                return permissions;
            });
        }

        /// <summary>
        /// Génère un token de visualisation sécurisé
        /// </summary>
        public string GenerateViewToken(string documentId, int expirationMinutes = 60)
        {
            var expiration = DateTime.UtcNow.AddMinutes(expirationMinutes);
            var data = $"{documentId}|{expiration:yyyy-MM-ddTHH:mm:ss}";

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SECRET_KEY)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var signature = Convert.ToBase64String(hash);
                var token = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(data))}|{signature}";
                return token;
            }
        }

        /// <summary>
        /// Valide un token de visualisation
        /// </summary>
        public bool ValidateViewToken(string token, string documentId)
        {
            try
            {
                var parts = token.Split('|');
                if (parts.Length != 2)
                    return false;

                var dataBase64 = parts[0];
                var signature = parts[1];

                // Vérifier la signature
                var data = Encoding.UTF8.GetString(Convert.FromBase64String(dataBase64));

                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SECRET_KEY)))
                {
                    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                    var expectedSignature = Convert.ToBase64String(hash);

                    if (signature != expectedSignature)
                        return false;
                }

                // Vérifier l'expiration
                var dataParts = data.Split('|');
                if (dataParts.Length != 2)
                    return false;

                var tokenDocId = dataParts[0];
                var expirationStr = dataParts[1];

                if (tokenDocId != documentId)
                    return false;

                if (!DateTime.TryParse(expirationStr, out DateTime expiration))
                    return false;

                if (DateTime.UtcNow > expiration)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        #region Méthodes Privées

        private bool IsPdfFile(byte[] data)
        {
            if (data == null || data.Length < 5)
                return false;

            // PDF commence toujours par %PDF-
            return data[0] == 0x25 && // %
                   data[1] == 0x50 && // P
                   data[2] == 0x44 && // D
                   data[3] == 0x46 && // F
                   data[4] == 0x2D;   // -
        }

        private async Task<string> ComputeHashAsync(byte[] data)
        {
            return await Task.Run(() =>
            {
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(data);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            });
        }

        private async Task<(bool IsCorrupted, string ErrorMessage)> CheckPdfCorruptionAsync(byte[] pdfData, string password)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var ms = new MemoryStream(pdfData))
                    {
                        var document = PdfReader.Open(ms, password, PdfDocumentOpenMode.InformationOnly);

                        // Si on arrive ici, le PDF est valide
                        if (document.PageCount == 0)
                            return (true, "Le PDF ne contient aucune page");

                        return (false, null);
                    }
                }
                catch (PdfReaderException ex)
                {
                    return (true, $"PDF corrompu: {ex.Message}");
                }
                catch (Exception ex)
                {
                    return (true, $"Erreur lecture PDF: {ex.Message}");
                }
            });
        }

        private async Task<bool> IsEncryptedAsync(byte[] pdfData)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var ms = new MemoryStream(pdfData))
                    {
                        // Essayer d'ouvrir le PDF
                        var document = PdfReader.Open(ms, PdfDocumentOpenMode.InformationOnly);

                        // Vérifier si des restrictions sont appliquées
                        var settings = document.SecuritySettings;

                        bool hasRestrictions =
                            settings.PermitModifyDocument == false ||
                            settings.PermitExtractContent == false ||
                            settings.PermitPrint == false;

                        return hasRestrictions;
                    }
                }
                catch (PdfReaderException ex) when (ex.Message.Contains("password") || ex.Message.Contains("encrypted"))
                {
                    // Le PDF nécessite un mot de passe
                    return true;
                }
                catch
                {
                    // Autre erreur, considérer comme non chiffré
                    return false;
                }
            });
        }
        private async Task<bool> DetectWatermarkAsync(byte[] pdfData, string password)
        {
            return await Task.Run(() =>
            {
                // Détection simple: vérifier si le PDF a du contenu graphique répété
                // (PdfSharp ne permet pas d'extraire facilement le contenu texte)
                // Pour une vraie détection, il faudrait analyser le contenu de chaque page
                return false; // Par défaut, considérer qu'il n'y a pas de filigrane détectable
            });
        }

        #endregion
    }
}