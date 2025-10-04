using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureDocumentPdf.Models;
using SecureDocumentPdf.Services;
using SecureDocumentPdf.Services.Interface;
using System.ComponentModel.DataAnnotations;

namespace SecureDocumentPdf.Pages
{
    /// <summary>
    /// PageModel pour la page principale d'upload et de traitement PDF
    /// G�re toute la logique m�tier et les interactions utilisateur
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IPdfSecurityService _pdfSecurityService;
        private readonly ILogger<IndexModel> _logger;

        /// <summary>
        /// Constructeur avec injection de d�pendances
        /// </summary>
        public IndexModel(IPdfSecurityService pdfSecurityService, ILogger<IndexModel> logger)
        {
            _pdfSecurityService = pdfSecurityService;
            _logger = logger;
        }

        /// <summary>
        /// Nom de l'utilisateur effectuant l'upload
        /// Propri�t� li�e au formulaire (binding bidirectionnel)
        /// </summary>
        [BindProperty]
        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caract�res")]
        [Display(Name = "Nom d'utilisateur")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Fichier PDF upload� par l'utilisateur
        /// Propri�t� li�e au formulaire
        /// </summary>
        [BindProperty]
        [Required(ErrorMessage = "Veuillez s�lectionner un fichier PDF")]
        [Display(Name = "Fichier PDF")]
        public IFormFile? PdfFile { get; set; }

        /// <summary>
        /// R�sultat du traitement PDF
        /// Affich� dans la vue apr�s traitement
        /// </summary>
        public UploadResult? Result { get; set; }

        /// <summary>
        /// Message de succ�s temporaire (TempData)
        /// </summary>
        [TempData]
        public string? SuccessMessage { get; set; }

        /// <summary>
        /// Message d'erreur temporaire (TempData)
        /// </summary>
        [TempData]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Handler GET - Affichage initial de la page
        /// Appel� quand l'utilisateur acc�de � la page
        /// </summary>
        public void OnGet()
        {
            _logger.LogInformation("Page Index charg�e - Affichage du formulaire d'upload");
        }

        /// <summary>
        /// Handler POST - Traitement du formulaire lors de la soumission
        /// Appel� quand l'utilisateur soumet le formulaire
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                _logger.LogInformation("Soumission du formulaire - Utilisateur: {UserName}", UserName);

                // 1. Validation automatique du mod�le via DataAnnotations
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Validation du mod�le �chou�e");
                    return Page();
                }

                // 2. Validation suppl�mentaire du fichier upload�
                if (PdfFile == null || PdfFile.Length == 0)
                {
                    ModelState.AddModelError(nameof(PdfFile), "Le fichier PDF est requis");
                    _logger.LogWarning("Aucun fichier upload�");
                    return Page();
                }

