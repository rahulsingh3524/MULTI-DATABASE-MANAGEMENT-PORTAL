using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using MULTI___DATABASE_MANAGEMENT_PORTAL.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace MULTI___DATABASE_MANAGEMENT_PORTAL.Services

{ 
    // Models the structure of the connection strings JSON
    public class DbConnectionContext
    {
        public Dictionary<string, string> ConnectionStrings { get; set; }
    }

    public class DatabaseHelper
    {
        private readonly IConfiguration _configuration;
        //private readonly string _connStringsConfigPath;
        // Use secure key/iv in production, ideally via environment variables or secure storage!
        private static readonly string EncryptionKey = "zQ5nD7pRf3KwL8tVeG0aY2uXiJ6vG4Nb"; // 32 chars  for AES-256
        private static readonly string IVString = "bXc9vYt5rUe2tO7k"; // 16 chars for AES
        private readonly string _connectionString;

        // Constructor: IConfiguration injected, config path set
        public DatabaseHelper(IConfiguration configuration, string connStringsConfigPath)
        {
            _configuration = configuration;
            //_connStringsConfigPath = connStringsConfigPath;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Loads connection string for a given DBID from the JSON config
        //public string GetConnectionStringFromContext(string dbId)
        //{
        //    var json = File.ReadAllText(_connStringsConfigPath);
        //    var context = JsonConvert.DeserializeObject<DbConnectionContext>(json);
        //    return context.ConnectionStrings.ContainsKey(dbId) ? context.ConnectionStrings[dbId] : null;
        //}

        // Executes a stored procedure using DBID (high-level API)
        public List<Dictionary<string, object>> ExecuteStoredProcedureWithDbId(string dbId, string spName, SqlParameter[] parameters)
        {
  var parameter = new SqlParameter[]
{
    new SqlParameter("@dbid", dbId)
};

var result = ExecuteStoredProcedure("API_GetDBString", parameter);

string connStr = null;
if (result.Count > 0 && result[0].ContainsKey("DBConnString"))
{
    connStr = result[0]["DBConnString"]?.ToString();
}

if (string.IsNullOrEmpty(connStr))
    throw new Exception("Connection string not found for DBID: " + dbId);

            return ExecuteStoredProcedureWithConnectionString(connStr, spName, parameters);
        }

        public List<Dictionary<string, object>> ExecuteStoredProcedure(string spName, SqlParameter[] parameters)
        {
            var result = new List<Dictionary<string, object>>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(spName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            result.Add(row);
                        }
                    }
                }
            }
            return result;
        }


        // Executes SP using provided connection string (low-level API)
        public List<Dictionary<string, object>> ExecuteStoredProcedureWithConnectionString(string connectionString, string spName, SqlParameter[] parameters)
        {
            var result = new List<Dictionary<string, object>>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(spName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        result.Add(row);
                    }
                }
            }
            return result;
        }

        // 2. Encrypt a string (returns Base64)
        public static string Encrypt(string plainText)
        {
            byte[] key = Encoding.UTF8.GetBytes(EncryptionKey);
            byte[] iv = Encoding.UTF8.GetBytes(IVString);

            if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes for AES-256.");
            if (iv.Length != 16) throw new ArgumentException("IV must be 16 bytes.");

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    // ms.ToArray() is guaranteed to have complete data now
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }


        // 3. Decrypt a Base64 string (returns plaintext)
        public static string Decrypt(string cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                aes.IV = Encoding.UTF8.GetBytes(IVString);

                ICryptoTransform decryptor = aes.CreateDecryptor();
                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        public List<Dictionary<string, object>> ExecuteSqlQueryWithDbid(string dbId, string sqlQuery, SqlParameter[] parameters = null)
        {
            var parameter = new SqlParameter[]
{
    new SqlParameter("@dbid", dbId)
};

            var resultC = ExecuteStoredProcedure("API_GetDBString", parameter);

            string connStr = null;
            if (resultC.Count > 0 && resultC[0].ContainsKey("DBConnString"))
            {
                connStr = resultC[0]["DBConnString"]?.ToString();
            }

            if (string.IsNullOrEmpty(connStr))
                throw new Exception("Connection string not found for DBID: " + dbId);
            

            var result = new List<Dictionary<string, object>>();

            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(sqlQuery, conn))
            {
                cmd.CommandType = CommandType.Text; // Indicates raw SQL query

                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        result.Add(row);
                    }
                }
            }

            return result;
        }

        public DataTable ConvertToDataTable(List<Dictionary<string, object>> list)
        {
            var dataTable = new DataTable();

            if (list == null || list.Count == 0)
                return dataTable;

            // Create columns
            foreach (var key in list[0].Keys)
            {
                dataTable.Columns.Add(key);
            }

            // Add rows
            foreach (var dict in list)
            {
                var row = dataTable.NewRow();
                foreach (var key in dict.Keys)
                {
                    row[key] = dict[key] ?? DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        public int GetRowCount(int dbid, string tableName, string whereClause = "", SqlParameter[] parameters = null)
        {
            var parameter = new SqlParameter[]
    {
         new SqlParameter("@dbid", dbid)
    };

            var resultC = ExecuteStoredProcedure("API_GetDBString", parameter);

            string connStr = null;
            if (resultC.Count > 0 && resultC[0].ContainsKey("DBConnString"))
            {
                connStr = resultC[0]["DBConnString"]?.ToString();
            }

            if (string.IsNullOrEmpty(connStr))
                throw new Exception("Connection string not found for DBID: " + dbid);


            string query = $"SELECT COUNT(*) FROM [{tableName}] {whereClause}";
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    conn.Open();
                    return (int)cmd.ExecuteScalar();
                }
            }
        }
        public bool TableExists(int dbid, string tableName)
        {
            var parameter = new SqlParameter[]
    {
         new SqlParameter("@dbid", dbid)
    };

            var resultC = ExecuteStoredProcedure("API_GetDBString", parameter);

            string connStr = null;
            if (resultC.Count > 0 && resultC[0].ContainsKey("DBConnString"))
            {
                connStr = resultC[0]["DBConnString"]?.ToString();
            }

            if (string.IsNullOrEmpty(connStr))
                throw new Exception("Connection string not found for DBID: " + dbid);


            string query = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName";
            var parameters = new SqlParameter[] { new SqlParameter("@tableName", tableName) };
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parameters);
                    conn.Open();
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public bool ColumnExists(int dbid, string tableName, string columnName)
        {
            var parameter = new SqlParameter[]
    {
         new SqlParameter("@dbid", dbid)
    };

            var resultC = ExecuteStoredProcedure("API_GetDBString", parameter);

            string connStr = null;
            if (resultC.Count > 0 && resultC[0].ContainsKey("DBConnString"))
            {
                connStr = resultC[0]["DBConnString"]?.ToString();
            }

            if (string.IsNullOrEmpty(connStr))
                throw new Exception("Connection string not found for DBID: " + dbid);


            string query = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName";
            var parameters = new SqlParameter[] {
                new SqlParameter("@tableName", tableName),
                new SqlParameter("@columnName", columnName)
            };
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parameters);
                    conn.Open();
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public bool IsIdentityColumn(int dbid, string tableName, string columnName)
        {
            var parameter = new SqlParameter[]
               {
                    new SqlParameter("@dbid", dbid)
               };

            var resultC = ExecuteStoredProcedure("API_GetDBString", parameter);

            string connStr = null;
            if (resultC.Count > 0 && resultC[0].ContainsKey("DBConnString"))
            {
                connStr = resultC[0]["DBConnString"]?.ToString();
            }

            if (string.IsNullOrEmpty(connStr))
                throw new Exception("Connection string not found for DBID: " + dbid);


            string query = "SELECT COLUMNPROPERTY(OBJECT_ID(@tableName), @columnName, 'IsIdentity')";
            var parameters = new SqlParameter[] {
                new SqlParameter("@tableName", tableName),
                new SqlParameter("@columnName", columnName)
            };
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parameters);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return result != DBNull.Value && Convert.ToInt32(result) == 1;
                }
            }
        }

        public List<TableInfo> GetTables(int dbid)
        {
            // Get connection string from DB ID
            var parameter = new SqlParameter[]
            {
        new SqlParameter("@dbid", dbid)
            };

            var resultC = ExecuteStoredProcedure("API_GetDBString", parameter);

            string connStr = null;
            if (resultC.Count > 0 && resultC[0].ContainsKey("DBConnString"))
            {
                connStr = resultC[0]["DBConnString"]?.ToString();
            }

            if (string.IsNullOrEmpty(connStr))
                throw new Exception("Connection string not found for DBID: " + dbid);

            var tables = new List<TableInfo>();
            string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

            try
            {
                using (var connection = new SqlConnection(connStr))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tables.Add(new TableInfo
                                {
                                    TableName = reader["TABLE_NAME"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting table names: {ex.Message}");
            }

            return tables;
        }

        public List<string> GetTableColumns(int dbid, string tableName)
        {
            var parameter = new SqlParameter[]
    {
         new SqlParameter("@dbid", dbid)
    };

            var resultC = ExecuteStoredProcedure("API_GetDBString", parameter);

            string connStr = null;
            if (resultC.Count > 0 && resultC[0].ContainsKey("DBConnString"))
            {
                connStr = resultC[0]["DBConnString"]?.ToString();
            }

            if (string.IsNullOrEmpty(connStr))
                throw new Exception("Connection string not found for DBID: " + dbid);


            var columns = new List<string>();
            string query = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName ORDER BY ORDINAL_POSITION";
            var parameters = new SqlParameter[] { new SqlParameter("@tableName", tableName) };

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddRange(parameters);
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                columns.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting table columns: {ex.Message}");
            }
            return columns;
        }

        public string GetPrimaryKeyColumn(int dbid, string tableName)
        {
            string query = @"SELECT C.COLUMN_NAME
                             FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS T
                             JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE C
                               ON T.CONSTRAINT_NAME = C.CONSTRAINT_NAME
                             WHERE T.CONSTRAINT_TYPE = 'PRIMARY KEY' AND T.TABLE_NAME = @tableName";
            var parameters = new SqlParameter[] { new SqlParameter("@tableName", tableName) };
            var rawdataTable = ExecuteSqlQueryWithDbid(dbid.ToString(), query, parameters);
            var dataTable = ConvertToDataTable(rawdataTable);
            if (dataTable.Rows.Count > 0)
            {
                return dataTable.Rows[0]["COLUMN_NAME"].ToString();
            }
            return null;
        }
        public void ExecuteNonQuery(int dbid, string query, SqlParameter[] parameters = null)
        {
            var parameter = new SqlParameter[]
{
         new SqlParameter("@dbid", dbid)
};

            var resultC = ExecuteStoredProcedure("API_GetDBString", parameter);

            string connStr = null;
            if (resultC.Count > 0 && resultC[0].ContainsKey("DBConnString"))
            {
                connStr = resultC[0]["DBConnString"]?.ToString();
            }

            if (string.IsNullOrEmpty(connStr))
                throw new Exception("Connection string not found for DBID: " + dbid);

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error executing non-query: {ex.Message}");
                        throw; // Re-throw the exception to be handled by the calling code.
                    }
                }
            }
        }


    }
}