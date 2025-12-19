using Microsoft.Identity.Client;
using MimeKit;
using MailKit.Security;
using MailKit.Net.Smtp;
using System.Collections.Generic;

namespace Final_VS1.Helper
{
    public class MailKitEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public MailKitEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public async Task SenderEmailAsync(string toEmail, string subject, string body)
        {
            await SenderEmailAsync(toEmail, subject, body, null);
        }

        public async Task SenderEmailAsync(string toEmail, string subject, string body, Dictionary<string, string>? imageUrls)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(
                emailSettings["SenderName"],
                emailSettings["SenderEmail"]
            ));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = body
            };

            // Nhúng ảnh vào email nếu có
            if (imageUrls != null && imageUrls.Any())
            {
                using var httpClient = new HttpClient();
                foreach (var kvp in imageUrls)
                {
                    try
                    {
                        var imageData = await httpClient.GetByteArrayAsync(kvp.Value);
                        var image = builder.LinkedResources.Add(kvp.Key, imageData);
                        image.ContentId = kvp.Key;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi tải ảnh {kvp.Key}: {ex.Message}");
                    }
                }
            }

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(emailSettings["SmtpServer"], int.Parse(emailSettings["SmtpPort"]), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(emailSettings["SenderEmail"], emailSettings["SenderPassword"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}

