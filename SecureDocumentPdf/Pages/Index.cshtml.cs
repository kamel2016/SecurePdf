//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using SecureDocumentPdf.Models;
//using SecureDocumentPdf.Services;
//using SecureDocumentPdf.Services.Interface;
//using System.ComponentModel.DataAnnotations;

//namespace SecureDocumentPdf.Pages
//{
//    /// <summary>
//    /// PageModel pour la page principale d'upload et de traitement PDF
//    /// Gère toute la logique métier et les interactions utilisateur
//    /// </summary>
//    public class IndexModel : PageModel
//    {
//        private readonly IPdfSecurityService _pdfSecurityService;
//        private readonly ILogger<IndexModel> _logger;

//        /// <summary>
//        /// Constructeur avec injection de dépendances
//        /// </summary>
//        public IndexModel(IPdfSecurityService pdfSecurityService, ILogger<IndexModel> logger)
//        {
//            _pdfSecurityService = pdfSecurityService;
//            _logger = logger;
//        }

//        /// <summary>
//        /// Nom de l'utilisateur effectuant l'upload
//        /// Propriété liée au formulaire (binding bidirectionnel)
//        /// </summary>
//        [BindProperty]
//        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
//        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
//        [Display(Name = "Nom d'utilisateur")]
//        public string UserName { get; set; } = string.Empty;

//        /// <summary>
//        /// Fichier PDF uploadé par l'utilisateur
//        /// Propriété liée au formulaire
//        /// </summary>
//        [BindProperty]
//        [Required(ErrorMessage = "Veuillez sélectionner un fichier PDF")]
//        [Display(Name = "Fichier PDF")]
//        public IFormFile? PdfFile { get; set; }

//        /// <summary>
//        /// Résultat du traitement PDF
//        /// Affiché dans la vue après traitement
//        /// </summary>
//        public UploadResult? Result { get; set; }

//        /// <summary>
//        /// Message de succès temporaire (TempData)
//        /// </summary>
//        [TempData]
//        public string? SuccessMessage { get; set; }

//        /// <summary>
//        /// Message d'erreur temporaire (TempData)
//        /// </summary>
//        [TempData]
//        public string? ErrorMessage { get; set; }

//        /// <summary>
//        /// Handler GET - Affichage initial de la page
//        /// Appelé quand l'utilisateur accède à la page
//        /// </summary>
//        public void OnGet()
//        {
//            _logger.LogInformation("Page Index chargée - Affichage du formulaire d'upload");
//        }

//        /// <summary>
//        /// Handler POST - Traitement du formulaire lors de la soumission
//        /// Appelé quand l'utilisateur soumet le formulaire
//        /// </summary>
//        public async Task<IActionResult> OnPostAsync()
//        {
//            try
//            {
//                _logger.LogInformation("Soumission du formulaire - Utilisateur: {UserName}", UserName);

//                // 1. Validation automatique du modèle via DataAnnotations
//                if (!ModelState.IsValid)
//                {
//                    _logger.LogWarning("Validation du modèle échouée");
//                    return Page();
//                }

//                // 2. Validation supplémentaire du fichier uploadé
//                if (PdfFile == null || PdfFile.Length == 0)
//                {
//                    ModelState.AddModelError(nameof(PdfFile), "Le fichier PDF est requis");
//                    _logger.LogWarning("Aucun fichier uploadé");
//                    return Page();
//                }

//                // 3. Validation du type MIME
//                if (!PdfFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) &&
//                    !PdfFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
//                {
//                    ModelState.AddModelError(nameof(PdfFile), "Seuls les fichiers PDF sont acceptés");
//                    _logger.LogWarning("Type de fichier invalide: {ContentType}", PdfFile.ContentType);
//                    return Page();
//                }

//                // 4. Validation de la taille du fichier (50MB max)
//                const long maxFileSize = 50_000_000; // 50MB
//                if (PdfFile.Length > maxFileSize)
//                {
//                    ModelState.AddModelError(nameof(PdfFile),
//                        $"Le fichier est trop volumineux (max {maxFileSize / 1_000_000}MB)");
//                    _logger.LogWarning("Fichier trop volumineux: {Size} bytes", PdfFile.Length);
//                    return Page();
//                }

//                // 5. Log du début du traitement
//                _logger.LogInformation(
//                    "Début du traitement PDF - Utilisateur: {UserName}, Fichier: {FileName}, Taille: {Size} bytes",
//                    UserName, PdfFile.FileName, PdfFile.Length);

//                // 6. Appel du service de traitement PDF
//                Result = await _pdfSecurityService.ProcessPdfAsync(PdfFile, UserName);

//                // 7. Gestion du résultat
//                if (Result.Success)
//                {
//                    _logger.LogInformation(
//                        "PDF traité avec succès - Utilisateur: {UserName}, Fichier sécurisé: {SecuredPath}",
//                        UserName, Result.SecuredPdfPath);

//                    SuccessMessage = "PDF sécurisé avec succès !";
//                }
//                else
//                {
//                    _logger.LogWarning(
//                        "Échec du traitement PDF - Utilisateur: {UserName}, Erreur: {Error}",
//                        UserName, Result.Message);

//                    ErrorMessage = Result.Message;
//                }

