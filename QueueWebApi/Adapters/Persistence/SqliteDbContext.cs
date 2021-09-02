using Microsoft.Data.Sqlite;
using System.Data;

namespace QueueWebApi.Adapters.Persistence
{
    internal sealed class SqliteDbContext : IDbContext
    {
        private readonly string connectionString =
            "Data Source=QueueWebApi.sqlite";

        public IDbConnection GetConnection()
        {
            return new SqliteConnection(connectionString);
        }
    }
}
