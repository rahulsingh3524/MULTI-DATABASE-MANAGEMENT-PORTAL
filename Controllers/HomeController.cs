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

        public HomeController(ILogger<HomeController> logger, DatabaseHelper databaseHelper)
        {
            _logger = logger;
            _databaseHelper = databaseHelper;
        }

        // Main page: select database, then table
       
        public IActionResult Index()
        {
            var dbList = GetDatabaseList() ?? new List<DatabaseInfo>(); // now only active dbs
            ViewBag.Databases = dbList;
            ViewBag.IsAdmin = IsUserAdmin();
            ViewBag.ActiveCount = ViewBag.IsAdmin ? dbList.Count : 0;
            return View();
        }


        // Get tables for selected database (AJAX)
        [HttpPost]
        public IActionResult GetTables(int dbid)
        {
            var tables = GetTablesForDatabase(dbid);
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



                using (var conn = new SqlConnection(db.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tables.Add(new TableInfo { TableName = reader.GetString(0) });
                        }
                    }
                }
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
            // Example: Check user identity, claims, or cookie session for admin flag.
            return true;
        }

        // Models
        public class DatabaseInfo
        {
            public int DBID { get; set; }
            public string DBName { get; set; }
            public string ConnectionString { get; set; }
            public bool IsActive { get; set; }
        }

        public class TableInfo
        {
            public string TableName { get; set; }
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
