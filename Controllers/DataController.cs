


using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MULTI___DATABASE_MANAGEMENT_PORTAL.Services;
using MULTI___DATABASE_MANAGEMENT_PORTAL.Models;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;


namespace MULTI___DATABASE_MANAGEMENT_PORTAL.Controllers
{
    public class DataController : Controller
    {
        private readonly DatabaseService _databaseService;
        private readonly CookieService _cookieService;
        private readonly DatabaseHelper _databaseHelper; // *** ADDED
        private readonly AuditService _auditService; // *** ADDED
        public LoginDetail logindata = new LoginDetail();

        // Add DatabaseHelper to constructor parameters and assign
        public DataController(DatabaseService databaseService, CookieService cookieService, AuditService auditService, DatabaseHelper databaseHelper)
        {
            _databaseService = databaseService;
            _cookieService = cookieService;
            _auditService = auditService;
            _databaseHelper = databaseHelper; 
            logindata = new LoginDetail();
        }


        //private DatabaseHelper GetDatabaseHelper()
        //{
        //    var databaseName = Request.Cookies["SelectedDatabase"];
        //    if (string.IsNullOrEmpty(databaseName))
        //    {
        //        return null;
        //    }
        //    var connectionString = _databaseService.GetConnectionString(databaseName);
        //    return new DatabaseHelper(connectionString);
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



        // Helper method to retrieve the username from the cookie
        private string GetCurrentUserName()
        {
            var cookieDict = _cookieService.GetDictionaryFromCookie("UI");
            if (cookieDict != null && cookieDict.ContainsKey("Username"))
            {
                string encryptedUsername = cookieDict["Username"];
                return DatabaseHelper.Decrypt(encryptedUsername);
            }
            return "Anonymous"; // Fallback for safety
        }

        public IActionResult ViewTable(string dbid, string tableName, string searchColumn, string searchOperator, string searchValue, string searchValue2, int page = 1, int pageSize = 10, string sortColumn = null, string sortOrder = null)
        {
            var cookieDict = _cookieService.GetDictionaryFromCookie("UI");
            if (cookieDict == null || !cookieDict.ContainsKey(logindata.Id))
                return RedirectToAction("Login", "Login");


            if (dbid == null)
            {
                dbid = HttpContext.Session.GetString("SelectedDbId");
                if (dbid == null) return RedirectToAction("Index", "Home");
                HttpContext.Session.SetString("SelectedDbId", dbid);
            }
            //var istheretable = _databaseHelper.TableExists(Convert.ToInt32(dbid), tableName);
            //if (!istheretable)
            //{
            //    return BadRequest("Invalid table name.");
            //}
            //if (!string.IsNullOrEmpty(searchColumn) && !_databaseHelper.ColumnExists(Convert.ToInt32(dbid), tableName, searchColumn))
            //{
            //    return BadRequest("Invalid search column.");
            //}
            //if (!string.IsNullOrEmpty(sortColumn) && !_databaseHelper.ColumnExists(Convert.ToInt32(dbid), tableName, sortColumn))
            //{
            //    return BadRequest("Invalid sort column.");
            //}

            string baseQuery = $"SELECT * FROM [{tableName}]";
            string whereClause = "";
            var parameters = new List<SqlParameter>();
            var dataTable = new DataTable();

            if (!string.IsNullOrEmpty(searchColumn) && !string.IsNullOrEmpty(searchOperator))
            {
                switch (searchOperator)
                {
                    case "DISTINCT":
                        string distinctQuery = $"SELECT DISTINCT [{searchColumn}] FROM [{tableName}]";
                        //dataTable = dbHelper.ExecuteQuery(distinctQuery);
                        var rawdataTable1 = _databaseHelper.ExecuteSqlQueryWithDbid(dbid, distinctQuery);
                        dataTable = _databaseHelper.ConvertToDataTable(rawdataTable1);
                        ViewBag.TotalPages = 1;
                        ViewBag.CurrentPage = 1;
                        return View(dataTable);

                    case "BETWEEN":
                        if (!string.IsNullOrEmpty(searchValue) && !string.IsNullOrEmpty(searchValue2))
                        {
                            whereClause += $" WHERE [{searchColumn}] BETWEEN @searchValue1 AND @searchValue2";
                            parameters.Add(new SqlParameter("@searchValue1", searchValue));
                            parameters.Add(new SqlParameter("@searchValue2", searchValue2));
                        }
                        break;

                    case "LIKE":
                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            whereClause += $" WHERE [{searchColumn}] LIKE @searchValue";
                            parameters.Add(new SqlParameter("@searchValue", $"%{searchValue}%"));
                        }
                        break;

                    default:
                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            whereClause += $" WHERE [{searchColumn}] {searchOperator} @searchValue";
                            parameters.Add(new SqlParameter("@searchValue", searchValue));
                        }
                        break;
                }
            }

