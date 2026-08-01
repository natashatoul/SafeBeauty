using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Linq;

namespace SafeBeauty.API.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;
        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }
        public void SendEmail(string toEmail, string subject, string body)
        {
            // Console.WriteLine($"[DEBUG] Username: '{_emailSettings.SmtpUsername}' (length: {_emailSettings.SmtpUsername.Length})");
            // Console.WriteLine($"[DEBUG] Password length: {_emailSettings.SmtpPassword.Length}");
            // Console.WriteLine($"[DEBUG] Password char codes: {string.Join(",", _emailSettings.SmtpPassword.Select(c => (int)c))}");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Support Student App", _emailSettings.SmtpUsername));
            message.To.Add(new MailboxAddress("Reciever Name", toEmail));
            message.Subject = subject;
            var textPart = new TextPart("plain")
            {
                Text = body
            };
            message.Body = textPart;
            using (var client = new SmtpClient())
            {
                client.Connect(_emailSettings.SmtpServer, _emailSettings.SmtpPort,
               SecureSocketOptions.StartTls);
                client.Authenticate(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                client.Send(message);
                client.Disconnect(true);
            }
        }
    }
}


