using QueueWebApi.Domain.Adapters;
using System.Data;

namespace QueueWebApi.Adapters.Persistence
{
    internal static class DatabaseSchema
    {
        public static void Setup(IDbContext context)
        {
            CreateRequestedTable(context);
            CreateCompletedTable(context);
        }

        private static void CreateRequestedTable(IDbContext context)
        {
            using var conn = context.GetConnection(ConnectionTarget.WorkRequested);
            using var command = conn.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS WorkRequested (
                    Id TEXT PRIMARY KEY,
                    Data TEXT,
                    RequestedAt TEXT
                )";

            conn.Open();
            command.ExecuteNonQuery();
        }

        private static void CreateCompletedTable(IDbContext context)
        {
            using var conn = context.GetConnection(ConnectionTarget.WorkCompleted);
            using var command = conn.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS WorkCompleted (
                    Id TEXT PRIMARY KEY,
                    CompletedAt TEXT
                )";

            conn.Open();
            command.ExecuteNonQuery();
        }
    }
}
