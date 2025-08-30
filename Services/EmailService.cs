using System.Net;
using System.Net.Mail;

namespace MULTI___DATABASE_MANAGEMENT_PORTAL.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // Original method (unchanged for backward compatibility)
        public bool SendEmail(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var host = _config["Smtp:Host"];
                var port = int.Parse(_config["Smtp:Port"] ?? "587");
                var username = _config["Smtp:Username"];
                var password = _config["Smtp:Password"];
                var fromEmail = _config["Smtp:FromEmail"];

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true
                };

                var message = new MailMessage(fromEmail, toEmail, subject, htmlBody)
                {
                    IsBodyHtml = true
                };

                client.Send(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email send failed: {ex.Message}");
                return false;
            }
        }

        // New dynamic method for noreply emails
        public bool SendNoReplyEmail(string toEmail, string subject, string htmlBody, string fromEmail = null, string fromDisplayName = null)
        {
            try
            {
                // Use provided fromEmail or default to config
                string senderEmail = fromEmail ?? _config["Smtp:FromEmail"];
                string displayName = fromDisplayName ?? "Ticketing System";

                // Get SMTP settings based on sender's domain
                var smtpConfig = GetSmtpConfigForEmail(senderEmail);

                using var client = new SmtpClient(smtpConfig.Host, smtpConfig.Port)
                {
                    Credentials = new NetworkCredential(smtpConfig.Username, smtpConfig.Password),
                    EnableSsl = true
                };

                var mailFrom = new MailAddress(senderEmail, displayName);
                var message = new MailMessage()
                {
                    From = mailFrom,
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                };
                message.To.Add(toEmail);

                client.Send(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NoReply Email send failed: {ex}");
                return false;
            }
        }

        // Fully dynamic method for custom SMTP settings
        public bool SendEmailDynamic(
            string toEmail,
            string subject,
            string htmlBody,
            string smtpHost,
            int smtpPort,
            string smtpUser,
            string smtpPass,
            string fromEmail,
            string fromDisplayName = null,
            string replyTo = null)
        {
            try
            {
                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var mailFrom = string.IsNullOrWhiteSpace(fromDisplayName)
                    ? new MailAddress(fromEmail)
                    : new MailAddress(fromEmail, fromDisplayName);

                var message = new MailMessage()
                {
                    From = mailFrom,
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                };
                message.To.Add(toEmail);

                if (!string.IsNullOrWhiteSpace(replyTo))
                    message.ReplyToList.Add(new MailAddress(replyTo));

                client.Send(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dynamic Email send failed: {ex}");
                return false;
            }
        }

        // Helper method to get SMTP config based on email domain
        private SmtpConfig GetSmtpConfigForEmail(string email)
        {
            try
            {
                string domain = email.Split('@')[1].ToLower();

                return domain switch
                {
                    "gmail.com" => new SmtpConfig
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        Username = _config["Smtp:Gmail:Username"] ?? _config["Smtp:Username"],
                        Password = _config["Smtp:Gmail:Password"] ?? _config["Smtp:Password"]
                    },
                    "outlook.com" or "hotmail.com" or "live.com" => new SmtpConfig
                    {
                        Host = "smtp-mail.outlook.com",
                        Port = 587,
                        Username = _config["Smtp:Outlook:Username"] ?? _config["Smtp:Username"],
                        Password = _config["Smtp:Outlook:Password"] ?? _config["Smtp:Password"]
                    },
                    "yahoo.com" => new SmtpConfig
                    {
                        Host = "smtp.mail.yahoo.com",
                        Port = 587,
                        Username = _config["Smtp:Yahoo:Username"] ?? _config["Smtp:Username"],
                        Password = _config["Smtp:Yahoo:Password"] ?? _config["Smtp:Password"]
                    },
                    // Add more providers as needed
                    _ => new SmtpConfig // Default/fallback
                    {
                        Host = _config["Smtp:Host"],
                        Port = int.Parse(_config["Smtp:Port"] ?? "587"),
                        Username = _config["Smtp:Username"],
                        Password = _config["Smtp:Password"]
                    }
                };
            }
            catch
            {
                // If domain parsing fails, return default config
                return new SmtpConfig
                {
                    Host = _config["Smtp:Host"],
                    Port = int.Parse(_config["Smtp:Port"] ?? "587"),
                    Username = _config["Smtp:Username"],
                    Password = _config["Smtp:Password"]
                };
            }
        }
    }

    // Helper class for SMTP configuration
    public class SmtpConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
