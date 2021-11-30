using Microsoft.Data.Sqlite;
using Resilient.Domain.Adapters;
using System.Data;

namespace Resilient.Adapters.Storage
{
    internal sealed class SqliteDbContext : IDbContext
    {
        public IDbConnection GetConnection(ConnectionTarget target)
        {
            return new SqliteConnection($"Data Source={TargetDatabaseFile(target)}");
        }

        public string Attach(IDbConnection conn, ConnectionTarget target)
        {
            var alias = $"{target}Attached";
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"ATTACH '{TargetDatabaseFile(target)}' AS {alias}";
            conn.Open();
            cmd.ExecuteNonQuery();
            return alias;
        }

        private static string TargetDatabaseFile(ConnectionTarget target) =>
            $"{target}.sqlite";
    }
}
