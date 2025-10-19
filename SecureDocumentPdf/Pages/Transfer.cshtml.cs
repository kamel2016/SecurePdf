using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace SecureDocumentPdf.Pages
{
    /// <summary>
    /// Page Model pour le transfert securise de fichiers
    /// </summary>
    public class TransferModel : PageModel
    {
        private readonly ILogger<TransferModel> _logger;

        public TransferModel(ILogger<TransferModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Mode de la page (upload ou download)
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string Mode { get; set; } = "upload";

        /// <summary>
        /// ID du transfert (pour le mode download)
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string TransferId { get; set; }

        /// <summary>
        /// Token d'acces (pour le mode download)
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string Token { get; set; }

        /// <summary>
        /// GET: Affichage de la page
        /// </summary>
        public void OnGet()
        {
            _logger.LogInformation($"Page transfert chargee - Mode: {Mode}");

            // Si un transferId et token sont fournis, c'est un lien de Téléchargement
            if (!string.IsNullOrEmpty(TransferId) && !string.IsNullOrEmpty(Token))
            {
                Mode = "download";
                _logger.LogInformation($"Mode download detecte - TransferId: {TransferId}");
            }
        }
    }
}