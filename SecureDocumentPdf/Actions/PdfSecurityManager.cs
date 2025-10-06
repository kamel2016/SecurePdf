using System;
using System.IO;
using PdfSharp.Pdf;
//using PdfSharp.Security; // Important : Cet espace de noms contient les options de sécurité
using PdfSharp.Pdf.IO;   // Pour ouvrir un PDF existant
using PdfSharp.Fonts;    // Pour la résolution des polices si vous manipulez le contenu (pas directement pour la sécurité)
using PdfSharp.Pdf.Security;
using System.Drawing;
using PdfiumViewer;
using System.Drawing.Imaging; // Si vous utilisez SystemFontResolver
using System.Security.Cryptography;

namespace VerifDocumentSecure.Actions
{
    /// <summary>
    /// Fournit des fonctionnalités pour sécuriser des fichiers PDF.
    /// Nécessite le package NuGet PdfSharpCore ou PdfSharp.
    /// </summary>
    public class PdfSecurityManager
    {
        /// <summary>
        /// Applique des protections à un fichier PDF existant :
        /// - Empêche la modification du document.
        /// - Empêche la copie de texte.
        /// - Empêche l'impression.
        /// - Ne peut pas empêcher une capture d'écran au niveau de l'OS.
        /// </summary>
        /// <param name="sourcePdfPath">Le chemin complet du fichier PDF source.</param>
        /// <param name="outputSecuredPdfPath">Le chemin complet où le fichier PDF sécurisé sera enregistré.</param>
        /// <param name="ownerPassword">Le mot de passe propriétaire (master) pour gérer les permissions.</param>
        /// <param name="userPassword">Le mot de passe utilisateur (si nécessaire) pour ouvrir le document.</param>
        /// <returns>True si la sécurisation a réussi, False sinon.</returns>
        public static bool SecurePdf(string sourcePdfPath, string outputSecuredPdfPath, string ownerPassword, string userPassword = null)
        {
            if (!File.Exists(sourcePdfPath))
            {
                Console.WriteLine($"Erreur : Le fichier PDF source '{sourcePdfPath}' n'existe pas.");
                return false;
            }

            string outputDirectory = Path.GetDirectoryName(outputSecuredPdfPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                try
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la création du répertoire de sortie '{outputDirectory}' : {ex.Message}");
                    return false;
                }
            }

            Console.WriteLine($"Tentative de sécurisation du PDF '{sourcePdfPath}'...");

            try
            {
                // Ouvrir le document PDF existant
                // PdfDocumentOpenMode.Modify permet d'ouvrir un PDF pour le modifier
                // Pour une protection, il est généralement préférable de ne pas spécifier de lecteur de mots de passe
                // si le document source n'est pas déjà protégé.
                using (PdfSharp.Pdf.PdfDocument document = PdfReader.Open(sourcePdfPath, PdfDocumentOpenMode.Modify))
                {
                    // Récupérer ou créer le gestionnaire de sécurité

                    //if (document.SecuritySettings == null)
                    //{
                    //    document.SecuritySettings = new PdfSecuritySettings(document);
                    //}

                    // --- Définir les mots de passe ---
                    // Le mot de passe propriétaire permet d'ouvrir le document avec tous les droits (modifier les permissions).
                    document.SecuritySettings.OwnerPassword = ownerPassword;

                    // Si un mot de passe utilisateur est fourni, il sera nécessaire pour ouvrir le document.
                    // Si null ou vide, le document s'ouvre sans mot de passe utilisateur.
                    if (!string.IsNullOrEmpty(userPassword))
                    {
                        document.SecuritySettings.UserPassword = userPassword;
                    }

                    // --- Définir les permissions utilisateur ---
                    // Par défaut, toutes les permissions sont activées. Nous allons les désactiver spécifiquement.

                    // Empêcher la modification du document (y compris l'assemblage, le remplissage de formulaires, etc.)
                    document.SecuritySettings.PermitModifyDocument = false; // Ne pas permettre la modification de contenu
                    document.SecuritySettings.PermitAnnotations = false; // Ne pas permettre les annotations
                    document.SecuritySettings.PermitFormsFill = false; // Ne pas permettre de remplir les formulaires
                    document.SecuritySettings.PermitExtractContent = false; // Empêcher la copie de texte et d'images

                    // Empêcher l'impression
                    // Note: Il y a deux niveaux pour l'impression dans PdfSharp: Print et PrintHighQuality
                    // Pour empêcher totalement l'impression, désactivez les deux.
                    document.SecuritySettings.PermitPrint = false; // Empêche l'impression (qualité standard)
                                                                   //document.SecuritySettings.PermitPrintHighQuality = false; // Empêche l'impression (haute qualité)

                    // Ne pas permettre d'ajouter/supprimer des pages, etc. (souvent inclus dans PermitModifyDocument)
                    document.SecuritySettings.PermitAssembleDocument = false;

                    // PdfSharp ne propose pas de paramètre direct pour "empêcher l'imprim-écran"
                    // car cela est une fonctionnalité du système d'exploitation, pas du PDF.
                    // Les restrictions ci-dessus réduisent la capacité d'interagir avec le contenu,
                    // mais ne bloquent pas une capture d'écran de l'OS.
                    string fileNameWithoutExtesion = Path.GetFileNameWithoutExtension(sourcePdfPath);
                    outputSecuredPdfPath = Path.Combine(outputSecuredPdfPath, fileNameWithoutExtesion + "SecureFile.pdf");
                    // Enregistrer le document sécurisé
                    document.Save(outputSecuredPdfPath);
                }

                Console.WriteLine($"PDF sécurisé avec succès : '{outputSecuredPdfPath}'.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Une erreur est survenue lors de la sécurisation du PDF : {ex.Message}");
                Console.WriteLine($"Détails de l'erreur : {ex.ToString()}");
                return false;
            }
        }