            // --- FIX: Create a new array to pass to GetRowCount ---
            int totalRecords = _databaseHelper.GetRowCount(Convert.ToInt32(dbid), tableName, whereClause, parameters.Select(p => new SqlParameter(p.ParameterName, p.Value)).ToArray());
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            string orderByClause = string.Empty;
            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortOrder))
            {
                orderByClause = $" ORDER BY [{sortColumn}] {(sortOrder.ToLower() == "desc" ? "DESC" : "ASC")}";
            }
            else
            {
                orderByClause = $" ORDER BY (SELECT NULL)";
            }

            string paginationClause = $" OFFSET {(page - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            string finalQuery = baseQuery + whereClause + orderByClause + paginationClause;

            // --- FIX: Create a new array to pass to ExecuteQuery ---
            var rawdataTable = _databaseHelper.ExecuteSqlQueryWithDbid(dbid, finalQuery, parameters.Select(p => new SqlParameter(p.ParameterName, p.Value)).ToArray());
            dataTable = _databaseHelper.ConvertToDataTable(rawdataTable);

            ViewBag.TableName = tableName;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;
            ViewBag.SearchColumn = searchColumn;
            ViewBag.SearchOperator = searchOperator;
            ViewBag.SearchValue = searchValue;
            ViewBag.SearchValue2 = searchValue2;

            return View(dataTable);
        }

        [HttpPost]
        public IActionResult SearchTable(string tableName, string searchColumn, string searchOperator, string searchValue, string searchValue2)
        {
            return RedirectToAction("ViewTable", new { tableName, searchColumn, searchOperator, searchValue, searchValue2 });
        }

        public IActionResult Create(string tableName)
        {
            var dbid = HttpContext.Session.GetString("SelectedDbId");
            if (dbid == null) return RedirectToAction("Index", "Home");
            HttpContext.Session.SetString("SelectedDbId", dbid);

            // --- VULNERABILITY FIX: VALIDATE tableName ---
            if (!_databaseHelper.TableExists(Convert.ToInt32(dbid), tableName))
            {
                return BadRequest("Invalid table name.");
            }

            var columns = _databaseHelper.GetTableColumns(Convert.ToInt32(dbid), tableName);
            ViewBag.TableName = tableName;
            return View(columns);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Dictionary<string, string> form)
        {
            var dbid = HttpContext.Session.GetString("SelectedDbId");
            if (dbid == null) return RedirectToAction("Index", "Home");
            HttpContext.Session.SetString("SelectedDbId", dbid);

            string tableName = form["tableName"];
            var formForNewValue = new Dictionary<string, string>(form); // Copy to preserve original
            form.Remove("tableName");

            // --- VULNERABILITY FIX: VALIDATE tableName ---
            if (!_databaseHelper.TableExists(Convert.ToInt32(dbid), tableName))
            {
                return BadRequest("Invalid table name.");
            }

            var tableColumns = _databaseHelper.GetTableColumns(Convert.ToInt32(dbid), tableName);
            if (tableColumns.Count == 0) return BadRequest("Table not found or has no columns.");

            var columns = new List<string>();
            var values = new List<string>();
            var parameters = new List<SqlParameter>();

            foreach (var kvp in form)
            {
                // --- LOGIC FIX: USE IsIdentityColumn METHOD TO SKIP IDENTITY COLUMN ---
                if (_databaseHelper.IsIdentityColumn(Convert.ToInt32(dbid), tableName, kvp.Key))
                {
                    continue;
                }

                // --- VULNERABILITY FIX: VALIDATE COLUMN NAME ---
                if (!_databaseHelper.ColumnExists(Convert.ToInt32(dbid), tableName, kvp.Key))
                {
                    continue;
                }

                columns.Add($"[{kvp.Key}]");
                values.Add($"@{kvp.Key}");
                parameters.Add(new SqlParameter($"@{kvp.Key}", kvp.Value));
            }

            if (columns.Count == 0)
            {
                return BadRequest("No valid columns to insert.");
            }

            string query = $"INSERT INTO [{tableName}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";

            string keyColumn = _databaseHelper.GetPrimaryKeyColumn(Convert.ToInt32(dbid), tableName);
            string id = formForNewValue.ContainsKey(keyColumn) ? formForNewValue[keyColumn] : "N/A";
            string currentUserName = GetCurrentUserName();

            try
            {
                _databaseHelper.ExecuteNonQuery(Convert.ToInt32(dbid), query, parameters.ToArray());

                // --- ADD LOGGING FOR CREATE OPERATION ---
                string newValue = JsonSerializer.Serialize(formForNewValue);
                //dbHelper.LogAuditHistory(tableName, id, "ADD", null, newValue, currentUserName);
                _auditService.LogAuditHistory(tableName, id, "ADD", null, newValue, currentUserName); // *** CHANGED

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during insert: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Error inserting data." });
            }
        }

        [HttpPost]
        public IActionResult Delete(string tableName, string id, string keyColumn)
        {
            var dbid = HttpContext.Session.GetString("SelectedDbId");
            if (dbid == null) return RedirectToAction("Index", "Home");
            HttpContext.Session.SetString("SelectedDbId", dbid);
            

            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(keyColumn))
            {
                return BadRequest("Missing required parameters.");
            }

            // --- VULNERABILITY FIX: VALIDATE tableName AND keyColumn ---
            if (!_databaseHelper.TableExists(Convert.ToInt32(dbid), tableName) || !_databaseHelper.ColumnExists(Convert.ToInt32(dbid), tableName, keyColumn))
            {
                return BadRequest("Invalid table or key column.");
            }

            // --- ADD LOGGING FOR DELETE OPERATION ---
            // 1. Retrieve the record data before deletion.
            string oldQuery = $"SELECT * FROM [{tableName}] WHERE [{keyColumn}] = @id";
            var oldParameters = new SqlParameter[] { new SqlParameter("@id", id) };
            var oldData = _databaseHelper.ExecuteSqlQueryWithDbid(dbid, oldQuery, oldParameters);
            string oldValue = oldData.Any() ? JsonSerializer.Serialize(oldData.First()) : null;

            string query = $"DELETE FROM [{tableName}] WHERE [{keyColumn}] = @id";
            var parameters = new SqlParameter[] { new SqlParameter("@id", id) };

            string currentUserName = GetCurrentUserName();

            try
            {
                _databaseHelper.ExecuteNonQuery(Convert.ToInt32(dbid), query, parameters);

                // 2. Log the deleted data.
                //dbHelper.LogAuditHistory(tableName, id, "DELETE", oldValue, null, currentUserName);
                _auditService.LogAuditHistory(tableName, id, "DELETE", oldValue, null, currentUserName); // *** CHANGED

                return RedirectToAction("ViewTable", new { tableName });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during delete: {ex.Message}");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost]
        public IActionResult Edit([FromBody] Dictionary<string, string> form)
        {
            string tableName = form["tableName"];
            string keyColumn = form["keyColumn"];
            string id = form["id"];

            var formForNewValue = new Dictionary<string, string>(form);

            form.Remove("tableName");
            form.Remove("keyColumn");
            form.Remove("id");

            var setClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            // --- VULNERABILITY FIX: VALIDATE tableName AND keyColumn ---
            var dbid = HttpContext.Session.GetString("SelectedDbId");
            if (dbid == null) return RedirectToAction("Index", "Home");
            HttpContext.Session.SetString("SelectedDbId", dbid);

            if (!_databaseHelper.TableExists(Convert.ToInt32(dbid), tableName) || !_databaseHelper.ColumnExists(Convert.ToInt32(dbid), tableName, keyColumn))
            {
                return BadRequest("Invalid table or key column.");
            }

            // --- ADD LOGGING FOR EDIT OPERATION ---
            // 1. Retrieve the old record data before the update.
            string oldQuery = $"SELECT * FROM [{tableName}] WHERE [{keyColumn}] = @id";
            var oldParameters = new SqlParameter[] { new SqlParameter("@id", id) };
            var oldData = _databaseHelper.ExecuteSqlQueryWithDbid(dbid, oldQuery, oldParameters);
            string oldValue = oldData.Any() ? JsonSerializer.Serialize(oldData.First()) : null;

            foreach (var kvp in form)
            {
                if (string.Equals(kvp.Key, keyColumn, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // --- VULNERABILITY FIX: VALIDATE COLUMN NAME ---
                if (!_databaseHelper.ColumnExists(Convert.ToInt32(dbid), tableName, kvp.Key))
                {
                    continue;
                }

                setClauses.Add($"[{kvp.Key}] = @{kvp.Key}");
                parameters.Add(new SqlParameter($"@{kvp.Key}", kvp.Value));
            }

            parameters.Add(new SqlParameter($"@{keyColumn}", id));

            string query = $"UPDATE [{tableName}] SET {string.Join(", ", setClauses)} WHERE [{keyColumn}] = @{keyColumn}";

            string currentUserName = GetCurrentUserName();

            try
            {
                _databaseHelper.ExecuteNonQuery(Convert.ToInt32(dbid), query, parameters.ToArray());

                // 2. Log the old and new data.
                string newValue = JsonSerializer.Serialize(formForNewValue);
                //dbHelper.LogAuditHistory(tableName, id, "EDIT", oldValue, newValue, currentUserName);
                _auditService.LogAuditHistory(tableName, id, "EDIT", oldValue, newValue, currentUserName); // *** CHANGED

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during edit: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Error updating data." });
            }
        }


    }
}