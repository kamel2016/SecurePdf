using Microsoft.AspNetCore.Mvc;
using SecureDocumentPdf.Models;
using SecureDocumentPdf.Services.Interface;

namespace SecureDocumentPdf.Controllers
{
    /// <summary>
    /// Contrôleur pour la visualisation sécurisée des PDF
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PdfViewerController : ControllerBase
    {
        private readonly IPdfSecurityValidator _securityValidator;
        private readonly ILogger<PdfViewerController> _logger;

        public PdfViewerController(
            IPdfSecurityValidator securityValidator,
            ILogger<PdfViewerController> logger)
        {
            _securityValidator = securityValidator;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint health pour keep-alive Render.com
        /// </summary>
        [HttpGet("/health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "alive",
                timestamp = DateTime.UtcNow,
                service = "Secure PDF Viewer"
            });
        }

        /// <summary>
        /// Upload et validation d'un PDF
        /// POST /api/pdfviewer/upload
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)] // 50 MB max
        public async Task<ActionResult<PdfViewResponse>> UploadPdf(
            [FromForm] IFormFile file,
            [FromForm] string expectedHash = null,
            [FromForm] string userPassword = null)
        {
            try
            {
                // 1. Validation basique
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new PdfViewResponse
                    {
                        CanView = false,
                        ErrorMessage = "Aucun fichier fourni"
                    });
                }

                // 2. Vérifier l'extension
                if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new PdfViewResponse
                    {
                        CanView = false,
                        ErrorMessage = "Le fichier doit être un PDF"
                    });
                }

                // 3. Lire le fichier
                byte[] fileData;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileData = memoryStream.ToArray();
                }

                // 4. Créer la requête de validation
                var request = new PdfViewRequest
                {
                    DocumentId = Guid.NewGuid().ToString(),
                    FileName = file.FileName,
                    FileData = fileData,
                    ExpectedHash = expectedHash,
                    UserPassword = userPassword
                };

                // 5. Valider le PDF
                var validationResult = await _securityValidator.ValidateAsync(request);

                // 6. Préparer la réponse
                var response = new PdfViewResponse
                {
                    CanView = validationResult.IsValid,
                    ViewToken = validationResult.ViewToken,
                    ErrorMessage = validationResult.ErrorMessage,
                    ValidationResult = validationResult
                };

                // 7. Si valide, convertir le PDF en base64 pour affichage
                if (validationResult.IsValid)
                {
                    response.PdfDataUrl = $"data:application/pdf;base64,{Convert.ToBase64String(fileData)}";

                    _logger.LogInformation($"PDF validé avec succès: {file.FileName}");
                }
                else
                {
                    _logger.LogWarning($"Validation PDF échouée: {file.FileName} - {validationResult.ErrorMessage}");
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'upload du PDF");

                return StatusCode(500, new PdfViewResponse
                {
                    CanView = false,
                    ErrorMessage = $"Erreur serveur: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Valider uniquement l'intégrité d'un PDF (hash)
        /// POST /api/pdfviewer/verify-integrity
        /// </summary>
        [HttpPost("verify-integrity")]
        public async Task<ActionResult<object>> VerifyIntegrity(
            [FromForm] IFormFile file,
            [FromForm] string expectedHash)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { valid = false, message = "Aucun fichier fourni" });
                }

                if (string.IsNullOrEmpty(expectedHash))
                {
                    return BadRequest(new { valid = false, message = "Hash attendu requis" });
                }

                byte[] fileData;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileData = memoryStream.ToArray();
                }

                bool isValid = await _securityValidator.VerifyIntegrityAsync(fileData, expectedHash);

                return Ok(new
                {
                    valid = isValid,
                    message = isValid
                        ? "Le document est intègre (non modifié)"
                        : "Le document a été modifié ou corrompu",
                    fileName = file.FileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'intégrité");
                return StatusCode(500, new { valid = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Extraire les permissions d'un PDF
        /// POST /api/pdfviewer/permissions
        /// </summary>
        [HttpPost("permissions")]
        public async Task<ActionResult<PdfPermissionsInfo>> GetPermissions(
            [FromForm] IFormFile file,
            [FromForm] string userPassword = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Aucun fichier fourni");
                }

                byte[] fileData;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileData = memoryStream.ToArray();
                }

                var permissions = await _securityValidator.ExtractPermissionsAsync(fileData, userPassword);

                return Ok(new
                {
                    permissions,
                    summary = new
                    {
                        isReadOnly = permissions.IsReadOnly,
                        isFullyRestricted = permissions.IsFullyRestricted,
                        canPrint = permissions.AllowPrinting,
                        canCopy = permissions.AllowCopy,
                        canModify = permissions.AllowModifyContents
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'extraction des permissions");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Valider un token de visualisation
        /// GET /api/pdfviewer/validate-token
        /// </summary>
        [HttpGet("validate-token")]
        public IActionResult ValidateToken([FromQuery] string token, [FromQuery] string documentId)
        {
            try
            {
                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(documentId))
                {
                    return BadRequest(new { valid = false, message = "Token et documentId requis" });
                }

                bool isValid = _securityValidator.ValidateViewToken(token, documentId);

                return Ok(new
                {
                    valid = isValid,
                    message = isValid ? "Token valide" : "Token invalide ou expiré"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation du token");
                return StatusCode(500, new { valid = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Télécharger un PDF sécurisé (avec vérifications)
        /// GET /api/pdfviewer/download/{documentId}
        /// </summary>
        [HttpGet("download/{documentId}")]
        public IActionResult DownloadPdf(
            string documentId,
            [FromQuery] string token)
        {
            try
            {
                // 1. Valider le token
                if (!_securityValidator.ValidateViewToken(token, documentId))
                {
                    return Unauthorized(new { message = "Token invalide ou expiré" });
                }

                // 2. Ici, vous devriez récupérer le PDF depuis votre stockage
                // (Base de données, système de fichiers, cloud storage, etc.)
                // Pour cet exemple, on retourne une erreur

                return NotFound(new { message = "Fonctionnalité de téléchargement non implémentée" });

                // Exemple d'implémentation:
                // byte[] pdfData = await _storageService.GetPdfAsync(documentId);
                // return File(pdfData, "application/pdf", "document.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement du PDF");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtenir les informations de sécurité d'un PDF déjà validé
        /// GET /api/pdfviewer/security-info/{documentId}
        /// </summary>
        [HttpGet("security-info/{documentId}")]
        public IActionResult GetSecurityInfo(
            string documentId,
            [FromQuery] string token)
        {
            try
            {
                // Valider le token
                if (!_securityValidator.ValidateViewToken(token, documentId))
                {
                    return Unauthorized(new { message = "Token invalide" });
                }

                // Ici, vous devriez récupérer les infos depuis un cache ou DB
                // Pour l'exemple, on retourne des données fictives

                return Ok(new
                {
                    documentId,
                    securityLevel = "High",
                    isEncrypted = true,
                    hasWatermark = true,
                    isReadOnly = true,
                    validatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des infos de sécurité");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint de test pour vérifier si un fichier est un PDF valide
        /// POST /api/pdfviewer/is-valid-pdf
        /// </summary>
        [HttpPost("is-valid-pdf")]
        public async Task<IActionResult> IsValidPdf([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Ok(new { valid = false, message = "Aucun fichier fourni" });
                }

                byte[] fileData;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileData = memoryStream.ToArray();
                }

                // Vérification basique du format PDF
                bool isPdf = fileData.Length >= 5 &&
                            fileData[0] == 0x25 && // %
                            fileData[1] == 0x50 && // P
                            fileData[2] == 0x44 && // D
                            fileData[3] == 0x46 && // F
                            fileData[4] == 0x2D;   // -

                return Ok(new
                {
                    valid = isPdf,
                    message = isPdf ? "Fichier PDF valide" : "Ce n'est pas un fichier PDF",
                    fileName = file.FileName,
                    fileSize = file.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification du PDF");
                return StatusCode(500, new { valid = false, message = ex.Message });
            }
        }
    }
}