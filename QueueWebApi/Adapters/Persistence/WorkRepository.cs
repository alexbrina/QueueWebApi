using Microsoft.Data.Sqlite;
using QueueWebApi.Domain.Adapters;
using QueueWebApi.Domain.Models;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace QueueWebApi.Adapters.Persistence
{
    internal sealed class WorkRepository : IWorkRepository
    {
        private readonly IDbContext context;

        public WorkRepository(IDbContext context)
        {
            this.context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        public Task SaveRequested(Work work)
        {
            using var conn = context.GetConnection(ConnectionTarget.WorkRequested);
            using var cmd = conn.CreateCommand();

            cmd.CommandText = "INSERT INTO WorkRequested (Id, Data, RequestedAt) " +
                "VALUES (@id, @data, @requestedAt)";

            cmd.Parameters.Add(new SqliteParameter("@id", work.Id));
            cmd.Parameters.Add(new SqliteParameter("@data", work.Data));
            cmd.Parameters.Add(new SqliteParameter("@requestedAt", work.RequestedAt));

            conn.Open();
            cmd.ExecuteNonQuery();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Set the status of an existing work to completed
        /// </summary>
        /// <remarks>Transaction must be controlled by caller</remarks>
        /// <param name="work"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public Task SetCompleted(Work work, IDbConnection conn)
        {
            using var cmd = conn.CreateCommand();

            cmd.CommandText = "INSERT INTO WorkCompleted (Id, CompletedAt) VALUES (@id, @completedAt)";

            cmd.Parameters.Add(new SqliteParameter("@id", work.Id));
            cmd.Parameters.Add(new SqliteParameter("@completedAt", work.CompletedAt));

            cmd.ExecuteNonQuery();

            return Task.CompletedTask;
        }

        public Task<IEnumerable<Work>> GetPending()
        {
            using var conn = context.GetConnection(ConnectionTarget.WorkRequested);
            var alias = context.Attach(conn, ConnectionTarget.WorkCompleted);
            
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Id, Data FROM WorkRequested r WHERE NOT EXISTS " +
                $"(SELECT 1 FROM {alias}.WorkCompleted c WHERE c.Id = r.Id);";

            conn.Open();
            using var result = cmd.ExecuteReader();

            var works = new List<Work>();
            while (result.Read())
            {
                works.Add(new Work(
                    result.GetString(0),
                    result.GetString(1)
                ));
            }

            return Task.FromResult(works.AsEnumerable());
        }
    }
}
