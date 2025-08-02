using Caskr.server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Caskr.Server.Tests;

public class EmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_UsesSendGridClient()
    {
        var settings = new Dictionary<string, string?>
        {
            ["SendGrid:ApiKey"] = "test-api-key",
            ["SendGrid:FromEmail"] = "support@caskr.co",
            ["SendGrid:FromName"] = "caskr",
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var logger = Mock.Of<ILogger<EmailService>>();
        var client = new Mock<ISendGridClient>();
        SendGridMessage? captured = null;
        client.Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
              .Callback<SendGridMessage, CancellationToken>((m, _) => captured = m)
              .ReturnsAsync(new Response(HttpStatusCode.Accepted, null, null));
        var service = new EmailService(config, logger, client.Object);

        await service.SendEmailAsync("user@example.com", "Subject", "Body");

        client.Verify(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("support@caskr.co", captured?.From.Email);
        Assert.Equal("caskr", captured?.From.Name);
        Assert.Equal("user@example.com", captured?.Personalizations[0].Tos[0].Email);
    }
}
