using MimeKit;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace SecureDocumentPdf.Actions
{
    /// <summary>
    /// Convertisseur Email (EML) vers PDF
    /// </summary>
    public class EmailToPdfConverter
    {
        public static byte[] ConvertToPdf(IFormFile emlFile)
        {
            if (emlFile == null || emlFile.Length == 0)
                return null;

            try
            {
                using (var stream = emlFile.OpenReadStream())
                {
                    var message = MimeMessage.Load(stream);

                    var pdfBytes = Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(40);

                            page.Content().Column(column =>
                            {
                                column.Item().Text("Email Message")
                                    .FontSize(18)
                                    .Bold();

                                column.Item().PaddingTop(10);

                                column.Item().Text($"From: {message.From}")
                                    .FontSize(11);

                                column.Item().Text($"To: {message.To}")
                                    .FontSize(11);

                                column.Item().Text($"Subject: {message.Subject}")
                                    .FontSize(11)
                                    .Bold();

                                column.Item().Text($"Date: {message.Date}")
                                    .FontSize(10);

                                column.Item().PaddingTop(20);

                                // Corps du message
                                string body = message.TextBody ?? message.HtmlBody ?? "";
                                if (!string.IsNullOrEmpty(message.HtmlBody))
                                {
                                    body = System.Text.RegularExpressions.Regex.Replace(body, "<.*?>", " ");
                                    body = System.Net.WebUtility.HtmlDecode(body);
                                }

                                column.Item().Text(body)
                                    .FontSize(11)
                                    .LineHeight(1.5f);
                            });
                        });
                    }).GeneratePdf();

                    return pdfBytes;
                }
            }
            catch
            {
                return null;
            }
        }

        public static Stream ConvertToPdfStream(IFormFile emlFile)
        {
            byte[] pdfBytes = ConvertToPdf(emlFile);
            return pdfBytes == null ? null : new MemoryStream(pdfBytes);
        }
    }
}