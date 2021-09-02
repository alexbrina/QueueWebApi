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

        /// <summary>
        /// Saves a new work request
        /// </summary>
        /// <remarks>Transaction must be controlled by caller</remarks>
        /// <param name="work"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public Task Save(Work work, IDbConnection conn)
        {
            using var cmd = conn.CreateCommand();

            cmd.CommandText = "INSERT INTO Work (Id, Status, Data, RequestedAt) " +
                "VALUES (@id, @status, @data, @requestedAt)";

            cmd.Parameters.Add(new SqliteParameter("@id", work.Id));
            cmd.Parameters.Add(new SqliteParameter("@status", work.Status));
            cmd.Parameters.Add(new SqliteParameter("@data", work.Data));
            cmd.Parameters.Add(new SqliteParameter("@requestedAt", work.RequestedAt));

            cmd.ExecuteNonQuery();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates the status of an existing work
        /// </summary>
        /// <remarks>Transaction must be controlled by caller</remarks>
        /// <param name="work"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public Task Update(Work work, IDbConnection conn)
        {
            using var cmd = conn.CreateCommand();

            cmd.CommandText = "UPDATE Work SET Status = @status, " +
                "CompletedAt = @completedAt WHERE Id = @id";

            cmd.Parameters.Add(new SqliteParameter("@id", work.Id));
            cmd.Parameters.Add(new SqliteParameter("@status", work.Status));
            cmd.Parameters.Add(new SqliteParameter("@completedAt", work.CompletedAt));

            cmd.ExecuteNonQuery();

            return Task.CompletedTask;
        }

        public Task<IEnumerable<Work>> GetRequested()
        {
            using var conn = context.GetConnection();
            
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Status, Data FROM Work WHERE Status = 'requested';";

            conn.Open();
            using var result = cmd.ExecuteReader();

            var works = new List<Work>();
            while (result.Read())
            {
                works.Add(new Work(
                    result.GetString(0),
                    result.GetString(1),
                    result.GetString(2)
                ));
            }

            return Task.FromResult(works.AsEnumerable());
        }
    }
}