//                return Page();
//            }
//            catch (Exception ex)
//            {
//                // Gestion globale des erreurs
//                _logger.LogError(ex,
//                    "Erreur critique lors du traitement du PDF - Utilisateur: {UserName}, Fichier: {FileName}",
//                    UserName, PdfFile?.FileName ?? "Inconnu");

//                Result = new UploadResult
//                {
//                    Success = false,
//                    Message = "Une erreur inattendue s'est produite lors du traitement",
//                    ErrorDetails = ex.Message
//                };

//                ErrorMessage = "Une erreur inattendue s'est produite";
//                return Page();
//            }
//        }

//        /// <summary>
//        /// Handler POST AJAX - Pour traitement asynchrone via JavaScript
//        /// Retourne du JSON au lieu d'une page HTML
//        /// </summary>
//        public async Task<IActionResult> OnPostUploadAsync()
//        {
//            try
//            {
//                _logger.LogInformation("Requête AJAX reçue - Utilisateur: {UserName}", UserName);

//                // Validation du modèle
//                if (!ModelState.IsValid || PdfFile == null)
//                {
//                    _logger.LogWarning("Validation AJAX échouée");
//                    return new JsonResult(new
//                    {
//                        success = false,
//                        message = "Données invalides",
//                        errors = ModelState.Values
//                            .SelectMany(v => v.Errors)
//                            .Select(e => e.ErrorMessage)
//                            .ToList()
//                    });
//                }

//                // Traitement du PDF
//                var result = await _pdfSecurityService.ProcessPdfAsync(PdfFile, UserName);

//                // Retour JSON pour AJAX
//                return new JsonResult(new
//                {
//                    success = result.Success,
//                    message = result.Message,
//                    securedPdfUrl = result.SecuredPdfPath,
//                    proofFileUrl = result.ProofFilePath,
//                    steps = result.ProcessingSteps,
//                    originalHash = result.OriginalHash,
//                    processedHash = result.ProcessedHash,
//                    processedAt = result.ProcessedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"),
//                    fileSizeBytes = result.FileSizeBytes,
//                    errorDetails = result.ErrorDetails
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Erreur AJAX lors du traitement PDF");
//                return new JsonResult(new
//                {
//                    success = false,
//                    message = "Erreur serveur",
//                    errorDetails = ex.Message
//                });
//            }
//        }

//        /// <summary>
//        /// Handler GET - Téléchargement du PDF sécurisé
//        /// Permet de télécharger directement via une route
//        /// </summary>
//        public IActionResult OnGetDownloadPdf(string fileName)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(fileName))
//                {
//                    return NotFound();
//                }

//                var filePath = Path.Combine(
//                    Directory.GetCurrentDirectory(),
//                    "wwwroot",
//                    "secured",
//                    fileName);

//                if (!System.IO.File.Exists(filePath))
//                {
//                    _logger.LogWarning("Fichier non trouvé: {FilePath}", filePath);
//                    return NotFound();
//                }

//                var fileBytes = System.IO.File.ReadAllBytes(filePath);
//                _logger.LogInformation("Téléchargement du PDF: {FileName}", fileName);

//                return File(fileBytes, "application/pdf", fileName);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Erreur lors du téléchargement: {FileName}", fileName);
//                return StatusCode(500);
//            }
//        }

//        /// <summary>
//        /// Handler GET - Téléchargement du fichier de preuve JSON
//        /// </summary>
//        public IActionResult OnGetDownloadProof(string fileName)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(fileName))
//                {
//                    return NotFound();
//                }

//                var filePath = Path.Combine(
//                    Directory.GetCurrentDirectory(),
//                    "wwwroot",
//                    "secured",
//                    fileName);

//                if (!System.IO.File.Exists(filePath))
//                {
//                    _logger.LogWarning("Fichier de preuve non trouvé: {FilePath}", filePath);
//                    return NotFound();
//                }

//                var fileBytes = System.IO.File.ReadAllBytes(filePath);
//                _logger.LogInformation("Téléchargement du fichier de preuve: {FileName}", fileName);

//                return File(fileBytes, "application/json", fileName);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Erreur lors du téléchargement de la preuve: {FileName}", fileName);
//                return StatusCode(500);
//            }
//        }

//        /// <summary>
//        /// Handler GET - Vérifier le statut d'un traitement (pour polling AJAX)
//        /// </summary>
//        public IActionResult OnGetStatus(string requestId)
//        {
//            try
//            {
//                // Ici vous pourriez implémenter un système de cache/session
//                // pour suivre l'état des traitements en cours

//                return new JsonResult(new
//                {
//                    status = "completed",
//                    message = "Traitement terminé"
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Erreur lors de la vérification du statut: {RequestId}", requestId);
//                return StatusCode(500);
//            }
//        }

//        /// <summary>
//        /// Méthode privée pour valider les extensions de fichier
//        /// </summary>
//        private bool IsValidPdfExtension(string fileName)
//        {
//            var allowedExtensions = new[] { ".pdf" };
//            var extension = Path.GetExtension(fileName).ToLowerInvariant();
//            return allowedExtensions.Contains(extension);
//        }

//        /// <summary>
//        /// Méthode privée pour nettoyer le nom de fichier (sécurité)
//        /// </summary>
//        private string SanitizeFileName(string fileName)
//        {
//            // Suppression des caractères dangereux
//            var invalidChars = Path.GetInvalidFileNameChars();
//            var sanitized = string.Join("_", fileName.Split(invalidChars));
//            return sanitized;
//        }
//    }
//}

