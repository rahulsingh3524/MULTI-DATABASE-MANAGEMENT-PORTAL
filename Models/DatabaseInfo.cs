namespace MULTI___DATABASE_MANAGEMENT_PORTAL.Models
{
    // Models
    public class DatabaseInfo
    {
        public int DBID { get; set; }
        public string DBName { get; set; }
        public string ConnectionString { get; set; }
        public bool IsActive { get; set; }
    }

    public class ConnStringContext
    {
        public List<DatabaseInfo> Databases { get; set; }
    }

    public class UserItem
    {
        public int ID { get; set; }
        public string UserName { get; set; }
    }

    public class DatabaseItem
    {
        public int ID { get; set; }
        public string DBName { get; set; }
    }
    public class UserRightsViewModel
    {
        public int SelectedUserId { get; set; }
        public List<UserItem> Users { get; set; }
        public List<DatabaseItem> AllocatedDatabases { get; set; }
        public List<DatabaseItem> UnallocatedDatabases { get; set; }
        public int SelectedDatabaseId { get; set; }
    }

    public class TableInfo
    {
        public string TableName { get; set; }
    }

}
