using System.Data.SqlClient;

namespace MULTI___DATABASE_MANAGEMENT_PORTAL.Services
{
    public class NotificationService
    {
        private readonly DatabaseHelper _databaseHelper;
        private readonly EmailService _emailService;

        public NotificationService(DatabaseHelper databaseHelper, EmailService emailService)
        {
            _databaseHelper = databaseHelper;
            _emailService = emailService;
        }

        // Enhanced method with optional fromEmail parameter
        public void SendNotificationWithEmail(long recipientId, int? ticketId, int notificationType, string subject, string message, string fromEmail = null)
        {
            try
            {
                // Add notification to database
                var parameters = new[]
                {
                    new SqlParameter("@recipient_id", recipientId),
                    new SqlParameter("@ticket_id", (object)ticketId ?? DBNull.Value),
                    new SqlParameter("@notification_type", notificationType),
                    new SqlParameter("@message", message)
                };
                _databaseHelper.ExecuteStoredProcedure("sp_AddNotification", parameters);

                // Get recipient's email
                var userResult = _databaseHelper.ExecuteStoredProcedure("sp_GetUserById",
                    new[] { new SqlParameter("@id", recipientId) });

                if (userResult != null && userResult.Count > 0)
                {
                    string recipientEmail = userResult[0]["email"]?.ToString();
                    string recipientName = userResult[0]["username"]?.ToString();

                    if (!string.IsNullOrEmpty(recipientEmail))
                    {
                        // Use NoReply method for better control over sender
                        _emailService.SendNoReplyEmail(
                            toEmail: recipientEmail,
                            subject: subject,
                            htmlBody: message,
                            fromEmail: fromEmail,
                            fromDisplayName: "Ticketing System Support"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Notification send failed: {ex.Message}");
            }
        }

        // Method for sending with specific sender details (for admin/supporter notifications)
        public void SendNotificationFromUser(long recipientId, int? ticketId, int notificationType, string subject, string message, long senderId, string senderRole = null)
        {
            try
            {
                // Add notification to database
                var parameters = new[]
                {
                    new SqlParameter("@recipient_id", recipientId),
                    new SqlParameter("@ticket_id", (object)ticketId ?? DBNull.Value),
                    new SqlParameter("@notification_type", notificationType),
                    new SqlParameter("@message", message)
                };
                _databaseHelper.ExecuteStoredProcedure("sp_AddNotification", parameters);

                // Get recipient's email
                var recipientResult = _databaseHelper.ExecuteStoredProcedure("sp_GetUserById",
                    new[] { new SqlParameter("@id", recipientId) });

                // Get sender's details
                var senderResult = _databaseHelper.ExecuteStoredProcedure("sp_GetUserById",
                    new[] { new SqlParameter("@id", senderId) });

                if (recipientResult != null && recipientResult.Count > 0 && senderResult != null && senderResult.Count > 0)
                {
                    string recipientEmail = recipientResult[0]["email"]?.ToString();
                    string senderEmail = senderResult[0]["email"]?.ToString();
                    string senderName = senderResult[0]["username"]?.ToString();

                    if (!string.IsNullOrEmpty(recipientEmail) && !string.IsNullOrEmpty(senderEmail))
                    {
                        string displayName = !string.IsNullOrEmpty(senderRole)
                            ? $"{senderName} ({senderRole})"
                            : senderName;

                        _emailService.SendNoReplyEmail(
                            toEmail: recipientEmail,
                            subject: subject,
                            htmlBody: message,
                            fromEmail: "noreply@yourcompany.com", // Use consistent noreply address
                            fromDisplayName: displayName
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"User notification send failed: {ex.Message}");
            }
        }

        // Method for bulk notifications (e.g., to all admins)
        public void SendBulkNotifications(List<long> recipientIds, int? ticketId, int notificationType, string subject, string message, string fromEmail = null)
        {
            foreach (var recipientId in recipientIds)
            {
                SendNotificationWithEmail(recipientId, ticketId, notificationType, subject, message, fromEmail);

                // Add small delay to prevent being flagged as spam
                Thread.Sleep(100);
            }
        }

        public List<Dictionary<string, object>> GetUnreadNotifications(long userId)
        {
            var parameters = new[] { new SqlParameter("@recipient_id", userId) };
            return _databaseHelper.ExecuteStoredProcedure("sp_GetUnreadNotifications", parameters) ?? new List<Dictionary<string, object>>();
        }

        public void MarkAllNotificationsAsRead(long userId)
        {
            var parameters = new[] { new SqlParameter("@recipient_id", userId) };
            _databaseHelper.ExecuteStoredProcedure("sp_MarkNotificationsRead", parameters);
        }

        // Helper method to get all admin IDs
        public List<long> GetAllAdminIds()
        {
            try
            {
                var adminResult = _databaseHelper.ExecuteStoredProcedure("sp_GetAdmins", null);
                return adminResult?.Select(row => Convert.ToInt64(row["id"])).ToList() ?? new List<long>();
            }
            catch
            {
                return new List<long>();
            }
        }
    }
}