        public static byte[] ExtractZoneAsImage(string pdfPath, RectangleF zonePdfPoints, int dpi = 150)
        {
            using var document = PdfiumViewer.PdfDocument.Load(pdfPath);

            // Dimensions de la page en points (unités PDF)
            var pageSize = document.PageSizes[0]; // 1ère page

            // Conversion points PDF → pixels
            int pageWidthPx = (int)(pageSize.Width * dpi / 72);
            int pageHeightPx = (int)(pageSize.Height * dpi / 72);

            using var rendered = document.Render(0, pageWidthPx, pageHeightPx, dpi, dpi, PdfRenderFlags.Annotations);

            // Zone PDF → coordonnées pixels sur l'image rendue
            float scaleX = dpi / 72f;
            float scaleY = dpi / 72f;

            int x = (int)(zonePdfPoints.X * scaleX);
            int y = (int)((pageSize.Height - zonePdfPoints.Y - zonePdfPoints.Height) * scaleY); // PDF origine en bas à gauche
            int width = (int)(zonePdfPoints.Width * scaleX);
            int height = (int)(zonePdfPoints.Height * scaleY);

            var cropRect = new Rectangle(x, y, width, height);
            Bitmap cropped = new Bitmap(width, height);
            using var g = Graphics.FromImage(cropped);
            g.DrawImage(rendered, new Rectangle(0, 0, width, height), cropRect, GraphicsUnit.Pixel);

            return BitmapToByteArray(cropped);
        }

        public static byte[] PixelizeImage(byte[] imageBytes, int blockSize = 10)
        {
            using var inputStream = new MemoryStream(imageBytes);
            using var original = new Bitmap(inputStream);
            using var pixelated = new Bitmap(original.Width, original.Height);

            using (var graphics = Graphics.FromImage(pixelated))
            {
                for (int y = 0; y < original.Height; y += blockSize)
                {
                    for (int x = 0; x < original.Width; x += blockSize)
                    {
                        int bw = Math.Min(blockSize, original.Width - x);
                        int bh = Math.Min(blockSize, original.Height - y);

                        // Calcul de la couleur moyenne du bloc
                        int r = 0, g = 0, b = 0, count = 0;
                        for (int j = 0; j < bh; j++)
                        {
                            for (int i = 0; i < bw; i++)
                            {
                                var pixel = original.GetPixel(x + i, y + j);
                                r += pixel.R;
                                g += pixel.G;
                                b += pixel.B;
                                count++;
                            }
                        }

                        var avgColor = Color.FromArgb(r / count, g / count, b / count);
                        using var brush = new SolidBrush(avgColor);
                        graphics.FillRectangle(brush, x, y, bw, bh);
                    }
                }
            }

            using var outputStream = new MemoryStream();
            pixelated.Save(outputStream, ImageFormat.Png);
            return outputStream.ToArray();
        }

        public static byte[] Encrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            aes.GenerateKey();
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();

            // Écrit l'IV au début (utile pour la déchiffrement plus tard)
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
            }

            GenerateKeyFile();

            // Optionnel : stocker la clé dans un emplacement sécurisé
            File.WriteAllBytes("wwwroot/secret/aes.key", aes.Key);

            return ms.ToArray(); // IV + données chiffrées
        }


        public static byte[] BitmapToByteArray(Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png); // ou .Jpeg, .Bmp, etc.
            return ms.ToArray();
        }

        public static void GenerateKeyFile()
        {
            using (Aes aes = Aes.Create())
            {
                // Crée le dossier s’il n’existe pas
                string directoryPath = "wwwroot/secret";
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);

                    // Écrit la clé dans le fichier
                    string keyFilePath = Path.Combine(directoryPath, "aes.key");
                    File.WriteAllBytes(keyFilePath, aes.Key);
                }
            }
        }
    }
}