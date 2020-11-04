using MailKit.Net.Smtp;
using MimeKit;
using MP.Client.Common.Configuration;
using System.Threading.Tasks;

namespace MP.Client.Common.Email
{
    public class EmailService
    {
        MailOptions _mailOptions;

        public EmailService()
        {
            _mailOptions = SiteConfigurationManager.Config.MailOptions;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress("Администрация сайта", _mailOptions.Address));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = message
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_mailOptions.Host, _mailOptions.Port, _mailOptions.UseSSL);
                await client.AuthenticateAsync(_mailOptions.Address, _mailOptions.Password);
                await client.SendAsync(emailMessage);

                await client.DisconnectAsync(true);
            }
        }
    }
}
