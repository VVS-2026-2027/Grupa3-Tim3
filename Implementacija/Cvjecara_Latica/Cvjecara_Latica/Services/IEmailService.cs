namespace Cvjecara_Latica.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string bodyHtml);
        Task SendReminder(string toEmail);
        Task SendDeactivationNotice(string toEmail);
    }
}
