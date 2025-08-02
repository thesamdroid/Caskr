using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Caskr.server.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly SendGridClient? _client;
        private readonly string? _fromEmail;
        private readonly string? _fromName;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _logger = logger;
            var apiKey = config["SendGrid:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                _client = new SendGridClient(apiKey);
            }

            _fromEmail = config["SendGrid:FromEmail"];
            _fromName = config["SendGrid:FromName"];
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            if (_client == null || string.IsNullOrEmpty(_fromEmail))
            {
                _logger.LogWarning("SendGrid configuration missing; unable to send email to {To}", to);
                return;
            }

            var from = new EmailAddress(_fromEmail, _fromName);
            var toAddress = new EmailAddress(to);
            var message = MailHelper.CreateSingleEmail(from, toAddress, subject, body, body);
            var response = await _client.SendEmailAsync(message);
            _logger.LogInformation("Sent email to {To} with status {StatusCode}", to, response.StatusCode);
        }
    }
}
