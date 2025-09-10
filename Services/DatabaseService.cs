

using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace MULTI___DATABASE_MANAGEMENT_PORTAL.Services
{
    public class DatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly AuditService _auditService;

        public DatabaseService(IConfiguration configuration, AuditService auditService)
        {
            _configuration = configuration;
            _auditService = auditService;
        }

        // Load available databases from appsettings.json
        public Dictionary<string, string> GetAvailableDatabases()
        {
            var databases = new Dictionary<string, string>();
            var connectionStringsSection = _configuration.GetSection("ConnectionStrings");

            foreach (var child in connectionStringsSection.GetChildren())
            {
                // Skip Mast_MultiData because that is used only for Audit
                if (child.Key != "Mast_MultiData")
                    databases.Add(child.Key, child.Value);
            }

            return databases;
        }

        // Get connection string for DB1 / DB2
        public string GetConnectionString(string databaseName)
        {
            return _configuration.GetConnectionString(databaseName);
        }

        // Example: Insert
        public void InsertRecord(string dbKey, string tableName, string recordId, string newValue, string modifiedBy)
        {
            var connStr = GetConnectionString(dbKey);

            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();

                // Example insert query (adjust according to your schema)
                var query = $"INSERT INTO {tableName} (Name) VALUES (@Name)";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Name", newValue);
                    cmd.ExecuteNonQuery();
                }
            }

            // Log to Mast_MultiData
            _auditService.LogAuditHistory(tableName, recordId, "INSERT", null, newValue, modifiedBy);
        }

        // Example: Update
        public void UpdateRecord(string dbKey, string tableName, string recordId, string oldValue, string newValue, string modifiedBy)
        {
            var connStr = GetConnectionString(dbKey);

            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();

                var query = $"UPDATE {tableName} SET Name = @NewValue WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@NewValue", newValue);
                    cmd.Parameters.AddWithValue("@Id", recordId);
                    cmd.ExecuteNonQuery();
                }
            }

            // Log to Mast_MultiData
            _auditService.LogAuditHistory(tableName, recordId, "UPDATE", oldValue, newValue, modifiedBy);
        }

        // Example: Delete
        public void DeleteRecord(string dbKey, string tableName, string recordId, string oldValue, string modifiedBy)
        {
            var connStr = GetConnectionString(dbKey);

            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();

                var query = $"DELETE FROM {tableName} WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", recordId);
                    cmd.ExecuteNonQuery();
                }
            }

            // Log to Mast_MultiData
            _auditService.LogAuditHistory(tableName, recordId, "DELETE", oldValue, null, modifiedBy);
        }
    }
}

