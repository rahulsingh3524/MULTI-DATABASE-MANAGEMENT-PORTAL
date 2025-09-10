using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using MULTI___DATABASE_MANAGEMENT_PORTAL.Models;
using MULTI___DATABASE_MANAGEMENT_PORTAL.Services;

namespace MULTI___DATABASE_PORTAL.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DatabaseHelper _databaseHelper;
        private readonly CookieService _cookieService;  
        public LoginDetail logindata;

        public HomeController(ILogger<HomeController> logger, DatabaseHelper databaseHelper, CookieService cookieService)
        {
            _logger = logger;
            _databaseHelper = databaseHelper;
            logindata = new LoginDetail();
            _cookieService = cookieService;
        }

        // Main page: select database, then table

        public IActionResult Index()
        {
            var cookieDict = _cookieService.GetDictionaryFromCookie("UI");
            if (cookieDict == null || !cookieDict.ContainsKey(logindata.Id))
                return RedirectToAction("Login", "Login");
            bool isadmin = Convert.ToBoolean(DatabaseHelper.Decrypt(cookieDict[logindata.IsAdmin]));
            var dbList = GetDatabaseList() ?? new List<DatabaseInfo>(); // now only active dbs
            ViewBag.Databases = dbList;
            ViewBag.IsAdmin = isadmin;
            ViewBag.ActiveCount = ViewBag.IsAdmin ? dbList.Count : 0;
            return View();
        }


        // Get tables for selected database (AJAX)
        [HttpPost]
        public IActionResult GetTables(int dbid)
        {
            var tables = GetTablesForDatabase(dbid);
            HttpContext.Session.SetString("SelectedDbId", dbid.ToString());
            return Json(tables);
        }

        // Load all databases from DB via stored proc
        //public List<DatabaseInfo> GetDatabaseList()
        //{
        //    var result = _databaseHelper.ExecuteStoredProcedure("SP_GetAllDBStrings", null);

        //    return result.Select(r => new DatabaseInfo
        //    {
        //        DBID = r.ContainsKey("ID") ? Convert.ToInt32(r["ID"]) : 0,
        //        DBName = r.ContainsKey("DBName") ? r["DBName"]?.ToString() : "",
        //        ConnectionString = r.ContainsKey("DBConnString") ? r["DBConnString"]?.ToString() : "",
        //        IsActive = r.ContainsKey("Is_Active") && (r["Is_Active"] != null) ? Convert.ToBoolean(r["Is_Active"]) : false
        //    }).ToList();
        //}

        public List<DatabaseInfo> GetDatabaseList()
        {
            var result = _databaseHelper.ExecuteStoredProcedure("SP_GetAllDBStrings", null);

            var allDatabases = result.Select(r => new DatabaseInfo
            {
                DBID = r.ContainsKey("ID") ? Convert.ToInt32(r["ID"]) : 0,
                DBName = r.ContainsKey("DBName") ? r["DBName"]?.ToString() : "",
                ConnectionString = r.ContainsKey("DBConnString") ? r["DBConnString"]?.ToString() : "",
                IsActive = r.ContainsKey("Is_Active") && r["Is_Active"] != null && Convert.ToBoolean(r["Is_Active"])
            }).ToList();

            // Return only active databases
            return allDatabases.Where(db => db.IsActive).ToList();
        }

        // Get tables for selected DB
        public List<TableInfo> GetTablesForDatabase(int dbid)
        {
            //var tables = new List<TableInfo>();

            //tables = _databaseHelper.ExecuteSqlQueryWithConnection(dbid, "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'");


            var databases = GetDatabaseList();
            var db = databases.FirstOrDefault(d => d.DBID == dbid);
            if (db == null) return new List<TableInfo>();
            var tables = new List<TableInfo>();
       
            try
            {
                //using (var conn = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=database1;TrustServerCertificate=False;"))
                //{
                //    conn.Open();
                //    Console.WriteLine("Success!");
                //    conn.Close();
                //}

                //var constr = db.ConnectionString;
                //Console.WriteLine("Trying connection string: " + constr);

                //var constr = db.ConnectionString;
                //var hardcoded = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=database1;TrustServerCertificate=False;";
                //Console.WriteLine("Hardcoded bytes: " + string.Join(",", hardcoded.Select(c => (int)c)));
                //Console.WriteLine("DB String bytes: " + string.Join(",", constr.Select(c => (int)c)));

                tables = _databaseHelper.GetTables(dbid);

                //using (var conn = new SqlConnection(db.ConnectionString))
                //{
                //    conn.Open();
                //    using (var cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", conn))
                //    using (var reader = cmd.ExecuteReader())
                //    {
                //        while (reader.Read())
                //        {
                //            tables.Add(new TableInfo { TableName = reader.GetString(0) });
                //        }
                //    }
                //}
            }
            catch (Exception ex)
            {
                // Log or throw with more context
                throw new Exception($"Connection failed to DB '{db.DBName}' with conn string '{db.ConnectionString}'", ex);
            }
            return tables;
        }


        [HttpPost]
        public IActionResult DeactivateDatabase(int dbId)
        {
            if (!IsUserAdmin())
                return Unauthorized();

            var parameters = new SqlParameter[]
            {
        new SqlParameter("@ID", dbId),
        new SqlParameter("@IsActive", false)
            };

            // Assuming you have a stored procedure to update IsActive flag
            _databaseHelper.ExecuteStoredProcedure("sp_UpdateDatabaseStatus", parameters);

            return RedirectToAction("ManageDatabases");
        }

        [HttpPost]
        public IActionResult ActivateDatabase(int dbId)
        {
            if (!IsUserAdmin())
                return Unauthorized();

            var parameters = new SqlParameter[]
            {
        new SqlParameter("@ID", dbId),
        new SqlParameter("@IsActive", true)
            };

            _databaseHelper.ExecuteStoredProcedure("sp_UpdateDatabaseStatus", parameters);

            return RedirectToAction("ManageDatabases");
        }

        [HttpPost]
        public IActionResult SearchDatabase(int dbId, string search)
        {
            var database = GetDatabaseList().FirstOrDefault(d => d.DBID == dbId);
            if (database == null || string.IsNullOrEmpty(search))
                return Json(new List<object>());

            var results = new List<object>();

            using (var conn = new SqlConnection(database.ConnectionString))
            {
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE DATA_TYPE IN ('varchar', 'nvarchar', 'char', 'nchar', 'text', 'ntext')
        ";

                var reader = cmd.ExecuteReader();
                var columns = new List<(string Schema, string Table, string Column)>();

                while (reader.Read())
                {
                    columns.Add((
                        reader.GetString(0), // schema
                        reader.GetString(1), // table
                        reader.GetString(2)  // column
                    ));
                }

                reader.Close();

                foreach (var col in columns)
                {
                    try
                    {
                        var sql = $@"
                    SELECT TOP 50 '{col.Table}' AS TableName, '{col.Column}' AS ColumnName, CAST([{col.Column}] AS NVARCHAR(MAX)) AS MatchedValue
                    FROM [{col.Schema}].[{col.Table}]
                    WHERE [{col.Column}] LIKE @search
                ";

                        var searchCmd = conn.CreateCommand();
                        searchCmd.CommandText = sql;
                        searchCmd.Parameters.AddWithValue("@search", $"%{search}%");

                        using (var r = searchCmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                results.Add(new
                                {
                                    tableName = r["TableName"].ToString(),
                                    columnName = r["ColumnName"].ToString(),
                                    matchedValue = r["MatchedValue"].ToString()
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // log or ignore problematic columns (binary, etc.)
                    }
                }

                conn.Close();
            }

            return Json(results);
        }



        // Admin only page for managing databases
        public IActionResult ManageDatabases()
        {
            if (!IsUserAdmin())
                return Unauthorized();

            var databases = GetDatabaseList();
            ViewBag.ActiveCount = databases.Count(d => d.IsActive);
            return View(databases);
        }

        // Add new database - Admin only
        [HttpPost]
        public IActionResult AddDatabase(string DBName, string DBConnString)
        {
            if (!IsUserAdmin())
                return Unauthorized();

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@DBName", DBName),
                new SqlParameter("@DBConnString", DBConnString),
                new SqlParameter("@IsActive", true)
            };

            _databaseHelper.ExecuteStoredProcedure("sp_AddDatabase", parameters);

            return RedirectToAction("ManageDatabases");
        }

        // Replace with your actual admin check logic
        private bool IsUserAdmin()
        {
            var cookieDict = _cookieService.GetDictionaryFromCookie("UI");
            bool isadmin = Convert.ToBoolean(DatabaseHelper.Decrypt(cookieDict[logindata.IsAdmin]));
            return isadmin;
        }

    

       

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
