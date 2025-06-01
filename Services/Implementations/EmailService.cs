using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GenericAPI.Models;

namespace GenericAPI.Services
{
    /// <summary>
    /// SMTP-based email service implementation
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _enableSsl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            _smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
            _fromEmail = _configuration["Email:FromEmail"] ?? "noreply@genericapi.com";
            _fromName = _configuration["Email:FromName"] ?? "GenericAPI";
            _username = _configuration["Email:Username"] ?? "";
            _password = _configuration["Email:Password"] ?? "";
            _enableSsl = _configuration.GetValue<bool>("Email:EnableSsl", true);
        }

        public async Task<bool> SendEmailAsync(EmailMessage message)
        {
            try
            {
                using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = _enableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_username, _password)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = message.Subject,
                    Body = message.Body,
                    IsBodyHtml = message.IsHtml
                };

                mailMessage.To.Add(message.To);

                // Add CC recipients
                foreach (var cc in message.CC)
                {
                    mailMessage.CC.Add(cc);
                }

                // Add BCC recipients
                foreach (var bcc in message.BCC)
                {
                    mailMessage.Bcc.Add(bcc);
                }

                // Add attachments
                foreach (var attachment in message.Attachments)
                {
                    var stream = new System.IO.MemoryStream(attachment.Value);
                    mailMessage.Attachments.Add(new Attachment(stream, attachment.Key));
                }

                await smtpClient.SendMailAsync(mailMessage);
                
                _logger.LogInformation("Email sent successfully to {To} with subject {Subject}", message.To, message.Subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To} with subject {Subject}", message.To, message.Subject);
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            var message = new EmailMessage
            {
                To = to,
                Subject = subject,
                Body = body,
                IsHtml = isHtml
            };

            return await SendEmailAsync(message);
        }

        public async Task<bool> SendWelcomeEmailAsync(string userEmail, string userName)
        {
            var subject = "Welcome to GenericAPI!";
            var body = GenerateWelcomeEmailBody(userName);
            
            return await SendEmailAsync(userEmail, subject, body);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string userEmail, string resetToken)
        {
            var subject = "Password Reset Request";
            var body = GeneratePasswordResetEmailBody(resetToken);
            
            return await SendEmailAsync(userEmail, subject, body);
        }

        public async Task<bool> SendOrderConfirmationEmailAsync(string userEmail, int orderId, decimal orderTotal)
        {
            var subject = $"Order Confirmation - Order #{orderId}";
            var body = GenerateOrderConfirmationEmailBody(orderId, orderTotal);
            
            return await SendEmailAsync(userEmail, subject, body);
        }

        private string GenerateWelcomeEmailBody(string userName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><title>Welcome to GenericAPI</title></head><body>");
            sb.AppendLine($"<h2>Welcome, {userName}!</h2>");
            sb.AppendLine("<p>Thank you for registering with GenericAPI. Your account has been created successfully.</p>");
            sb.AppendLine("<p>You can now start using our API services. Check out our documentation for more information.</p>");
            sb.AppendLine("<p>Best regards,<br/>The GenericAPI Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string GeneratePasswordResetEmailBody(string resetToken)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><title>Password Reset</title></head><body>");
            sb.AppendLine("<h2>Password Reset Request</h2>");
            sb.AppendLine("<p>You have requested to reset your password. Please use the following token:</p>");
            sb.AppendLine($"<p><strong>Reset Token:</strong> {resetToken}</p>");
            sb.AppendLine("<p>This token will expire in 24 hours for security reasons.</p>");
            sb.AppendLine("<p>If you did not request this password reset, please ignore this email.</p>");
            sb.AppendLine("<p>Best regards,<br/>The GenericAPI Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string GenerateOrderConfirmationEmailBody(int orderId, decimal orderTotal)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><title>Order Confirmation</title></head><body>");
            sb.AppendLine($"<h2>Order Confirmation - #{orderId}</h2>");
            sb.AppendLine("<p>Thank you for your order! We have received your order and it is being processed.</p>");
            sb.AppendLine($"<p><strong>Order ID:</strong> {orderId}</p>");
            sb.AppendLine($"<p><strong>Order Total:</strong> ${orderTotal:F2}</p>");
            sb.AppendLine("<p>You will receive another email once your order has been shipped.</p>");
            sb.AppendLine("<p>Best regards,<br/>The GenericAPI Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }
    }
}
