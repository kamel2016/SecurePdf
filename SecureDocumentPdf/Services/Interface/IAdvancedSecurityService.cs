namespace SecureDocumentPdf.Services.Interface
{
    /// <summary>
    /// Service de sécurité avancée : Expiration, AES-256, Filigrane invisible
    /// </summary>
    public interface IAdvancedSecurityService
    {
        Task<string> AddExpirationDateAsync(string pdfPath, DateTime expirationDate, string userName);
        Task<byte[]> UpgradeToAES256Async(byte[] pdfBytes, string password);
        Task<string> AddInvisibleWatermarkAsync(string imagePath, string watermarkData);
        Task<string?> ExtractInvisibleWatermarkAsync(string imagePath);
    }
}