//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using SecureDocumentPdf.Models;
//using SecureDocumentPdf.Services.Interface;
//using SecureDocumentPdf.Actions;
//using System.ComponentModel.DataAnnotations;
//using SecureDocumentPdf.Actions;
//using SecureDocumentPdf.Services;

//namespace SecureDocumentPdf.Pages
//{
//    /// <summary>
//    /// PageModel pour la page principale d'upload et de traitement multi-formats
//    /// Supporte PDF, Word, Excel, PowerPoint, Images, TXT, CSV, MD, HTML, etc.
//    /// </summary>
//    public class IndexModel : PageModel
//    {
//        private readonly IPdfSecurityService _pdfSecurityService;
//        private readonly ILogger<IndexModel> _logger;
//        private readonly IWebHostEnvironment _environment;

//        public IndexModel(
//            IPdfSecurityService pdfSecurityService,
//            ILogger<IndexModel> logger,
//            IWebHostEnvironment environment)
//        {
//            _pdfSecurityService = pdfSecurityService;
//            _logger = logger;
//            _environment = environment;
//        }

//        [BindProperty]
//        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
//        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
//        [Display(Name = "Nom d'utilisateur")]
//        public string UserName { get; set; } = string.Empty;

//        [BindProperty]
//        [Required(ErrorMessage = "Veuillez sélectionner un fichier")]
//        [Display(Name = "Fichier à sécuriser")]
//        public IFormFile? UploadedFile { get; set; }

//        [BindProperty]
//        [Display(Name = "Jours avant expiration (0 = jamais)")]
//        [Range(0, 3650, ErrorMessage = "Entre 0 et 3650 jours")]
//        public int ExpirationDays { get; set; } = 0;

//        [BindProperty]
//        [Display(Name = "Activer sécurité maximale")]
//        public bool EnableMaximumSecurity { get; set; } = true;

//        [BindProperty]
//        [Display(Name = "Nécessite plusieurs signatures")]
//        public bool RequireMultipleSignatures { get; set; } = false;

//        [BindProperty]
//        [Display(Name = "Signataires requis (séparés par des virgules)")]
//        public string RequiredSigners { get; set; } = string.Empty;

//        public UploadResult? Result { get; set; }

//        [TempData]
//        public string? SuccessMessage { get; set; }

//        [TempData]
//        public string? ErrorMessage { get; set; }

//        public string? DetectedFileType { get; set; }

//        // Formats supportés
//        private readonly string[] _supportedExtensions = new[]
//        {
//            // Documents Office
//            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
//            // Images
//            ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".svg",
//            // Texte et données
//            ".txt", ".csv", ".json", ".xml",
//            // Markdown et Web
//            ".md", ".markdown", ".html", ".htm",
//            // OpenDocument
//            ".odt", ".ods", ".odp",
//            // Email
//            ".eml", ".msg",
//            // Rich Text
//            ".rtf"
//        };

//        public void OnGet()
//        {
//            _logger.LogInformation(" Page Index chargée - Support multi-formats activé");
//        }

//        public async Task<IActionResult> OnPostAsync()
//        {
//            try
//            {
//                _logger.LogInformation(" Soumission formulaire - Utilisateur: {UserName}", UserName);

//                if (!ModelState.IsValid)
//                {
//                    _logger.LogWarning(" Validation modèle échouée");
//                    return Page();
//                }

//                if (UploadedFile == null || UploadedFile.Length == 0)
//                {
//                    ModelState.AddModelError(nameof(UploadedFile), "Le fichier est requis");
//                    _logger.LogWarning(" Aucun fichier uploadé");
//                    return Page();
//                }

//                // Validation extension
//                var extension = Path.GetExtension(UploadedFile.FileName).ToLowerInvariant();
//                if (!_supportedExtensions.Contains(extension))
//                {
//                    ModelState.AddModelError(nameof(UploadedFile),
//                        $"Format non supporté. Formats acceptés: {string.Join(", ", _supportedExtensions)}");
//                    _logger.LogWarning(" Extension non supportée: {Extension}", extension);
//                    return Page();
//                }

//                DetectedFileType = GetFileTypeDescription(extension);
//                _logger.LogInformation(" Type détecté: {Type}", DetectedFileType);

//                // Validation taille (100MB max pour sécurité maximale)
//                const long maxFileSize = 100_000_000;
//                if (UploadedFile.Length > maxFileSize)
//                {
//                    ModelState.AddModelError(nameof(UploadedFile),
//                        $"Fichier trop volumineux (max {maxFileSize / 1_000_000}MB)");
//                    _logger.LogWarning(" Fichier trop volumineux: {Size} bytes", UploadedFile.Length);
//                    return Page();
//                }

//                _logger.LogInformation(
//                    " Début traitement - Type: {Type}, Taille: {Size} bytes",
//                    DetectedFileType, UploadedFile.Length);

//                // ÉTAPE 1: Conversion vers PDF si nécessaire
//                IFormFile pdfFile = UploadedFile;
//                string? tempPdfPath = null;

//                if (!extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
//                {
//                    _logger.LogInformation(" Conversion vers PDF nécessaire...");

//                    byte[]? pdfBytes = await ConvertToPdfAsync(UploadedFile, extension);

