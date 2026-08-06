using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Text.RegularExpressions;
using TheBugTracker.Models;

namespace TheBugTracker.Services
{
    public partial class SendGridEmailSender : IEmailSender, IEmailSender<ApplicationUser>
    {
        private readonly string _sendGridKey;
        private readonly string _fromAddress;
        private readonly string _fromName;

        public SendGridEmailSender(IConfiguration config)
        {
            _sendGridKey = config["SendGridKey"] ?? Environment.GetEnvironmentVariable("SendGridKey")
                ?? throw new InvalidOperationException("SendGridKey could not be found!");

            _fromAddress = config["SendGridEmail"] ?? Environment.GetEnvironmentVariable("SendGridEmail")
                ?? throw new InvalidOperationException("SendGridEmail could not be found!");

            _fromName = config["SendGridName"] ?? Environment.GetEnvironmentVariable("SendGridName")
                ?? throw new InvalidOperationException("SendGridName could not be found!");
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SendGridClient(_sendGridKey);
            var from = new EmailAddress(_fromAddress, _fromName);

            List<string> emails = [.. email.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
            List<EmailAddress> addresses = [.. emails.Select(e => new EmailAddress(e))];

            string plainTextContent = _htmlTagRegex.Replace(htmlMessage, "").Trim();

            SendGridMessage message = MailHelper.CreateSingleEmailToMultipleRecipients(
                from, addresses, subject, plainTextContent, htmlMessage);

            Response response = await client.SendEmailAsync(message);

            if (response.IsSuccessStatusCode == false)
            {
                Console.WriteLine("********* EMAIL SERVICE ERROR *********");
                Console.WriteLine(await response.Body.ReadAsStringAsync());
                Console.WriteLine("********* EMAIL SERVICE ERROR *********");

                throw new BadHttpRequestException("SendGrid Email could not be sent!");
            }
        }

        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
            SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
            SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
            SendEmailAsync(email, "Reset your password", $"Please reset your password using the following code: {resetCode}");

        private static readonly Regex _htmlTagRegex = new("<[a-zA-Z].*?>", RegexOptions.Compiled);
    }
}
