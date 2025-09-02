using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
        private readonly string _connStringsConfigPath;
        // Use secure key/iv in production, ideally via environment variables or secure storage!
        private static readonly string EncryptionKey = "zQ5nD7pRf3KwL8tVeG0aY2uXiJ6vG4Nb"; // 32 chars  for AES-256
        private static readonly string IVString = "bXc9vYt5rUe2tO7k"; // 16 chars for AES
        private readonly string _connectionString;

        // Constructor: IConfiguration injected, config path set
        public DatabaseHelper(IConfiguration configuration, string connStringsConfigPath)
        {
            _configuration = configuration;
            _connStringsConfigPath = connStringsConfigPath;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Loads connection string for a given DBID from the JSON config
        public string GetConnectionStringFromContext(string dbId)
        {
            var json = File.ReadAllText(_connStringsConfigPath);
            var context = JsonConvert.DeserializeObject<DbConnectionContext>(json);
            return context.ConnectionStrings.ContainsKey(dbId) ? context.ConnectionStrings[dbId] : null;
        }

        // Executes a stored procedure using DBID (high-level API)
        public List<Dictionary<string, object>> ExecuteStoredProcedureWithDbId(string dbId, string spName, SqlParameter[] parameters)
        {
            var connStr = GetConnectionStringFromContext(dbId);
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

        public List<Dictionary<string, object>> ExecuteSqlQueryWithConnection(string dbId, string sqlQuery, SqlParameter[] parameters = null)
        {
            var connectionString = GetConnectionStringFromContext(dbId);
            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("Connection string not found for DBID: " + dbId);

            var result = new List<Dictionary<string, object>>();

            using (var conn = new SqlConnection(connectionString))
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


    }
}