//                    if (pdfBytes == null || pdfBytes.Length == 0)
//                    {
//                        ModelState.AddModelError(nameof(UploadedFile),
//                            "Erreur lors de la conversion en PDF");
//                        _logger.LogError(" Échec conversion PDF");
//                        return Page();
//                    }

//                    // Créer un IFormFile à partir des bytes
//                    var fileName = Path.GetFileNameWithoutExtension(UploadedFile.FileName) + ".pdf";
//                    var stream = new MemoryStream(pdfBytes);
//                    pdfFile = new FormFile(stream, 0, pdfBytes.Length, "file", fileName)
//                    {
//                        Headers = new HeaderDictionary(),
//                        ContentType = "application/pdf"
//                    };

//                    _logger.LogInformation(" Conversion PDF réussie: {FileName}", fileName);
//                }

//                // ÉTAPE 2: Configuration options de sécurité
//                var securityOptions = new SecurityOptions
//                {
//                    ExpirationDays = ExpirationDays,
//                    RequireMultipleSignatures = RequireMultipleSignatures,
//                    RequiredSigners = string.IsNullOrWhiteSpace(RequiredSigners)
//                        ? new List<string>()
//                        : RequiredSigners.Split(',', StringSplitOptions.RemoveEmptyEntries)
//                            .Select(s => s.Trim()).ToList(),
//                    EnableScreenCaptureProtection = EnableMaximumSecurity,
//                    EnablePrintWatermark = EnableMaximumSecurity,
//                    EnableGeolocation = EnableMaximumSecurity,
//                    EnableIpRestriction = EnableMaximumSecurity,
//                    BlockchainHashIterations = EnableMaximumSecurity ? 5 : 3
//                };

//                // ÉTAPE 3: Traitement de sécurisation ultra-protégé
//                _logger.LogInformation(" Lancement sécurisation maximale...");
//                Result = await _pdfSecurityService.ProcessPdfAsync(pdfFile, UserName, securityOptions);

//                if (Result.Success)
//                {
//                    _logger.LogInformation("Traitement réussi - PDF sécurisé: {Path}, Durée: {Duration}s",
//                        Result.SecuredPdfPath, Result.ProcessingDurationSeconds);

//                    SuccessMessage = $" {DetectedFileType} converti et ultra-sécurisé avec succès !";
//                }
//                else
//                {
//                    _logger.LogWarning("Échec traitement: {Message}", Result.Message);
//                    ErrorMessage = Result.Message;
//                }

//                // Nettoyage
//                if (tempPdfPath != null && File.Exists(tempPdfPath))
//                {
//                    File.Delete(tempPdfPath);
//                }

//                return Page();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex,
//                    " Erreur critique - Utilisateur: {UserName}, Fichier: {FileName}",
//                    UserName, UploadedFile?.FileName ?? "Inconnu");

//                Result = new UploadResult
//                {
//                    Success = false,
//                    Message = "Une erreur inattendue s'est produite",
//                    ErrorDetails = ex.Message
//                };

//                ErrorMessage = "Une erreur inattendue s'est produite";
//                return Page();
//            }
//        }

//        /// <summary>
//        /// Convertit un fichier vers PDF selon son extension
//        /// </summary>
//        private async Task<byte[]?> ConvertToPdfAsync(IFormFile file, string extension)
//        {
//            try
//            {
//                _logger.LogInformation(" Conversion {Extension} → PDF", extension);

//                return extension.ToLowerInvariant() switch
//                {
//                    // Documents Office
//                    ".doc" or ".docx" => WordToPdfConverter.ConvertToPdf(file),
//                    ".xls" or ".xlsx" => ExcelToPdfConverter.ConvertToPdf(file),
//                    ".ppt" or ".pptx" => PowerPointToPdfConverter.ConvertToPdf(file),

//                    // Images
//                    ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff"
//                        => ImageToPdfConverter.ConvertToPdf(file),
//                    ".svg" => SvgToPdfConverter.ConvertToPdf(file),

//                    // Texte et données
//                    ".txt" => TextToPdfConverter.ConvertToPdf(file),
//                    ".csv" => CsvToPdfConverter.ConvertToPdf(file),
//                    ".json" => JsonToPdfConverter.ConvertToPdf(file),
//                    ".xml" => XmlToPdfConverter.ConvertToPdf(file),

//                    // Markdown et Web
//                    ".md" or ".markdown" => MarkdownToPdfConverter.ConvertToPdf(file),
//                    ".html" or ".htm" => HtmlToPdfConverter.ConvertToPdf(file),

//                    // Rich Text
//                    ".rtf" => RtfToPdfConverter.ConvertToPdf(file),

//                    // Email
//                    ".eml" => EmailToPdfConverter.ConvertToPdf(file),

//                    // OpenDocument
//                    ".odt" or ".ods" or ".odp" => OpenDocumentToPdfConverter.ConvertToPdf(file),

//                    _ => null
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Erreur conversion {Extension}", extension);
//                return null;
//            }
//        }

