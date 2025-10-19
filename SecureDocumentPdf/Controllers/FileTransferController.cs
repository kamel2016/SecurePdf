using Microsoft.AspNetCore.Mvc;
using SecureDocumentPdf.Models;
using SecureDocumentPdf.Services.Interface;

namespace SecureDocumentPdf.Controllers
{
    /// <summary>
    /// Controleur pour le transfert securise de fichiers
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FileTransferController : ControllerBase
    {
        private readonly IFileTransferService _transferService;
        private readonly ILogger<FileTransferController> _logger;

        public FileTransferController(
            IFileTransferService transferService,
            ILogger<FileTransferController> logger)
        {
            _transferService = transferService;
            _logger = logger;
        }

        /// <summary>
        /// Cree un nouveau transfert securise
        /// POST /api/filetransfer/create
        /// </summary>
        [HttpPost("create")]
        [RequestSizeLimit(2_147_483_648)] // 2 GB
        public async Task<ActionResult<CreateTransferResponse>> CreateTransfer(
            [FromForm] IFormFile file,
            [FromForm] string senderEmail,
            [FromForm] string senderName,
            [FromForm] string recipientEmail = null,
            [FromForm] string message = null,
            [FromForm] int expirationHours = 24,
            [FromForm] int maxDownloads = 10,
            [FromForm] string password = null)
        {
            try
            {
                // Validation
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new CreateTransferResponse
                    {
                        Success = false,
                        ErrorMessage = "Aucun fichier fourni"
                    });
                }

                if (string.IsNullOrWhiteSpace(senderEmail))
                {
                    return BadRequest(new CreateTransferResponse
                    {
                        Success = false,
                        ErrorMessage = "Email expediteur requis"
                    });
                }

                // Limiter la duree d'expiration (max 7 jours)
                if (expirationHours < 1 || expirationHours > 168)
                {
                    return BadRequest(new CreateTransferResponse
                    {
                        Success = false,
                        ErrorMessage = "Duree d'expiration invalide (1h - 7 jours)"
                    });
                }

                // Creer la requete
                var request = new CreateTransferRequest
                {
                    SenderEmail = senderEmail,
                    SenderName = senderName ?? senderEmail,
                    RecipientEmail = recipientEmail,
                    Message = message,
                    ExpirationHours = expirationHours,
                    MaxDownloads = maxDownloads,
                    Password = password
                };

                // Creer le transfert
                using var stream = file.OpenReadStream();
                var response = await _transferService.CreateTransferAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    request);

                if (response.Success)
                {
                    // Construire l'URL complete
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    response.ShareUrl = $"{baseUrl}{response.ShareUrl}";

                    _logger.LogInformation($"Transfert cree avec succes : {response.TransferId}");
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la creation du transfert");
                return StatusCode(500, new CreateTransferResponse
                {
                    Success = false,
                    ErrorMessage = $"Erreur serveur : {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Recupere les informations d'un transfert
        /// GET /api/filetransfer/info/{transferId}
        /// </summary>
        [HttpGet("info/{transferId}")]
        public async Task<ActionResult<TransferInfo>> GetTransferInfo(
            string transferId,
            [FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new { message = "Token requis" });
                }

                var info = await _transferService.GetTransferInfoAsync(transferId, token);

                if (info == null)
                {
                    return NotFound(new { message = "Transfert introuvable ou token invalide" });
                }

                return Ok(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur recuperation info transfert : {transferId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Telecharge un fichier transfere
        /// POST /api/filetransfer/download
        /// </summary>
        [HttpPost("download")]
        public async Task<IActionResult> DownloadFile([FromBody] DownloadRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TransferId) ||
                    string.IsNullOrWhiteSpace(request.AccessToken))
                {
                    return BadRequest(new { message = "TransferId et AccessToken requis" });
                }

                // Recuperer l'IP et User-Agent
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = Request.Headers["User-Agent"].ToString();

                // Telecharger le fichier
                var (fileStream, fileName, contentType) = await _transferService.DownloadFileAsync(
                    request,
                    ipAddress,
                    userAgent);

                return File(fileStream, contentType, fileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Tentative d'acces non autorise");
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operation invalide");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du telechargement");
                return StatusCode(500, new { message = "Erreur lors du telechargement" });
            }
        }

        /// <summary>
        /// Valide un transfert (verifie mot de passe si requis)
        /// POST /api/filetransfer/validate
        /// </summary>
        [HttpPost("validate")]
        public async Task<ActionResult<object>> ValidateTransfer(
            [FromBody] DownloadRequest request)
        {
            try
            {
                var isValid = await _transferService.ValidateTransferAsync(
                    request.TransferId,
                    request.AccessToken,
                    request.Password);

                return Ok(new
                {
                    valid = isValid,
                    message = isValid ? "Acces autorise" : "Acces refuse"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur validation transfert");
                return StatusCode(500, new { valid = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Supprime un transfert
        /// DELETE /api/filetransfer/{transferId}
        /// </summary>
        [HttpDelete("{transferId}")]
        public async Task<IActionResult> DeleteTransfer(
            string transferId,
            [FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new { message = "Token requis" });
                }

                var deleted = await _transferService.DeleteTransferAsync(transferId, token);

                if (deleted)
                {
                    return Ok(new { message = "Transfert supprime avec succes" });
                }
                else
                {
                    return NotFound(new { message = "Transfert introuvable ou token invalide" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur suppression transfert : {transferId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtient les statistiques d'un transfert
        /// GET /api/filetransfer/stats/{transferId}
        /// </summary>
        [HttpGet("stats/{transferId}")]
        public async Task<ActionResult<TransferStatistics>> GetStatistics(
            string transferId,
            [FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new { message = "Token requis" });
                }

                var stats = await _transferService.GetTransferStatisticsAsync(transferId, token);

                if (stats == null)
                {
                    return NotFound(new { message = "Transfert introuvable ou token invalide" });
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur recuperation statistiques : {transferId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Nettoie les transferts expires (endpoint admin)
        /// POST /api/filetransfer/cleanup
        /// </summary>
        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupExpiredTransfers()
        {
            try
            {
                await _transferService.CleanupExpiredTransfersAsync();
                return Ok(new { message = "Nettoyage effectue avec succes" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du nettoyage");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}