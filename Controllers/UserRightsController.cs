using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using MULTI___DATABASE_MANAGEMENT_PORTAL.Models;
using MULTI___DATABASE_MANAGEMENT_PORTAL.Services;

namespace MULTI___DATABASE_MANAGEMENT_PORTAL.Controllers
{
    public class UserRightsController : Controller
    {
        private readonly DatabaseHelper _databaseHelper;
        public UserRightsController(DatabaseHelper db)
        {
            _databaseHelper = db;
        }
        public IActionResult UserRights(int? userId)
        {
            // Get all active users
            var usersResult = _databaseHelper.ExecuteStoredProcedure("sp_GetAllActiveUsers", null);
            var users = usersResult.Select(r => new UserItem
            {
                ID = Convert.ToInt32(r["ID"]),
                UserName = r["UserName"].ToString()
            }).ToList();

            // Only load details if a user ID was selected
            if (userId == null || userId == 0)
                return View(new UserRightsViewModel { Users = users, SelectedUserId = 0 });

            int selectedUserId = userId ?? (users.FirstOrDefault()?.ID ?? 0);

            // Get allocated databases for selected user
            var allocatedParams = new SqlParameter[]
            {
        new SqlParameter("@UserID", selectedUserId)
            };
            var allocatedResult = _databaseHelper.ExecuteStoredProcedure("sp_GetAllocatedDatabases", allocatedParams);
            var allocated = allocatedResult.Select(r => new DatabaseItem
            {
                ID = Convert.ToInt32(r["ID"]),
                DBName = r["DBName"].ToString()
            }).ToList();

            // Get unallocated databases for selected user
            var unallocatedParams = new SqlParameter[]
            {
        new SqlParameter("@UserID", selectedUserId)
            };
            var unallocatedResult = _databaseHelper.ExecuteStoredProcedure("sp_GetUnallocatedDatabases", unallocatedParams);
            var unallocated = unallocatedResult.Select(r => new DatabaseItem
            {
                ID = Convert.ToInt32(r["ID"]),
                DBName = r["DBName"].ToString()
            }).ToList();

            return View(new UserRightsViewModel
            {
                Users = users,
                SelectedUserId = selectedUserId,
                AllocatedDatabases = allocated,
                UnallocatedDatabases = unallocated
            });
        }

        [HttpPost]
        public IActionResult AllocateDatabase(int SelectedUserId, int SelectedDatabaseId)
        {
            var parameters = new SqlParameter[]
            {
        new SqlParameter("@UserID", SelectedUserId),
        new SqlParameter("@DB_ID", SelectedDatabaseId)
            };
            _databaseHelper.ExecuteStoredProcedure("sp_AllocateDatabaseToUser", parameters);

            return RedirectToAction("UserRights", new { userId = SelectedUserId });
        }

        [HttpPost]
        public IActionResult DeallocateDatabase(int SelectedUserId, int SelectedDatabaseId)
        {
            var parameters = new SqlParameter[]
            {
        new SqlParameter("@UserID", SelectedUserId),
        new SqlParameter("@DB_ID", SelectedDatabaseId)
            };
            _databaseHelper.ExecuteStoredProcedure("sp_DeallocateDatabaseFromUser", parameters);

            return RedirectToAction("UserRights", new { userId = SelectedUserId });
        }

    }
}