//        /// <summary>
//        /// Retourne une description lisible du type de fichier
//        /// </summary>
//        private string GetFileTypeDescription(string extension)
//        {
//            return extension.ToLowerInvariant() switch
//            {
//                ".pdf" => "Document PDF",
//                ".doc" or ".docx" => "Document Word",
//                ".xls" or ".xlsx" => "Tableur Excel",
//                ".ppt" or ".pptx" => "Présentation PowerPoint",
//                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" => "Image",
//                ".svg" => "Image vectorielle SVG",
//                ".txt" => "Fichier texte",
//                ".csv" => "Fichier CSV",
//                ".json" => "Fichier JSON",
//                ".xml" => "Fichier XML",
//                ".md" or ".markdown" => "Document Markdown",
//                ".html" or ".htm" => "Page HTML",
//                ".rtf" => "Document RTF",
//                ".eml" => "Email",
//                ".odt" => "Document OpenDocument",
//                ".ods" => "Tableur OpenDocument",
//                ".odp" => "Présentation OpenDocument",
//                _ => "Fichier"
//            };
//        }

//        public async Task<IActionResult> OnPostUploadAsync()
//        {
//            try
//            {
//                _logger.LogInformation("Requête AJAX - Utilisateur: {UserName}", UserName);

//                if (!ModelState.IsValid || UploadedFile == null)
//                {
//                    return new JsonResult(new
//                    {
//                        success = false,
//                        message = "Données invalides",
//                        errors = ModelState.Values
//                            .SelectMany(v => v.Errors)
//                            .Select(e => e.ErrorMessage)
//                            .ToList()
//                    });
//                }

//                // Même logique que OnPostAsync mais retour JSON
//                var extension = Path.GetExtension(UploadedFile.FileName).ToLowerInvariant();

//                if (!_supportedExtensions.Contains(extension))
//                {
//                    return new JsonResult(new
//                    {
//                        success = false,
//                        message = $"Format non supporté: {extension}"
//                    });
//                }

//                IFormFile pdfFile = UploadedFile;

//                if (!extension.Equals(".pdf"))
//                {
//                    var pdfBytes = await ConvertToPdfAsync(UploadedFile, extension);

//                    if (pdfBytes == null)
//                    {
//                        return new JsonResult(new
//                        {
//                            success = false,
//                            message = "Erreur de conversion en PDF"
//                        });
//                    }

//                    var fileName = Path.GetFileNameWithoutExtension(UploadedFile.FileName) + ".pdf";
//                    var stream = new MemoryStream(pdfBytes);
//                    pdfFile = new FormFile(stream, 0, pdfBytes.Length, "file", fileName)
//                    {
//                        Headers = new HeaderDictionary(),
//                        ContentType = "application/pdf"
//                    };
//                }

//                var securityOptions = new SecurityOptions
//                {
//                    ExpirationDays = ExpirationDays,
//                    RequireMultipleSignatures = RequireMultipleSignatures,
//                    RequiredSigners = string.IsNullOrWhiteSpace(RequiredSigners)
//                        ? new List<string>()
//                        : RequiredSigners.Split(',').Select(s => s.Trim()).ToList(),
//                    EnableScreenCaptureProtection = EnableMaximumSecurity,
//                    EnablePrintWatermark = EnableMaximumSecurity,
//                    EnableGeolocation = EnableMaximumSecurity,
//                    EnableIpRestriction = EnableMaximumSecurity,
//                    BlockchainHashIterations = 5
//                };

//                var result = await _pdfSecurityService.ProcessPdfAsync(pdfFile, UserName, securityOptions);

//                return new JsonResult(new
//                {
//                    success = result.Success,
//                    message = result.Message,
//                    documentId = result.DocumentId,
//                    securedPdfUrl = result.SecuredPdfPath,
//                    proofFileUrl = result.ProofFilePath,
//                    qrCodeUrl = result.QRCodePath,
//                    auditLogUrl = result.AuditLogPath,
//                    biometricSignature = result.BiometricSignature,
//                    steps = result.ProcessingSteps,
//                    originalHash = result.OriginalHash,
//                    processedHash = result.ProcessedHash,
//                    pageHashes = result.PageHashes,
//                    hashChain = result.HashChain,
//                    processedAt = result.ProcessedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"),
//                    fileSizeBytes = result.FileSizeBytes,
//                    processingDuration = result.ProcessingDurationSeconds,
//                    securityLevel = result.SecurityLevel,
//                    ipAddress = result.IpAddress,
//                    expirationDate = result.ExpirationDate?.ToString("yyyy-MM-dd"),
//                    isPasswordProtected = result.IsPasswordProtected,
//                    protectionInfo = result.ProtectionInfo,
//                    errorDetails = result.ErrorDetails
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Erreur AJAX");
//                return new JsonResult(new
//                {
//                    success = false,
//                    message = "Erreur serveur",
//                    errorDetails = ex.Message
//                });
//            }
//        }

//        public IActionResult OnGetDownloadPdf(string fileName)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(fileName))
//                    return NotFound();

//                var filePath = Path.Combine(
//                    Directory.GetCurrentDirectory(),
//                    "wwwroot",
//                    "secured",
//                    fileName);

//                if (!System.IO.File.Exists(filePath))
//                {
//                    _logger.LogWarning("Fichier non trouvé: {FilePath}", filePath);
//                    return NotFound();
//                }

//                var fileBytes = System.IO.File.ReadAllBytes(filePath);
//                _logger.LogInformation("Téléchargement: {FileName}", fileName);

//                return File(fileBytes, "application/pdf", fileName);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Erreur téléchargement: {FileName}", fileName);
//                return StatusCode(500);
//            }
//        }

