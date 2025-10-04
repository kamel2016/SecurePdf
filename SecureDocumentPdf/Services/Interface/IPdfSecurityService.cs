using SecureDocumentPdf.Models;

namespace SecureDocumentPdf.Services.Interface
{
    /// <summary>
    /// Interface pour les services de sécurisation PDF
    /// </summary>
    public interface IPdfSecurityService
    {
        /// <summary>
        /// Traite et sécurise un fichier PDF uploadé
        /// </summary>
        /// <param name="file">Fichier PDF à traiter</param>
        /// <param name="userName">Nom de l'utilisateur</param>
        /// <returns>Résultat du traitement</returns>
        Task<UploadResult> ProcessPdfAsync(IFormFile file, string userName);

        /// <summary>
        /// Calcule le hash SHA256 d'un fichier
        /// </summary>
        /// <param name="filePath">Chemin du fichier</param>
        /// <returns>Hash SHA256</returns>
        Task<string> CalculateSha256HashAsync(string filePath);

        /// <summary>
        /// Génère un fichier de preuve JSON signé
        /// </summary>
        /// <param name="pdfPath">Chemin du PDF</param>
        /// <param name="userName">Nom utilisateur</param>
        /// <param name="hash">Hash du fichier</param>
        /// <returns>Chemin du fichier de preuve</returns>
        Task<string> GenerateProofFileAsync(string pdfPath, string userName, string hash);
    }
}