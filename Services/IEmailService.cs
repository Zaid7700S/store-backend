namespace store.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string targetEmail, string otpCode);

    }
}
