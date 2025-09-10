

using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MULTI___DATABASE_MANAGEMENT_PORTAL.Services
{
    public class AuditService
    {
        private readonly string _auditConnectionString;

        public AuditService(IConfiguration configuration)
        {
            // Connection string for Mast_MultiData
            _auditConnectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public void LogAuditHistory(string tableName, string recordId, string operationType,
                                    string oldValue, string newValue, string modifiedBy)
        {
            using (var connection = new SqlConnection(_auditConnectionString))
            {
                using (var command = new SqlCommand("sp_LogAuditHistory", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@TableName", tableName);
                    command.Parameters.AddWithValue("@RecordId", recordId);
                    command.Parameters.AddWithValue("@OperationType", operationType);
                    command.Parameters.AddWithValue("@OldValue", (object)oldValue ?? DBNull.Value);
                    command.Parameters.AddWithValue("@NewValue", (object)newValue ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ModifiedBy", modifiedBy);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}