//        public IActionResult OnGetDownloadProof(string fileName)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(fileName))
//                    return NotFound();

//                var filePath = Path.Combine(
//                    Directory.GetCurrentDirectory(),
//                    "wwwroot",
//                    "secured",
//                    fileName);

//                if (!System.IO.File.Exists(filePath))
//                {
//                    _logger.LogWarning("Preuve non trouvée: {FilePath}", filePath);
//                    return NotFound();
//                }

//                var fileBytes = System.IO.File.ReadAllBytes(filePath);
//                _logger.LogInformation("Téléchargement preuve: {FileName}", fileName);

//                return File(fileBytes, "application/json", fileName);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Erreur téléchargement preuve: {FileName}", fileName);
//                return StatusCode(500);
//            }
//        }

//        public IActionResult OnGetDownloadQRCode(string fileName)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(fileName))
//                    return NotFound();

//                var filePath = Path.Combine(
//                    Directory.GetCurrentDirectory(),
//                    "wwwroot",
//                    "secured",
//                    fileName);

//                if (!System.IO.File.Exists(filePath))
//                    return NotFound();

//                var fileBytes = System.IO.File.ReadAllBytes(filePath);
//                _logger.LogInformation(" Téléchargement QR: {FileName}", fileName);

//                return File(fileBytes, "image/png", fileName);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, " Erreur téléchargement QR: {FileName}", fileName);
//                return StatusCode(500);
//            }
//        }
//    }
//}


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureDocumentPdf.Models;
using SecureDocumentPdf.Services.Interface;
using SecureDocumentPdf.Actions;
using System.ComponentModel.DataAnnotations;
using SecureDocumentPdf.Actions;
using SecureDocumentPdf.Services;

namespace SecureDocumentPdf.Pages
{
    /// <summary>
    /// PageModel pour la page principale d'upload et de traitement multi-formats
    /// Supporte PDF, Word, Excel, PowerPoint, Images, TXT, CSV, MD, HTML, etc.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IPdfSecurityService _pdfSecurityService;
        private readonly ILogger<IndexModel> _logger;
        private readonly IWebHostEnvironment _environment;

        public IndexModel(
            IPdfSecurityService pdfSecurityService,
            ILogger<IndexModel> logger,
            IWebHostEnvironment environment)
        {
            _pdfSecurityService = pdfSecurityService;
            _logger = logger;
            _environment = environment;
        }

        [BindProperty]
        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
        [Display(Name = "Nom d'utilisateur")]
        public string UserName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Veuillez sélectionner un fichier")]
        [Display(Name = "Fichier à sécuriser")]
        public IFormFile? UploadedFile { get; set; }

        [BindProperty]
        [Display(Name = "Jours avant expiration (0 = jamais)")]
        [Range(0, 3650, ErrorMessage = "Entre 0 et 3650 jours")]
        public int ExpirationDays { get; set; } = 0;

        [BindProperty]
        [Display(Name = "Activer sécurité maximale")]
        public bool EnableMaximumSecurity { get; set; } = true;

        [BindProperty]
        [Display(Name = "Nécessite plusieurs signatures")]
        public bool RequireMultipleSignatures { get; set; } = false;

        [BindProperty]
        [Display(Name = "Signataires requis (séparés par des virgules)")]
        public string RequiredSigners { get; set; } = string.Empty;

