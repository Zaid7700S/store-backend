using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;

namespace store.Services
{
    public class EmailService(IConfiguration configuration) : IEmailService
        
    {
        private readonly IConfiguration _config = configuration;
        public async Task SendOtpEmailAsync(string targetEmail, string otpCode)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]
            ));
            email.To.Add(MailboxAddress.Parse(targetEmail));
            email.Subject = "Your Password Reset Code";

            email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = $"<h3>Your reset code is: <b>{otpCode}</b></h3><p>This code expires in 10 minutes.</p>"
            };

            using var smtp = new SmtpClient();

            try
            {
                await smtp.ConnectAsync(
                    _config["EmailSettings:SmtpServer"],
                    int.Parse(_config["EmailSettings:SmtpPort"]!),
                    SecureSocketOptions.StartTls
                );

                await smtp.AuthenticateAsync(
                    _config["EmailSettings:SenderEmail"],
                    _config["EmailSettings:AppPassword"]
                );

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
}
}
