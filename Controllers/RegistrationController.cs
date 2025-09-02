using GlassCodeTech_Ticketing_System_Project.Services;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.Data.SqlClient;

namespace GlassCodeTech_Ticketing_System_Project.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly DatabaseHelper _databaseHelper;

        public RegistrationController(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }

        // GET: /Registration/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Registration/Register
        [HttpPost]
        public IActionResult Register(string username, string email, string password, string companyName, string position, string role)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(companyName) ||
                string.IsNullOrWhiteSpace(position) || string.IsNullOrWhiteSpace(role))
            {
                ModelState.AddModelError("", "All fields including role are required.");
                return View();
            }


           var hudsi = DatabaseHelper.Encrypt(password);
            var parameters = new SqlParameter[]
            {
                new SqlParameter("@username", username),
                new SqlParameter("@email", email),
                new SqlParameter("@password_hash", DatabaseHelper.Encrypt(password)),
                new SqlParameter("@company_name", companyName),
                new SqlParameter("@position", position),
                new SqlParameter("@role", role)
            };

            _databaseHelper.ExecuteStoredProcedure("sp_RegisterUser", parameters);

            return RedirectToAction("Login", "Login");
        }
    }
}
