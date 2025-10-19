using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace SecureDocumentPdf.Pages
{
    /// <summary>
    /// Page Model pour l'interface de visualisation PDF s�curis�e
    /// </summary>
    public class PdfViewerModel : PageModel
    {
        private readonly ILogger<PdfViewerModel> _logger;

        public PdfViewerModel(ILogger<PdfViewerModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Propri�t� pour afficher des messages � l'utilisateur
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// GET: Affichage de la page
        /// </summary>
        public void OnGet()
        {
            _logger.LogInformation("Page visualiseur PDF charg�e");
        }

        /// <summary>
        /// POST: Traitement optionnel c�t� serveur si n�cessaire
        /// (La plupart du traitement se fait via l'API JavaScript)
        /// </summary>
        public IActionResult OnPost()
        {
            // Cette m�thode peut �tre utilis�e pour des op�rations suppl�mentaires
            // si besoin, mais l'essentiel se passe via l'API REST

            _logger.LogInformation("POST re�u sur la page Index");

            return Page();
        }
    }
}