        public UploadResult? Result { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public string? DetectedFileType { get; set; }

        // Formats supportés
        private readonly string[] _supportedExtensions = new[]
        {
            // Documents Office
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            // Images
            ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".svg",
            // Texte et données
            ".txt", ".csv", ".json", ".xml",
            // Markdown et Web
            ".md", ".markdown", ".html", ".htm",
            // OpenDocument
            ".odt", ".ods", ".odp",
            // Email
            ".eml", ".msg",
            // Rich Text
            ".rtf"
        };

        public void OnGet()
        {
            _logger.LogInformation("📄 Page Index chargée - Support multi-formats activé");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                _logger.LogInformation("📤 Soumission formulaire - Utilisateur: {UserName}", UserName);

                //if (!ModelState.IsValid)
                //{
                //    _logger.LogWarning("⚠️ Validation modèle échouée");
                //    return Page();
                //}

                if (UploadedFile == null || UploadedFile.Length == 0)
                {
                    ModelState.AddModelError(nameof(UploadedFile), "Le fichier est requis");
                    _logger.LogWarning("⚠️ Aucun fichier uploadé");
                    return Page();
                }

                // Validation extension
                var extension = Path.GetExtension(UploadedFile.FileName).ToLowerInvariant();
                if (!_supportedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(nameof(UploadedFile),
                        $"Format non supporté. Formats acceptés: {string.Join(", ", _supportedExtensions)}");
                    _logger.LogWarning("⚠️ Extension non supportée: {Extension}", extension);
                    return Page();
                }

                DetectedFileType = GetFileTypeDescription(extension);
                _logger.LogInformation("📋 Type détecté: {Type}", DetectedFileType);

                // Validation taille (100MB max pour sécurité maximale)
                const long maxFileSize = 100_000_000;
                if (UploadedFile.Length > maxFileSize)
                {
                    ModelState.AddModelError(nameof(UploadedFile),
                        $"Fichier trop volumineux (max {maxFileSize / 1_000_000}MB)");
                    _logger.LogWarning("⚠️ Fichier trop volumineux: {Size} bytes", UploadedFile.Length);
                    return Page();
                }

                _logger.LogInformation(
                    "🔄 Début traitement - Type: {Type}, Taille: {Size} bytes",
                    DetectedFileType, UploadedFile.Length);

                // ÉTAPE 1: Conversion vers PDF si nécessaire
                IFormFile pdfFile = UploadedFile;
                string? tempPdfPath = null;

                if (!extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("🔄 Conversion vers PDF nécessaire...");

                    byte[]? pdfBytes = await ConvertToPdfAsync(UploadedFile, extension);

                    if (pdfBytes == null || pdfBytes.Length == 0)
                    {
                        ModelState.AddModelError(nameof(UploadedFile),
                            "Erreur lors de la conversion en PDF");
                        _logger.LogError("❌ Échec conversion PDF");
                        return Page();
                    }

                    // Créer un IFormFile à partir des bytes
                    var fileName = Path.GetFileNameWithoutExtension(UploadedFile.FileName) + ".pdf";
                    var stream = new MemoryStream(pdfBytes);
                    pdfFile = new FormFile(stream, 0, pdfBytes.Length, "file", fileName)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "application/pdf"
                    };

                    _logger.LogInformation("✅ Conversion PDF réussie: {FileName}", fileName);
                }

                // ÉTAPE 2: Configuration options de sécurité
                var securityOptions = new SecurityOptions
                {
                    ExpirationDays = ExpirationDays,
                    RequireMultipleSignatures = RequireMultipleSignatures,
                    RequiredSigners = string.IsNullOrWhiteSpace(RequiredSigners)
                        ? new List<string>()
                        : RequiredSigners.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).ToList(),
                    EnableScreenCaptureProtection = EnableMaximumSecurity,
                    EnablePrintWatermark = EnableMaximumSecurity,
                    EnableGeolocation = EnableMaximumSecurity,
                    EnableIpRestriction = EnableMaximumSecurity,
                    BlockchainHashIterations = EnableMaximumSecurity ? 5 : 3
                };

                // ÉTAPE 3: Traitement de sécurisation ultra-protégé
                _logger.LogInformation("🔒 Lancement sécurisation maximale...");
                Result = await _pdfSecurityService.ProcessPdfAsync(pdfFile, UserName, securityOptions);

                if (Result.Success)
                {
                    _logger.LogInformation(
                        "✅ Traitement réussi - PDF sécurisé: {Path}, Durée: {Duration}s",
                        Result.SecuredPdfPath, Result.ProcessingDurationSeconds);

                    SuccessMessage = $"✅ {DetectedFileType} converti et ultra-sécurisé avec succès !";
                }
                else
                {
                    _logger.LogWarning("❌ Échec traitement: {Message}", Result.Message);
                    ErrorMessage = Result.Message;
                }

                // Nettoyage
                if (tempPdfPath != null && System.IO.File.Exists(tempPdfPath))
                {
                    System.IO.File.Delete(tempPdfPath);
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Erreur critique - Utilisateur: {UserName}, Fichier: {FileName}",
                    UserName, UploadedFile?.FileName ?? "Inconnu");

                Result = new UploadResult
                {
                    Success = false,
                    Message = "Une erreur inattendue s'est produite",
                    ErrorDetails = ex.Message
                };

