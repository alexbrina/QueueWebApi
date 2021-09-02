using System.Data;

namespace QueueWebApi.Adapters.Persistence
{
    internal static class DatabaseSchema
    {
        public static void Setup(IDbContext context)
        {
            using var conn = context.GetConnection();
            CreateRequestTable(conn);
        }

        private static void CreateRequestTable(IDbConnection conn)
        {
            using var command = conn.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Work (
                    Id TEXT PRIMARY KEY,
                    Status TEXT,
                    Data TEXT,
                    RequestedAt TEXT,
                    CompletedAt TEXT
                )";

            conn.Open();
            command.ExecuteNonQuery();
        }
    }
}
