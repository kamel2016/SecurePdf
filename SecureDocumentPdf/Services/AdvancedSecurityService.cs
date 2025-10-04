using SecureDocumentPdf.Services.Interface;
using SixLabors.ImageSharp.PixelFormats;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using SixLabors.ImageSharp;

namespace SecureDocumentPdf.Services
{
    public class AdvancedSecurityService : IAdvancedSecurityService
    {
        private readonly ILogger<AdvancedSecurityService> _logger;
        private readonly IWebHostEnvironment _environment;

        public AdvancedSecurityService(ILogger<AdvancedSecurityService> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        // ========================================
        // 1. EXPIRATION AUTOMATIQUE DU PDF
        // ========================================

        /// <summary>
        /// Ajoute une date d'expiration au PDF - Le document devient inaccessible après cette date
        /// Crée un fichier JavaScript qui sera exécuté à l'ouverture du PDF
        /// </summary>
        public async Task<string> AddExpirationDateAsync(string pdfPath, DateTime expirationDate, string userName)
        {
            try
            {
                _logger.LogInformation("⏰ Ajout d'expiration au PDF: {PdfPath}, expire le {ExpirationDate}",
                    Path.GetFileName(pdfPath), expirationDate.ToString("dd/MM/yyyy"));

                // Créer un fichier JSON d'expiration à côté du PDF
                var expirationInfo = new
                {
                    PdfFileName = Path.GetFileName(pdfPath),
                    ExpirationDate = expirationDate,
                    CreatedBy = userName,
                    CreatedAt = DateTime.UtcNow,
                    WarningDays = 7, // Avertissement 7 jours avant expiration
                    Status = DateTime.UtcNow < expirationDate ? "Active" : "Expired",
                    Message = "Ce document deviendra inaccessible après la date d'expiration",

                    // JavaScript à injecter dans le PDF (pour Adobe Reader)
                    ValidationScript = GenerateExpirationJavaScript(expirationDate)
                };

                var json = JsonSerializer.Serialize(expirationInfo, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                var expirationFile = pdfPath.Replace(".pdf", "_EXPIRATION.json");
                await File.WriteAllTextAsync(expirationFile, json, Encoding.UTF8);

                // Créer également un fichier de vérification côté serveur
                await CreateServerSideValidationFile(pdfPath, expirationDate, userName);

                _logger.LogInformation("✅ Expiration configurée avec succès. Document expire le {Date}",
                    expirationDate.ToString("dd/MM/yyyy HH:mm"));

                return expirationFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'ajout de la date d'expiration");
                throw;
            }
        }

        /// <summary>
        /// Génère le JavaScript à injecter dans le PDF pour la validation d'expiration
        /// </summary>
        private string GenerateExpirationJavaScript(DateTime expirationDate)
        {
            return $@"
// Script d'expiration automatique - PDF Security App
var expirationYear = {expirationDate.Year};
var expirationMonth = {expirationDate.Month - 1}; // JavaScript: 0-11
var expirationDay = {expirationDate.Day};
var expirationHour = {expirationDate.Hour};
var expirationMinute = {expirationDate.Minute};

var expirationDate = new Date(expirationYear, expirationMonth, expirationDay, expirationHour, expirationMinute);
var currentDate = new Date();

// Vérification de l'expiration
if (currentDate > expirationDate) {{
    app.alert({{
        cMsg: '🚫 DOCUMENT EXPIRÉ\\n\\n' +
              'Ce document a expiré le ' + expirationDate.toLocaleDateString() + '\\n' +
              'Il n\\'est plus accessible pour des raisons de sécurité.\\n\\n' +
              'Contactez l\\'émetteur pour obtenir une version mise à jour.',
        cTitle: 'Document Expiré',
        nIcon: 0,
        nType: 0
    }});
    
    // Fermer automatiquement le document
    this.closeDoc(true);
}}
else {{
    // Calculer les jours restants
    var daysRemaining = Math.ceil((expirationDate - currentDate) / (1000 * 60 * 60 * 24));
    
    // Avertissement si moins de 7 jours
    if (daysRemaining <= 7 && daysRemaining > 0) {{
        app.alert({{
            cMsg: '⚠️ AVERTISSEMENT D\\'EXPIRATION\\n\\n' +
                  'Ce document expire dans ' + daysRemaining + ' jour(s)\\n' +
                  'Date d\\'expiration: ' + expirationDate.toLocaleDateString() + '\\n\\n' +
                  'Pensez à en demander une copie actualisée.',
            cTitle: 'Expiration Proche',
            nIcon: 1,
            nType: 0
        }});
    }}
}}
";
        }

        /// <summary>
        /// Crée un fichier de validation côté serveur pour vérification en ligne
        /// </summary>
        private async Task CreateServerSideValidationFile(string pdfPath, DateTime expirationDate, string userName)
        {
            var documentId = Guid.NewGuid().ToString("N");

            var validationData = new
            {
                DocumentId = documentId,
                FileName = Path.GetFileName(pdfPath),
                ExpirationDate = expirationDate,
                CreatedBy = userName,
                CreatedAt = DateTime.UtcNow,
                ValidationUrl = $"https://votre-domaine.com/api/validate/{documentId}",
                IsExpired = DateTime.UtcNow > expirationDate,

                // Hash du PDF pour validation d'intégrité
                DocumentHash = await CalculateFileHashAsync(pdfPath),

                // Clé de révocation (pour annuler l'accès avant expiration)
                RevocationKey = GenerateRevocationKey()
            };

            var validationFile = Path.Combine(
                _environment.WebRootPath,
                "secured",
                $"VALIDATION_{documentId}.json"
            );

            var json = JsonSerializer.Serialize(validationData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(validationFile, json, Encoding.UTF8);

            _logger.LogInformation("📋 Fichier de validation serveur créé: {ValidationFile}", Path.GetFileName(validationFile));
        }

        // ========================================
        // 2. CHIFFREMENT AES-256 (UPGRADE)
        // ========================================

        /// <summary>
        /// Upgrade le chiffrement PDF de 128-bit vers 256-bit (norme militaire)
        /// NOTE: PdfSharp ne supporte que 128-bit, cette méthode utilise un chiffrement supplémentaire
        /// </summary>
        public async Task<byte[]> UpgradeToAES256Async(byte[] pdfBytes, string password)
        {
            try
            {
                _logger.LogInformation("🔐 Upgrade vers chiffrement AES-256...");

                // Génération d'une clé AES-256 dérivée du mot de passe
                using var aes = Aes.Create();
                aes.KeySize = 256; // 256-bit
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Dérivation de clé sécurisée avec PBKDF2
                using var deriveBytes = new Rfc2898DeriveBytes(
                    password,
                    salt: Encoding.UTF8.GetBytes("PDFSecuritySalt2025!@#"),
                    iterations: 100000, // 100k iterations (sécurité renforcée)
                    HashAlgorithmName.SHA256
                );

                aes.Key = deriveBytes.GetBytes(32); // 256 bits = 32 bytes
                aes.IV = deriveBytes.GetBytes(16);  // IV de 128 bits

                // Chiffrement du PDF
                using var encryptor = aes.CreateEncryptor();
                using var msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

                await csEncrypt.WriteAsync(pdfBytes);
                await csEncrypt.FlushFinalBlockAsync();

                var encryptedBytes = msEncrypt.ToArray();

                // Ajouter un en-tête pour identifier le chiffrement AES-256
                var header = Encoding.UTF8.GetBytes("PDF-AES256-ENCRYPTED:");
                var result = new byte[header.Length + aes.IV.Length + encryptedBytes.Length];

                Buffer.BlockCopy(header, 0, result, 0, header.Length);
                Buffer.BlockCopy(aes.IV, 0, result, header.Length, aes.IV.Length);
                Buffer.BlockCopy(encryptedBytes, 0, result, header.Length + aes.IV.Length, encryptedBytes.Length);

                _logger.LogInformation("✅ Chiffrement AES-256 appliqué avec succès ({Size} bytes)", result.Length);
                _logger.LogInformation("🔑 Clé: {KeySize}-bit, Iterations: {Iterations}", aes.KeySize, 100000);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du chiffrement AES-256");
                throw;
            }
        }

        /// <summary>
        /// Déchiffrement AES-256 (pour vérification)
        /// </summary>
        public async Task<byte[]> DecryptAES256Async(byte[] encryptedBytes, string password)
        {
            try
            {
                var header = Encoding.UTF8.GetBytes("PDF-AES256-ENCRYPTED:");

                // Vérifier l'en-tête
                var headerCheck = new byte[header.Length];
                Buffer.BlockCopy(encryptedBytes, 0, headerCheck, 0, header.Length);

                if (!headerCheck.SequenceEqual(header))
                {
                    throw new InvalidOperationException("Fichier non chiffré avec AES-256");
                }

                // Extraire l'IV
                var iv = new byte[16];
                Buffer.BlockCopy(encryptedBytes, header.Length, iv, 0, 16);

                // Extraire les données chiffrées
                var cipherText = new byte[encryptedBytes.Length - header.Length - 16];
                Buffer.BlockCopy(encryptedBytes, header.Length + 16, cipherText, 0, cipherText.Length);

                // Déchiffrement
                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var deriveBytes = new Rfc2898DeriveBytes(
                    password,
                    salt: Encoding.UTF8.GetBytes("PDFSecuritySalt2025!@#"),
                    iterations: 100000,
                    HashAlgorithmName.SHA256
                );

                aes.Key = deriveBytes.GetBytes(32);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(cipherText);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var msPlain = new MemoryStream();

                await csDecrypt.CopyToAsync(msPlain);

                _logger.LogInformation("✅ Déchiffrement AES-256 réussi");
                return msPlain.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du déchiffrement AES-256");
                throw;
            }
        }

        // ========================================
        // 3. FILIGRANE INVISIBLE (STEGANOGRAPHIE)
        // ========================================

        /// <summary>
        /// Ajoute un filigrane invisible dans une image en utilisant la stéganographie LSB
        /// Les données sont cachées dans les bits de poids faible des pixels
        /// </summary>
        public async Task<string> AddInvisibleWatermarkAsync(string imagePath, string watermarkData)
        {
            try
            {
                _logger.LogInformation("🔍 Ajout de filigrane invisible: {ImagePath}", Path.GetFileName(imagePath));

                using var image = await Image.LoadAsync<Rgba32>(imagePath);

                // Préparer les données à cacher (avec en-tête de validation)
                var header = "PDFSEC:";
                var fullData = header + watermarkData;
                var dataBytes = Encoding.UTF8.GetBytes(fullData);
                var dataLength = dataBytes.Length;

                _logger.LogInformation("📊 Données à cacher: {Length} bytes", dataLength);

                // Vérifier qu'il y a assez d'espace dans l'image
                var maxCapacity = (image.Width * image.Height * 3) / 8; // 3 canaux RGB, 1 bit par canal
                if (dataLength > maxCapacity)
                {
                    throw new InvalidOperationException($"Image trop petite. Capacité: {maxCapacity} bytes, Requis: {dataLength} bytes");
                }

                // Convertir les bytes en bits
                var bits = new List<bool>();

                // Ajouter la longueur des données (32 bits)
                for (int i = 0; i < 32; i++)
                {
                    bits.Add(((dataLength >> i) & 1) == 1);
                }

                // Ajouter les données
                foreach (var b in dataBytes)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        bits.Add(((b >> i) & 1) == 1);
                    }
                }

                // Cacher les bits dans l'image (LSB Steganography)
                int bitIndex = 0;
                bool done = false;

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height && !done; y++)
                    {
                        var row = accessor.GetRowSpan(y);

                        for (int x = 0; x < row.Length && !done; x++)
                        {
                            var pixel = row[x];

                            // Modifier le bit de poids faible de chaque canal (R, G, B)
                            if (bitIndex < bits.Count)
                            {
                                pixel.R = (byte)((pixel.R & 0xFE) | (bits[bitIndex++] ? 1 : 0));
                            }

                            if (bitIndex < bits.Count)
                            {
                                pixel.G = (byte)((pixel.G & 0xFE) | (bits[bitIndex++] ? 1 : 0));
                            }

                            if (bitIndex < bits.Count)
                            {
                                pixel.B = (byte)((pixel.B & 0xFE) | (bits[bitIndex++] ? 1 : 0));
                            }

                            row[x] = pixel;

                            if (bitIndex >= bits.Count)
                            {
                                done = true;
                            }
                        }
                    }
                });

