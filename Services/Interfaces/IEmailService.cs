using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenericAPI.Services
{
    /// <summary>
    /// Email message model
    /// </summary>
    public class EmailMessage
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
        public List<string> CC { get; set; } = new();
        public List<string> BCC { get; set; } = new();
        public Dictionary<string, byte[]> Attachments { get; set; } = new();
    }

    /// <summary>
    /// Interface for email service operations
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email message
        /// </summary>
        /// <param name="message">Email message to send</param>
        /// <returns>True if sent successfully</returns>
        Task<bool> SendEmailAsync(EmailMessage message);

        /// <summary>
        /// Sends a simple email
        /// </summary>
        /// <param name="to">Recipient email</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body</param>
        /// <param name="isHtml">Whether body is HTML</param>
        /// <returns>True if sent successfully</returns>
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);

        /// <summary>
        /// Sends a welcome email to new users
        /// </summary>
        /// <param name="userEmail">User's email address</param>
        /// <param name="userName">User's name</param>
        /// <returns>True if sent successfully</returns>
        Task<bool> SendWelcomeEmailAsync(string userEmail, string userName);

        /// <summary>
        /// Sends password reset email
        /// </summary>
        /// <param name="userEmail">User's email address</param>
        /// <param name="resetToken">Password reset token</param>
        /// <returns>True if sent successfully</returns>
        Task<bool> SendPasswordResetEmailAsync(string userEmail, string resetToken);

        /// <summary>
        /// Sends order confirmation email
        /// </summary>
        /// <param name="userEmail">User's email address</param>
        /// <param name="orderId">Order ID</param>
        /// <param name="orderTotal">Order total amount</param>
        /// <returns>True if sent successfully</returns>
        Task<bool> SendOrderConfirmationEmailAsync(string userEmail, int orderId, decimal orderTotal);
    }
}
