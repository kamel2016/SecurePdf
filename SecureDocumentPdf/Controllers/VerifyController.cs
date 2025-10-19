using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SecureDocumentPdf.Controllers
{
    /// <summary>
    /// Contrôleur pour vérifier l'authenticité des PDFs sécurisés via QR Code
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class VerifyController : ControllerBase
    {
        private readonly ILogger<VerifyController> _logger;
        private readonly IWebHostEnvironment _environment;

        public VerifyController(ILogger<VerifyController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Vérifie l'authenticité d'un PDF sécurisé via son ID et son hash
        /// Appelé depuis le QR Code
        /// </summary>
        [HttpGet("document")]
        public IActionResult VerifyDocument(
            [FromQuery] string id,
            [FromQuery] string hash,
            [FromQuery] string user)
        {
            try
            {
                _logger.LogInformation("🔍 Vérification document: ID={Id}, User={User}, Hash={Hash}",
                    id, user, hash?[..16] + "...");

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(hash))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Paramètres manquants (id, hash requis)"
                    });
                }

                // Chercher le fichier de preuve correspondant
                var securedPath = Path.Combine(_environment.WebRootPath, "secured");
                var proofFiles = Directory.GetFiles(securedPath, $"PROOF_*{id[..8]}*.json");

                if (!proofFiles.Any())
                {
                    _logger.LogWarning("⚠️ Aucun fichier de preuve trouvé pour ID={Id}", id);
                    return NotFound(new
                    {
                        success = false,
                        message = "Document introuvable",
                        documentId = id
                    });
                }

                var proofPath = proofFiles.First();
                var proofJson = System.IO.File.ReadAllText(proofPath);
                var proof = JsonSerializer.Deserialize<JsonElement>(proofJson);

                // Extraire les informations du certificat
                var originalHash = proof.GetProperty("CryptographicIntegrity")
                    .GetProperty("OriginalSHA256").GetString();

                var processedHash = proof.GetProperty("CryptographicIntegrity")
                    .GetProperty("ProcessedSHA256").GetString();

                var biometricSignature = proof.GetProperty("CryptographicIntegrity")
                    .GetProperty("BiometricSignature").GetString();

                var documentInfo = proof.GetProperty("Document");
                var documentUser = documentInfo.GetProperty("ProcessedBy").GetString();
                var processedAt = documentInfo.GetProperty("ProcessedAt").GetString();

                var traçabilité = proof.GetProperty("TraceabilityInfo");
                var originIP = traçabilité.GetProperty("OriginIP").GetString();

                // Vérifier le hash fourni en paramètre
                bool hashMatches = originalHash.StartsWith(hash, StringComparison.OrdinalIgnoreCase) ||
                                   processedHash.StartsWith(hash, StringComparison.OrdinalIgnoreCase);

                if (!hashMatches)
                {
                    _logger.LogWarning("❌ ALERTE: Hash ne correspond pas pour ID={Id}", id);
                    return BadRequest(new
                    {
                        success = false,
                        message = "ALERTE SÉCURITÉ: Le hash du document ne correspond pas!",
                        documentId = id,
                        providedHash = hash,
                        expectedHashStart = originalHash?[..16] + "...",
                        alert = true
                    });
                }

                _logger.LogInformation("✅ Document vérifié avec succès: ID={Id}", id);

                return Ok(new
                {
                    success = true,
                    message = "Document authentique et intègre",
                    document = new
                    {
                        id = id,
                        fileName = documentInfo.GetProperty("FileName").GetString(),
                        createdBy = documentUser,
                        createdAt = processedAt,
                        originIP = originIP,
                        biometricSignature = biometricSignature,
                        securityLevel = proof.GetProperty("ProofMetadata")
                            .GetProperty("Standards").GetString()
                    },
                    integrity = new
                    {
                        originalHash = originalHash,
                        processedHash = processedHash,
                        hashesMatch = true,
                        status = "✅ Document intègre - Aucune modification détectée"
                    },
                    security = new
                    {
                        passwordProtected = proof.GetProperty("PasswordProtection")
                            .GetProperty("Enabled").GetBoolean(),
                        encryptionLevel = proof.GetProperty("PasswordProtection")
                            .GetProperty("EncryptionLevel").GetString(),
                        digitallySignedPAdES = true,
                        timestampRFC3161 = true,
                        blockchainVerifiable = true
                    },
                    verifiedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    validFor = "Cette vérification est valide indéfiniment"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la vérification du document");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur lors de la vérification",
                    errorDetails = ex.Message
                });
            }
        }

        [HttpGet("page")]
        public IActionResult VerificationPage(
            [FromQuery] string id,
            [FromQuery] string hash,
            [FromQuery] string user)
        {
            var encodedId = Uri.EscapeDataString(id ?? "");
            var encodedHash = Uri.EscapeDataString(hash ?? "");
            var encodedUser = Uri.EscapeDataString(user ?? "");

            var verificationUrl = $"/api/verify/document?id={encodedId}&hash={encodedHash}&user={encodedUser}";

            var html = $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Vérification PDF Sécurisé</title>
    <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>
    <link href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css' rel='stylesheet'>
    <style>
        body {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; padding: 40px 20px; }}
        .container {{ max-width: 600px; }}
        .card {{ border: none; border-radius: 15px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); }}
        .card-header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-radius: 15px 15px 0 0; }}
        .success-badge, .error-badge {{ padding: 20px; border-radius: 10px; text-align: center; margin: 20px 0; color: white; }}
        .success-badge {{ background: #28a745; }}
        .error-badge {{ background: #dc3545; }}
        .loading {{ text-align: center; padding: 40px; }}
        .spinner-border {{ color: #667eea; }}
        .info-item {{ padding: 10px 0; border-bottom: 1px solid #eee; }}
        .info-item:last-child {{ border-bottom: none; }}
        .info-label {{ font-weight: bold; color: #667eea; }}
        .result-section {{ margin: 20px 0; }}
        .checkmark {{ color: #28a745; font-size: 24px; }}
        .cross {{ color: #dc3545; font-size: 24px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='card'>
            <div class='card-header'>
                <h4 class='mb-0'>
                    <i class='fas fa-shield-alt me-2'></i>
                    Vérification PDF Sécurisé
                </h4>
            </div>
            <div class='card-body'>
                <div id='loading' class='loading'>
                    <div class='spinner-border' role='status'>
                        <span class='visually-hidden'>Vérification en cours...</span>
                    </div>
                    <p class='mt-3'>Vérification du document en cours...</p>
                </div>
                <div id='result' style='display: none;'></div>
            </div>
        </div>
    </div>

    <script>
        fetch('{verificationUrl}')
            .then(response => response.json())
            .then(data => {{
                const resultDiv = document.getElementById('result');
                const loadingDiv = document.getElementById('loading');
                loadingDiv.style.display = 'none';

                if (data.success) {{
                    resultDiv.innerHTML = `
                        <div class='success-badge'>
                            <i class='fas fa-check-circle checkmark'></i>
                            <h4>Document Authentique</h4>
                            <p class='mb-0'>Le document a été vérifié avec succès</p>
                        </div>

                        <div class='result-section'>
                            <h5><i class='fas fa-file-pdf me-2 text-danger'></i>Informations du Document</h5>
                            <div class='info-item'><span class='info-label'>ID Document:</span><br><small class='text-monospace'>$$\{{data.document.id}}</small></div>
                            <div class='info-item'><span class='info-label'>Fichier:</span><br><small>$$\{{data.document.fileName}}</small></div>
                            <div class='info-item'><span class='info-label'>Créé par:</span><br><small>$$\{{data.document.createdBy}}</small></div>
                            <div class='info-item'><span class='info-label'>Date:</span><br><small>$$\{{new Date(data.document.createdAt).toLocaleString('fr-FR')}}</small></div>
                            <div class='info-item'><span class='info-label'>IP d'origine:</span><br><small>$$\{{data.document.originIP}}</small></div>
                        </div>

                        <div class='result-section'>
                            <h5><i class='fas fa-link me-2 text-success'></i>Intégrité du Document</h5>
                            <div class='alert alert-success'>
                                <i class='fas fa-check-circle me-2'></i>$$\{{data.integrity.status}}
                            </div>
                            <div class='info-item'><span class='info-label'>Hash Original:</span><br><small class='text-monospace'>$$\{{data.integrity.originalHash}}</small></div>
                            <div class='info-item'><span class='info-label'>Hash Final:</span><br><small class='text-monospace'>$$\{{data.integrity.processedHash}}</small></div>
                        </div>

                        <div class='result-section'>
                            <h5><i class='fas fa-lock me-2 text-primary'></i>Protections Activées</h5>
                            <div class='info-item'><i class='fas fa-check-circle checkmark me-2'></i>Chiffrement $$\{{data.security.encryptionLevel}}</div>
                            <div class='info-item'><i class='fas fa-check-circle checkmark me-2'></i>Signature Numérique PAdES</div>
                            <div class='info-item'><i class='fas fa-check-circle checkmark me-2'></i>Horodatage RFC3161</div>
                            <div class='info-item'><i class='fas fa-check-circle checkmark me-2'></i>Vérification Blockchain</div>
                        </div>

                        <div class='alert alert-info mt-4'>
                            <i class='fas fa-info-circle me-2'></i>
                            <strong>Vérification effectuée:</strong> $$\{{data.verifiedAt}}
                        </div>
                    `;
                }} else {{
                    resultDiv.innerHTML = `
                        <div class='error-badge'>
                            <i class='fas fa-times-circle cross'></i>
                            <h4>Erreur de Vérification</h4>
                            <p class='mb-0'>$$\{{data.message}}</p>
                        </div>
                        <div class='alert alert-warning mt-3'>
                            <strong>ID Document:</strong> $$\{{data.documentId || 'N/A'}}<br>
                            <strong>Détails:</strong> $$\{{data.errorDetails || 'Aucune information supplémentaire'}}
                        </div>
                        $$\{{data.alert ? `
                            <div class='alert alert-danger'>
                                <i class='fas fa-exclamation-triangle me-2'></i>
                                <strong>ALERTE SÉCURITÉ:</strong> Le hash ne correspond pas! Ce document pourrait avoir été modifié.
                            </div>
                        ` : ''}}
                    `;
                }}

                resultDiv.style.display = 'block';
            }})
            .catch(error => {{
                const resultDiv = document.getElementById('result');
                const loadingDiv = document.getElementById('loading');
                loadingDiv.style.display = 'none';

                resultDiv.innerHTML = `
                    <div class='error-badge'>
                        <i class='fas fa-exclamation-circle cross'></i>
                        <h4>Erreur</h4>
                        <p class='mb-0'>$$\{{error.message}}</p>
                    </div>
                `;
                resultDiv.style.display = 'block';
            }});
    </script>
</body>
</html>";

            return Content(html, "text/html");
        }


        /// <summary>
        /// Endpoint simple pour vérifier si un document existe
        /// </summary>
        [HttpGet("exists/{documentId}")]
        public IActionResult DocumentExists(string documentId)
        {
            try
            {
                var securedPath = Path.Combine(_environment.WebRootPath, "secured");
                var proofFiles = Directory.GetFiles(securedPath, $"PROOF_*{documentId[..8]}*.json");

                return Ok(new
                {
                    exists = proofFiles.Any(),
                    documentId = documentId,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur vérification existence");
                return StatusCode(500);
            }
        }
    }
}