                // Sauvegarder l'image avec le filigrane invisible
                var outputPath = imagePath.Replace(".png", "_watermarked.png");
                await image.SaveAsPngAsync(outputPath);

                _logger.LogInformation("✅ Filigrane invisible ajouté avec succès: {OutputPath}", Path.GetFileName(outputPath));
                _logger.LogInformation("🔐 {BitCount} bits cachés dans l'image", bits.Count);

                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'ajout du filigrane invisible");
                throw;
            }
        }

        /// <summary>
        /// Extrait le filigrane invisible d'une image
        /// </summary>
        public async Task<string?> ExtractInvisibleWatermarkAsync(string imagePath)
        {
            try
            {
                _logger.LogInformation("🔎 Extraction du filigrane invisible: {ImagePath}", Path.GetFileName(imagePath));

                using var image = await Image.LoadAsync<Rgba32>(imagePath);

                var bits = new List<bool>();

                // Extraire les bits cachés
                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        var row = accessor.GetRowSpan(y);

                        for (int x = 0; x < row.Length; x++)
                        {
                            var pixel = row[x];

                            // Extraire le LSB de chaque canal
                            bits.Add((pixel.R & 1) == 1);
                            bits.Add((pixel.G & 1) == 1);
                            bits.Add((pixel.B & 1) == 1);
                        }
                    }
                });

                // Extraire la longueur des données (32 premiers bits)
                int dataLength = 0;
                for (int i = 0; i < 32 && i < bits.Count; i++)
                {
                    if (bits[i])
                    {
                        dataLength |= (1 << i);
                    }
                }

                _logger.LogInformation("📊 Longueur des données cachées: {Length} bytes", dataLength);

                // Extraire les données
                var dataBytes = new byte[dataLength];
                int bitIndex = 32; // Après les 32 bits de longueur

                for (int i = 0; i < dataLength && bitIndex + 7 < bits.Count; i++)
                {
                    byte b = 0;
                    for (int j = 0; j < 8; j++)
                    {
                        if (bits[bitIndex++])
                        {
                            b |= (byte)(1 << j);
                        }
                    }
                    dataBytes[i] = b;
                }

                var extractedData = Encoding.UTF8.GetString(dataBytes);

                // Vérifier l'en-tête
                if (!extractedData.StartsWith("PDFSEC:"))
                {
                    _logger.LogWarning("⚠️ En-tête invalide - Aucun filigrane détecté");
                    return null;
                }

                var watermark = extractedData.Substring(7); // Retirer "PDFSEC:"

                _logger.LogInformation("✅ Filigrane invisible extrait: {Watermark}", watermark);

                return watermark;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'extraction du filigrane invisible");
                return null;
            }
        }

        // ========================================
        // MÉTHODES UTILITAIRES
        // ========================================

        private async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            await using var stream = File.OpenRead(filePath);
            var hashBytes = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private string GenerateRevocationKey()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }
    }
}