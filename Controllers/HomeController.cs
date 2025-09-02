using System.Data.SqlClient;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MULTI___DATABASE_MANAGEMENT_PORTAL.Models;
using MULTI___DATABASE_MANAGEMENT_PORTAL.Services;
using Newtonsoft.Json;


namespace MULTI___DATABASE_MANAGEMENT_PORTAL.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public readonly DatabaseHelper _databaseHelper;

        public HomeController(ILogger<HomeController> logger, DatabaseHelper databaseHelper)
        {
            _logger = logger;
            _databaseHelper = databaseHelper;
        }

        public IActionResult Index()
        {
            // Example: Fetch list of databases from config or service
            ViewBag.Databases = GetDatabaseList(); // [{ DBID = 1, DBName = "Support Database" }, ...]
            ViewBag.Tables = null; // Initially null
            return View();
        }

        [HttpPost]
        public IActionResult GetTables(int dbid)
        {
            // Fetch table list for selected DBID
            var tables = GetTableListForDatabase(dbid); // [{ TableName = "DB_Table_1" }, ...]
            return Json(tables);
        }

        public List<DatabaseInfo> GetDatabaseList()
        {
            //var json = File.ReadAllText("connStringContext.json");
            var json = System.IO.File.ReadAllText("connStringContext.json");
            var databases = JsonConvert.DeserializeObject<ConnStringContext>(json);
            return databases.Databases; // assuming root object has a "Databases" property that's a list
        }


      

    public List<TableInfo> GetTableListForDatabase(int dbid)
    {
        // Get list of all databases from config
        List<DatabaseInfo> dbList = GetDatabaseList();
        var db = dbList.FirstOrDefault(d => d.DBID == dbid);
        if (db == null) return new List<TableInfo>();

        var tables = new List<TableInfo>();

            var result = _databaseHelper.ExecuteSqlQueryWithConnection();


        using (var conn = new SqlConnection(db.ConnectionString))
        {
            conn.Open();
            // Query for user tables
            using (var cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    tables.Add(new TableInfo { TableName = reader.GetString(0) });
                }
            }
        }
        return tables;
    }

    public class TableInfo
    {
        public string TableName { get; set; }
}



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
