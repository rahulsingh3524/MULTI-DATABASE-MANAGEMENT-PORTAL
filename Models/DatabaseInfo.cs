namespace MULTI___DATABASE_MANAGEMENT_PORTAL.Models
{
    public class DatabaseInfo
    {
        public int DBID { get; set; }
        public string DBName { get; set; }
        public string ConnectionString { get; set; }
    }

    public class ConnStringContext
    {
        public List<DatabaseInfo> Databases { get; set; }
    }

}
