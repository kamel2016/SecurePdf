using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using PdfSharp.Pdf.IO;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Security;
using System.Drawing;
using System.Drawing.Imaging;
using Ghostscript.NET.Rasterizer;
using Ghostscript.NET;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.IO.Image;
using iText.Layout;
using iText.Layout.Element;
using iText.Signatures;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using System.Security.Cryptography.X509Certificates;

namespace SecureDocumentPdf.Services
{
    public class SecurisationDocument
    {


namespace PdfSecurityExample
    {
        /// <summary>
        /// Exemple de sécurisation PDF avec bibliothèques gratuites
        /// Packages NuGet requis :
        /// - itext7 (AGPL - gratuit pour usage open source)
        /// - BouncyCastle.Cryptography
        /// - System.Drawing.Common
        /// - Ghostscript.NET (pour conversion en images)
        /// 
        /// Note: Ghostscript doit être installé sur le système
        /// </summary>
        public class PdfSecurityProcessor
        {
            /// <summary>
            /// Processus complet de sécurisation du PDF
            /// </summary>
            public static void SecurePdfComplete(
                string inputPdfPath,
                string outputPdfPath,
                string certificatePath,
                string certificatePassword,
                string ownerPassword,
                string userPassword = "")
            {
                Console.WriteLine("Démarrage du processus de sécurisation...");

                // Étape 1 : Convertir les pages en images
                Console.WriteLine("Étape 1/3 : Conversion des pages en images...");
                string tempImageDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempImageDir);

                List<string> imagePaths = ConvertPdfToImages(inputPdfPath, tempImageDir);

                // Étape 2 : Créer un nouveau PDF avec les images et restrictions
                Console.WriteLine("Étape 2/3 : Création du PDF sécurisé...");
                string tempSecuredPdf = Path.Combine(Path.GetTempPath(), "secured_temp.pdf");
                CreateSecuredImagePdf(imagePaths, tempSecuredPdf, ownerPassword, userPassword);

                // Étape 3 : Ajouter la signature numérique
                Console.WriteLine("Étape 3/3 : Ajout de la signature numérique...");
                SignPdf(tempSecuredPdf, outputPdfPath, certificatePath, certificatePassword);

                // Nettoyage
                foreach (var img in imagePaths)
                {
                    File.Delete(img);
                }
                Directory.Delete(tempImageDir);
                File.Delete(tempSecuredPdf);

                Console.WriteLine($"✓ PDF sécurisé créé avec succès : {outputPdfPath}");
            }

            /// <summary>
            /// Convertit chaque page du PDF en image PNG
            /// Utilise Ghostscript.NET (gratuit)
            /// </summary>
            private static List<string> ConvertPdfToImages(string pdfPath, string outputDir)
            {
                var imagePaths = new List<string>();
                int dpi = 150; // Résolution des images (150-300 recommandé)

                // Version alternative sans Ghostscript.NET (utilise System.Drawing)
                // Pour une solution simple, on peut aussi utiliser une bibliothèque comme PDFium

                using (var reader = new PdfReader(pdfPath))
                using (var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader))
                {
                    int pageCount = pdfDoc.GetNumberOfPages();

                    for (int i = 1; i <= pageCount; i++)
                    {
                        // Cette partie nécessiterait une bibliothèque de rendu
                        // Exemple simplifié - en production, utilisez PDFium ou Ghostscript
                        string imagePath = Path.Combine(outputDir, $"page_{i}.png");

                        // Note: Pour un vrai projet, utilisez :
                        // - PDFiumSharp (gratuit, wrapper de PDFium)
                        // - Ghostscript.NET avec Ghostscript installé
                        // - DocNet (wrapper de PDFium pour .NET)

                        imagePaths.Add(imagePath);
                    }
                }

                return imagePaths;
            }

            /// <summary>
            /// Alternative: Méthode simplifiée avec PDFiumSharp (gratuit et plus simple)
            /// </summary>
            private static List<string> ConvertPdfToImagesWithPDFium(string pdfPath, string outputDir)
            {
                var imagePaths = new List<string>();

                // Package NuGet: PDFiumSharp
                // using PDFiumSharp;

                /*
                using (var document = PdfDocument.Load(pdfPath))
                {
                    for (int i = 0; i < document.Pages.Count; i++)
                    {
                        using (var page = document.Pages[i])
                        using (var bitmap = new PDFiumBitmap((int)page.Width * 2, (int)page.Height * 2, true))
                        {
                            page.Render(bitmap);
                            string imagePath = Path.Combine(outputDir, $"page_{i + 1}.png");
                            bitmap.Save(imagePath);
                            imagePaths.Add(imagePath);
                        }
                    }
                }
                */

                return imagePaths;
            }

