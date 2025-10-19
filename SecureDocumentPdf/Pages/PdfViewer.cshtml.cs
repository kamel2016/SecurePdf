using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace SecureDocumentPdf.Pages
{
    /// <summary>
    /// Page Model pour l'interface de visualisation PDF sécurisée
    /// </summary>
    public class PdfViewerModel : PageModel
    {
        private readonly ILogger<PdfViewerModel> _logger;

        public PdfViewerModel(ILogger<PdfViewerModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Propriété pour afficher des messages à l'utilisateur
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// GET: Affichage de la page
        /// </summary>
        public void OnGet()
        {
            _logger.LogInformation("Page visualiseur PDF chargée");
        }

        /// <summary>
        /// POST: Traitement optionnel côté serveur si nécessaire
        /// (La plupart du traitement se fait via l'API JavaScript)
        /// </summary>
        public IActionResult OnPost()
        {
            // Cette méthode peut être utilisée pour des opérations supplémentaires
            // si besoin, mais l'essentiel se passe via l'API REST

            _logger.LogInformation("POST reçu sur la page Index");

            return Page();
        }
    }
}