                // 3. Validation du type MIME
                if (!PdfFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) &&
                    !PdfFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(nameof(PdfFile), "Seuls les fichiers PDF sont accept�s");
                    _logger.LogWarning("Type de fichier invalide: {ContentType}", PdfFile.ContentType);
                    return Page();
                }

                // 4. Validation de la taille du fichier (50MB max)
                const long maxFileSize = 50_000_000; // 50MB
                if (PdfFile.Length > maxFileSize)
                {
                    ModelState.AddModelError(nameof(PdfFile),
                        $"Le fichier est trop volumineux (max {maxFileSize / 1_000_000}MB)");
                    _logger.LogWarning("Fichier trop volumineux: {Size} bytes", PdfFile.Length);
                    return Page();
                }

                // 5. Log du d�but du traitement
                _logger.LogInformation(
                    "D�but du traitement PDF - Utilisateur: {UserName}, Fichier: {FileName}, Taille: {Size} bytes",
                    UserName, PdfFile.FileName, PdfFile.Length);

                // 6. Appel du service de traitement PDF
                Result = await _pdfSecurityService.ProcessPdfAsync(PdfFile, UserName);

                // 7. Gestion du r�sultat
                if (Result.Success)
                {
                    _logger.LogInformation(
                        "PDF trait� avec succ�s - Utilisateur: {UserName}, Fichier s�curis�: {SecuredPath}",
                        UserName, Result.SecuredPdfPath);

                    SuccessMessage = "PDF s�curis� avec succ�s !";
                }
                else
                {
                    _logger.LogWarning(
                        "�chec du traitement PDF - Utilisateur: {UserName}, Erreur: {Error}",
                        UserName, Result.Message);

                    ErrorMessage = Result.Message;
                }

                return Page();
            }
            catch (Exception ex)
            {
                // Gestion globale des erreurs
                _logger.LogError(ex,
                    "Erreur critique lors du traitement du PDF - Utilisateur: {UserName}, Fichier: {FileName}",
                    UserName, PdfFile?.FileName ?? "Inconnu");

                Result = new UploadResult
                {
                    Success = false,
                    Message = "Une erreur inattendue s'est produite lors du traitement",
                    ErrorDetails = ex.Message
                };

                ErrorMessage = "Une erreur inattendue s'est produite";
                return Page();
            }
        }

        /// <summary>
        /// Handler POST AJAX - Pour traitement asynchrone via JavaScript
        /// Retourne du JSON au lieu d'une page HTML
        /// </summary>
        public async Task<IActionResult> OnPostUploadAsync()
        {
            try
            {
                _logger.LogInformation("Requ�te AJAX re�ue - Utilisateur: {UserName}", UserName);

                // Validation du mod�le
                if (!ModelState.IsValid || PdfFile == null)
                {
                    _logger.LogWarning("Validation AJAX �chou�e");
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Donn�es invalides",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                // Traitement du PDF
                var result = await _pdfSecurityService.ProcessPdfAsync(PdfFile, UserName);

                // Retour JSON pour AJAX
                return new JsonResult(new
                {
                    success = result.Success,
                    message = result.Message,
                    securedPdfUrl = result.SecuredPdfPath,
                    proofFileUrl = result.ProofFilePath,
                    steps = result.ProcessingSteps,
                    originalHash = result.OriginalHash,
                    processedHash = result.ProcessedHash,
                    processedAt = result.ProcessedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    fileSizeBytes = result.FileSizeBytes,
                    errorDetails = result.ErrorDetails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur AJAX lors du traitement PDF");
                return new JsonResult(new
                {
                    success = false,
                    message = "Erreur serveur",
                    errorDetails = ex.Message
                });
            }
        }

        /// <summary>
        /// Handler GET - T�l�chargement du PDF s�curis�
        /// Permet de t�l�charger directement via une route
        /// </summary>
        public IActionResult OnGetDownloadPdf(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return NotFound();
                }

                var filePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "secured",
                    fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("Fichier non trouv�: {FilePath}", filePath);
                    return NotFound();
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                _logger.LogInformation("T�l�chargement du PDF: {FileName}", fileName);

                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du t�l�chargement: {FileName}", fileName);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Handler GET - T�l�chargement du fichier de preuve JSON
        /// </summary>
        public IActionResult OnGetDownloadProof(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return NotFound();
                }

                var filePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "secured",
                    fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("Fichier de preuve non trouv�: {FilePath}", filePath);
                    return NotFound();
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                _logger.LogInformation("T�l�chargement du fichier de preuve: {FileName}", fileName);

                return File(fileBytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du t�l�chargement de la preuve: {FileName}", fileName);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Handler GET - V�rifier le statut d'un traitement (pour polling AJAX)
        /// </summary>
        public IActionResult OnGetStatus(string requestId)
        {
            try
            {
                // Ici vous pourriez impl�menter un syst�me de cache/session
                // pour suivre l'�tat des traitements en cours

                return new JsonResult(new
                {
                    status = "completed",
                    message = "Traitement termin�"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la v�rification du statut: {RequestId}", requestId);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// M�thode priv�e pour valider les extensions de fichier
        /// </summary>
        private bool IsValidPdfExtension(string fileName)
        {
            var allowedExtensions = new[] { ".pdf" };
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }

        /// <summary>
        /// M�thode priv�e pour nettoyer le nom de fichier (s�curit�)
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            // Suppression des caract�res dangereux
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars));
            return sanitized;
        }
    }
}