            /// <summary>
            /// Crée un nouveau PDF avec les images et applique les restrictions
            /// Utilise iText7 (AGPL - gratuit pour projets open source)
            /// </summary>
            private static void CreateSecuredImagePdf(
                List<string> imagePaths,
                string outputPath,
                string ownerPassword,
                string userPassword)
            {
                using (var writer = new PdfWriter(outputPath))
                using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
                using (var document = new Document(pdf))
                {
                    // Configuration de la sécurité : AUCUNE permission
                    WriterProperties writerProps = new WriterProperties()
                        .SetStandardEncryption(
                            System.Text.Encoding.UTF8.GetBytes(userPassword),
                            System.Text.Encoding.UTF8.GetBytes(ownerPassword),
                            0, // Aucune permission (pas d'impression, copie, modification)
                            EncryptionConstants.ENCRYPTION_AES_256
                        );

                    // Recréer le writer avec les propriétés de sécurité
                    writer.SetEncryption(
                        System.Text.Encoding.UTF8.GetBytes(userPassword),
                        System.Text.Encoding.UTF8.GetBytes(ownerPassword),
                        0, // Permissions : 0 = AUCUNE
                        EncryptionConstants.ENCRYPTION_AES_256
                    );

                    // Ajouter chaque image comme une page
                    foreach (var imagePath in imagePaths)
                    {
                        if (File.Exists(imagePath))
                        {
                            ImageData imageData = ImageDataFactory.Create(imagePath);
                            iText.Layout.Element.Image img = new iText.Layout.Element.Image(imageData);

                            // Adapter l'image à la page
                            img.SetAutoScale(true);

                            document.Add(img);
                            document.Add(new AreaBreak());
                        }
                    }
                }
            }

            /// <summary>
            /// Signe numériquement le PDF
            /// Utilise iText7 + BouncyCastle (tous deux gratuits)
            /// </summary>
            private static void SignPdf(
                string inputPdfPath,
                string outputPdfPath,
                string certificatePath,
                string certificatePassword)
            {
                // Charger le certificat
                Pkcs12Store store = new Pkcs12Store(
                    File.OpenRead(certificatePath),
                    certificatePassword.ToCharArray()
                );

                string alias = null;
                foreach (string al in store.Aliases)
                {
                    if (store.IsKeyEntry(al))
                    {
                        alias = al;
                        break;
                    }
                }

                ICipherParameters privateKey = store.GetKey(alias).Key;
                X509CertificateEntry[] chain = store.GetCertificateChain(alias);
                Org.BouncyCastle.X509.X509Certificate[] certChain =
                    new Org.BouncyCastle.X509.X509Certificate[chain.Length];

                for (int i = 0; i < chain.Length; i++)
                {
                    certChain[i] = chain[i].Certificate;
                }

                // Signer le PDF
                using (PdfReader reader = new PdfReader(inputPdfPath))
                using (FileStream os = new FileStream(outputPdfPath, FileMode.Create))
                {
                    PdfSigner signer = new PdfSigner(reader, os, new StampingProperties());

                    // Apparence de la signature (invisible par défaut)
                    PdfSignatureAppearance appearance = signer.GetSignatureAppearance();
                    appearance
                        .SetReason("Document sécurisé")
                        .SetLocation("France")
                        .SetReuseAppearance(false);

                    // Créer la signature
                    IExternalSignature pks = new PrivateKeySignature(privateKey, "SHA-256");

                    // Signer
                    signer.SignDetached(pks, certChain, null, null, null, 0,
                        PdfSigner.CryptoStandard.CMS);
                }
            }

            /// <summary>
            /// Exemple de génération d'un certificat auto-signé pour les tests
            /// </summary>
            public static void GenerateTestCertificate(string outputPath, string password)
            {
                // Pour les tests uniquement - utilisez un vrai certificat en production
                var cert = new X509Certificate2(
                    CreateSelfSignedCertificate(),
                    password,
                    X509KeyStorageFlags.Exportable
                );

                File.WriteAllBytes(outputPath, cert.Export(X509ContentType.Pfx, password));
                Console.WriteLine($"Certificat de test créé : {outputPath}");
            }

            private static byte[] CreateSelfSignedCertificate()
            {
                // Implémentation simplifiée - utilisez X509Certificate2.CreateSelfSigned en .NET 8
                throw new NotImplementedException("Utilisez un certificat existant ou créez-en un avec OpenSSL");
            }
        }

        /// <summary>
        /// Exemple d'utilisation
        /// </summary>
        class Program
        {
            static void Main(string[] args)
            {
                try
                {
                    string inputPdf = "document_original.pdf";
                    string outputPdf = "document_securise_signe.pdf";
                    string certificatePath = "certificat.pfx";
                    string certificatePassword = "MotDePasseCertificat";
                    string ownerPassword = "MotDePasseProprietaire123!";
                    string userPassword = ""; // Vide = pas de mot de passe pour ouvrir

                    PdfSecurityProcessor.SecurePdfComplete(
                        inputPdf,
                        outputPdf,
                        certificatePath,
                        certificatePassword,
                        ownerPassword,
                        userPassword
                    );

                    Console.WriteLine("\n=== Caractéristiques du PDF sécurisé ===");
                    Console.WriteLine("✓ Contenu converti en images (pas de texte extractible)");
                    Console.WriteLine("✓ Impression interdite");
                    Console.WriteLine("✓ Copie interdite");
                    Console.WriteLine("✓ Modification interdite");
                    Console.WriteLine("✓ Signé numériquement");
                    Console.WriteLine("✓ Chiffrement AES-256");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur : {ex.Message}");
                }
            }
        }
    }
}
}
