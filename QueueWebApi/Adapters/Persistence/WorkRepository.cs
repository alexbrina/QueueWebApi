using Microsoft.Data.Sqlite;
using QueueWebApi.Domain.Adapters;
using QueueWebApi.Domain.Exceptions;
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

        // https://www.sqlite.org/rescode.html#constraint_primarykey
        private const int SQLITE_CONSTRAINT_PRIMARYKEY = 1555;

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
            try
            {
                using var cmd = conn.CreateCommand();

                cmd.CommandText = "INSERT INTO WorkCompleted (Id, CompletedAt) VALUES (@id, @completedAt)";

                cmd.Parameters.Add(new SqliteParameter("@id", work.Id));
                cmd.Parameters.Add(new SqliteParameter("@completedAt", work.CompletedAt));

                cmd.ExecuteNonQuery();

                return Task.CompletedTask;
            }
            catch (SqliteException ex)
            {
                if (ex.SqliteExtendedErrorCode == SQLITE_CONSTRAINT_PRIMARYKEY)
                {
                    // in case of a pk error we send back a more meaningful exception to domain.
                    // we could simply ignore it here, but it is kind of a domain concern so
                    // it is better let the domain decide for itself.
                    // we could return some error codes to express this cenario, but I see
                    // exceptions as a more versatile way of expressing exceptional paths.
                    // I care less about that "don't use exceptions for control flow" stuff.
                    // it applies indeed, but in different degrees of abuse!
                    throw new WorkCompletedException(ex);
                }
                throw;
            }
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
