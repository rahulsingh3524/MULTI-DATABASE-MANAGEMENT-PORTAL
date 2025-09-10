using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using MULTI___DATABASE_MANAGEMENT_PORTAL.Services; // Assuming your services namespace
using System.Threading.Tasks;
using System.Data.SqlClient;
using MULTI___DATABASE_MANAGEMENT_PORTAL.Models;

namespace MULTI___DATABASE_MANAGEMENT_PORTAL.Controllers
{
    public class LoginController : Controller
    {
        private readonly DatabaseHelper _databaseHelper;
        private readonly CookieService _cookieService;
        public LoginDetail logindata;
        public LoginController(DatabaseHelper databaseHelper, CookieService cookieService)
        {
            _databaseHelper = databaseHelper;
            _cookieService = cookieService;
            logindata = new LoginDetail();
        }

        //GET: /Login
        //Check for existing cookie and show partial view or login page
        [HttpGet]
        public IActionResult Login()
        {
            var dict = _cookieService.GetDictionaryFromCookie("UI");
            if (dict != null && dict.ContainsKey(logindata.Username)) // "B" is username key
            {
                string encryptedUsername = dict[logindata.Username];
                string username = DatabaseHelper.Decrypt(encryptedUsername);
                return PartialView("_PasswordOnlyLoginPartial", username);

            }
            else
            {
                var model = new LoginViewModel
                {
                    Username = string.Empty,
                    Password = string.Empty,
                    IsPasswordOnly = false
                };
                return View(model);
            }
        }

        // POST: /Login
        // Full login with username and password
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Username and password are required.");
                return View();
            }

            string encryptedPassword = DatabaseHelper.Encrypt(password);

            var parameters = new SqlParameter[]
            {
        new SqlParameter("@username", username),
        new SqlParameter("@password_hash", encryptedPassword)
            };

            // Call VerifyUser stored procedure (table: Master_User)
            var result = _databaseHelper.ExecuteStoredProcedure("VerifyUser", parameters);

            if (result != null && result.Count > 0)
            {
                var row = result[0];
                var loginDetail = new Dictionary<string, string>
        {
            { logindata.Id, DatabaseHelper.Encrypt(row["ID"]?.ToString() ?? "") },
            { logindata.Username, DatabaseHelper.Encrypt(row["UserName"]?.ToString() ?? "") },
            { logindata.Email, "" }, // Email column not returned by SP, add if needed
            { logindata.CompanyName, DatabaseHelper.Encrypt(row["company_name"]?.ToString() ?? "") },
            { logindata.Position, DatabaseHelper.Encrypt(row["position"]?.ToString() ?? "") },
            { logindata.Role, DatabaseHelper.Encrypt(row["role"]?.ToString() ?? "") },
            { logindata.IsAdmin, DatabaseHelper.Encrypt(row["is_admin"]?.ToString() ?? "") }
        };

                _cookieService.SetKeyValueInCookie("UI", loginDetail, 30);

            //    var historyParams = new SqlParameter[]
            //    {
            //new SqlParameter("@id", row["ID"]?.ToString() ?? "")
            //    };

            //    _databaseHelper.ExecuteStoredProcedure("sp_savelogin_history", historyParams);

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View();
            }
        }



        // POST: /Login/PasswordOnly
        [HttpPost]
        public IActionResult PasswordOnlyLogin(string password)
        {
            var dict = _cookieService.GetDictionaryFromCookie("UI");
            if (dict == null || !dict.ContainsKey(logindata.Username))
            {
                return RedirectToAction("Login");
            }

            string username = DatabaseHelper.Decrypt(dict[logindata.Username]);
            string encryptedPassword = DatabaseHelper.Encrypt(password);
            ViewBag.CurrentUserName = username;
            var parameters = new SqlParameter[]
            {
                        new SqlParameter("@username", username),
                        new SqlParameter("@password_hash", encryptedPassword)
            };

            var result = _databaseHelper.ExecuteStoredProcedure("VerifyUser", parameters);

            if (result != null && result.Count > 0)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Invalid password.");

                return View("Login", new LoginViewModel
                {
                    Username = username,
                    IsPasswordOnly = true
                });

            }
        }

        // POST: /Logout
        [HttpPost]
        public IActionResult Logout()
        {
            _cookieService.DeleteCookie("UI");
            return RedirectToAction("Login");
        }
    }
}

  