                ErrorMessage = "Une erreur inattendue s'est produite";
                return Page();
            }
        }

        /// <summary>
        /// Convertit un fichier vers PDF selon son extension
        /// </summary>
        private async Task<byte[]?> ConvertToPdfAsync(IFormFile file, string extension)
        {
            try
            {
                _logger.LogInformation("🔄 Conversion {Extension} → PDF", extension);

                return extension.ToLowerInvariant() switch
                {
                    // Documents Office
                    ".doc" or ".docx" => WordToPdfConverter.ConvertToPdf(file),
                    ".xls" or ".xlsx" => ExcelToPdfConverter.ConvertToPdf(file),
                    ".ppt" or ".pptx" => PowerPointToPdfConverter.ConvertToPdf(file),

                    // Images
                    ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff"
                        => ImageToPdfConverter.ConvertToPdf(file),
                    ".svg" => SvgToPdfConverter.ConvertToPdf(file),

                    // Texte et données
                    ".txt" => TextToPdfConverter.ConvertToPdf(file),
                    ".csv" => CsvToPdfConverter.ConvertToPdf(file),
                    ".json" => JsonToPdfConverter.ConvertToPdf(file),
                    ".xml" => XmlToPdfConverter.ConvertToPdf(file),

                    // Markdown et Web
                    ".md" or ".markdown" => MarkdownToPdfConverter.ConvertToPdf(file),
                    ".html" or ".htm" => HtmlToPdfConverter.ConvertToPdf(file),

                    // Rich Text
                    ".rtf" => RtfToPdfConverter.ConvertToPdf(file),

                    // Email
                    ".eml" => EmailToPdfConverter.ConvertToPdf(file),

                    // OpenDocument
                    ".odt" or ".ods" or ".odp" => OpenDocumentToPdfConverter.ConvertToPdf(file),

                    _ => null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur conversion {Extension}", extension);
                return null;
            }
        }

        /// <summary>
        /// Retourne une description lisible du type de fichier
        /// </summary>
        private string GetFileTypeDescription(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => "Document PDF",
                ".doc" or ".docx" => "Document Word",
                ".xls" or ".xlsx" => "Tableur Excel",
                ".ppt" or ".pptx" => "Présentation PowerPoint",
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" => "Image",
                ".svg" => "Image vectorielle SVG",
                ".txt" => "Fichier texte",
                ".csv" => "Fichier CSV",
                ".json" => "Fichier JSON",
                ".xml" => "Fichier XML",
                ".md" or ".markdown" => "Document Markdown",
                ".html" or ".htm" => "Page HTML",
                ".rtf" => "Document RTF",
                ".eml" => "Email",
                ".odt" => "Document OpenDocument",
                ".ods" => "Tableur OpenDocument",
                ".odp" => "Présentation OpenDocument",
                _ => "Fichier"
            };
        }

        public async Task<IActionResult> OnPostUploadAsync()
        {
            try
            {
                _logger.LogInformation("🔄 Requête AJAX - Utilisateur: {UserName}", UserName);

                if (!ModelState.IsValid || UploadedFile == null)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Données invalides",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                // Même logique que OnPostAsync mais retour JSON
                var extension = Path.GetExtension(UploadedFile.FileName).ToLowerInvariant();

                if (!_supportedExtensions.Contains(extension))
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = $"Format non supporté: {extension}"
                    });
                }

                IFormFile pdfFile = UploadedFile;

                if (!extension.Equals(".pdf"))
                {
                    var pdfBytes = await ConvertToPdfAsync(UploadedFile, extension);

                    if (pdfBytes == null)
                    {
                        return new JsonResult(new
                        {
                            success = false,
                            message = "Erreur de conversion en PDF"
                        });
                    }

                    var fileName = Path.GetFileNameWithoutExtension(UploadedFile.FileName) + ".pdf";
                    var stream = new MemoryStream(pdfBytes);
                    pdfFile = new FormFile(stream, 0, pdfBytes.Length, "file", fileName)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "application/pdf"
                    };
                }

                var securityOptions = new SecurityOptions
                {
                    ExpirationDays = ExpirationDays,
                    RequireMultipleSignatures = RequireMultipleSignatures,
                    RequiredSigners = string.IsNullOrWhiteSpace(RequiredSigners)
                        ? new List<string>()
                        : RequiredSigners.Split(',').Select(s => s.Trim()).ToList(),
                    EnableScreenCaptureProtection = EnableMaximumSecurity,
                    EnablePrintWatermark = EnableMaximumSecurity,
                    EnableGeolocation = EnableMaximumSecurity,
                    EnableIpRestriction = EnableMaximumSecurity,
                    BlockchainHashIterations = 5
                };

                var result = await _pdfSecurityService.ProcessPdfAsync(pdfFile, UserName, securityOptions);

                return new JsonResult(new
                {
                    success = result.Success,
                    message = result.Message,
                    documentId = result.DocumentId,
                    securedPdfUrl = result.SecuredPdfPath,
                    proofFileUrl = result.ProofFilePath,
                    qrCodeUrl = result.QRCodePath,
                    auditLogUrl = result.AuditLogPath,
                    biometricSignature = result.BiometricSignature,
                    steps = result.ProcessingSteps,
                    originalHash = result.OriginalHash,
                    processedHash = result.ProcessedHash,
                    pageHashes = result.PageHashes,
                    hashChain = result.HashChain,
                    processedAt = result.ProcessedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    fileSizeBytes = result.FileSizeBytes,
                    processingDuration = result.ProcessingDurationSeconds,
                    securityLevel = result.SecurityLevel,
                    ipAddress = result.IpAddress,
                    expirationDate = result.ExpirationDate?.ToString("yyyy-MM-dd"),
                    isPasswordProtected = result.IsPasswordProtected,
                    protectionInfo = result.ProtectionInfo,
                    errorDetails = result.ErrorDetails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur AJAX");
                return new JsonResult(new
                {
                    success = false,
                    message = "Erreur serveur",
                    errorDetails = ex.Message
                });
            }
        }

        public IActionResult OnGetDownloadPdf(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return NotFound();

                var filePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "secured",
                    fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("❌ Fichier non trouvé: {FilePath}", filePath);
                    return NotFound();
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                _logger.LogInformation("📥 Téléchargement: {FileName}", fileName);

                return this.File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur téléchargement: {FileName}", fileName);
                return StatusCode(500);
            }
        }

        public IActionResult OnGetDownloadProof(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return NotFound();

                var filePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "secured",
                    fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("❌ Preuve non trouvée: {FilePath}", filePath);
                    return NotFound();
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                _logger.LogInformation("📥 Téléchargement preuve: {FileName}", fileName);

                return this.File(fileBytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur téléchargement preuve: {FileName}", fileName);
                return StatusCode(500);
            }
        }

        public IActionResult OnGetDownloadQRCode(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return NotFound();

                var filePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "secured",
                    fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("❌ QR Code non trouvé: {FilePath}", filePath);
                    return NotFound();
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                _logger.LogInformation("📥 Téléchargement QR: {FileName}", fileName);

                return this.File(fileBytes, "image/png", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur téléchargement QR: {FileName}", fileName);
                return StatusCode(500);
            }
        }
    }
}