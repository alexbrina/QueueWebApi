using Resilient.Domain.Adapters;

namespace Resilient.Adapters.Storage
{
    internal static class DatabaseSchema
    {
        public static void Setup(IDbContext context)
        {
            CreateWorkTable(context);
            CreateWorkOutboxTable(context);
        }

        private static void CreateWorkTable(IDbContext context)
        {
            using var conn = context.GetConnection(ConnectionTarget.Work);
            using var command = conn.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Work (
                    Id TEXT PRIMARY KEY,
                    Data TEXT,
                    RequestedAt TEXT
                )";

            conn.Open();
            command.ExecuteNonQuery();
        }

        private static void CreateWorkOutboxTable(IDbContext context)
        {
            using var conn = context.GetConnection(ConnectionTarget.WorkOutbox);
            using var command = conn.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS WorkOutbox (
                    Id TEXT PRIMARY KEY,
                    CompletedAt TEXT
                )";

            conn.Open();
            command.ExecuteNonQuery();
        }
    }
}
