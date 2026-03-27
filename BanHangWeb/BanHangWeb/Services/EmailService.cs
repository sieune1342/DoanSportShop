using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace BanHangWeb.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gửi email đơn giản: chỉ cần email người nhận.
        /// </summary>
        public Task<bool> SendEmailAsync(string recipientEmail, string subject, string bodyHtml)
        {
            // dùng email làm luôn display name
            return SendEmailAsync(recipientEmail, recipientEmail, subject, bodyHtml);
        }

        /// <summary>
        /// Gửi email đầy đủ: có cả tên + email người nhận.
        /// </summary>
        public async Task<bool> SendEmailAsync(
            string recipientName,
            string recipientEmail,
            string subject,
            string bodyHtml)
        {
            try
            {
                var section = _configuration.GetSection("EmailSettings");

                var smtpServer = section["SMTPServer"];
                var smtpPort = section.GetValue<int>("SmtpPort");
                var smtpUser = section["SenderEmail"];
                var smtpPass = section["Password"];
                var senderEmail = section["SenderEmail"];
                var senderName = section["SenderName"] ?? senderEmail;
                var enableSSL = section.GetValue<bool>("EnableSSL");

                // Kiểm tra config
                if (string.IsNullOrWhiteSpace(smtpServer) ||
                    string.IsNullOrWhiteSpace(smtpUser) ||
                    string.IsNullOrWhiteSpace(smtpPass))
                {
                    Console.WriteLine("Cấu hình EmailSettings không hợp lệ. Vui lòng kiểm tra appsettings.json");
                    return false;
                }

                // Tạo message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(
                    string.IsNullOrWhiteSpace(recipientName) ? recipientEmail : recipientName,
                    recipientEmail));

                message.Subject = subject ?? string.Empty;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = bodyHtml
                };
                message.Body = bodyBuilder.ToMessageBody();

                // Gửi mail
                using var client = new SmtpClient();

                var secureOption = enableSSL
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto;

                await client.ConnectAsync(smtpServer, smtpPort, secureOption);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi email: {ex}");
                return false;
            }
        }
    }
}
