using Microsoft.Data.Sqlite;
using Resilient.Domain.Adapters;
using Resilient.Domain.Exceptions;
using Resilient.Domain.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Resilient.Adapters.Storage
{
    internal sealed class WorkRepositoryBetterApproach : IWorkRepository
    {
        private readonly IDbContext context;

        // https://www.sqlite.org/rescode.html#constraint_primarykey
        private const int SQLITE_CONSTRAINT_PRIMARYKEY = 1555;

        public WorkRepositoryBetterApproach(IDbContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Save requested work for async execution using main table only.
        /// Another approach is shown in <see cref="WorkRepositoryBasicApproach"/>
        /// </summary>
        /// <remarks>Here we show a different approach of the Outbox Pattern.
        /// We use only the main table to save incoming requests and do not
        /// insert a record in the WorkOutbox table.
        /// We put a record in the WorkOutbox only when work is done. This way
        /// we avoid creating a transaction here and therefore reduce the
        /// occurrence of locks when saving new requests.
        /// </remarks>
        /// <param name="work"></param>
        /// <returns></returns>
        public Task SaveRequested(Work work)
        {
            using var conn = context.GetConnection(ConnectionTarget.Work);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Work (Id, Data, RequestedAt) " +
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
        /// <remarks>We let the transaction be controlled by caller so it can
        /// rollback if related operation(s) fail</remarks>
        /// <param name="work"></param>
        /// <param name="conn"></param>
        /// <param name="trans"></param>
        /// <returns></returns>
        public Task SetCompleted(Work work, IDbConnection conn, IDbTransaction trans)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = trans;

                cmd.CommandText = "INSERT INTO WorkOutbox (Id, CompletedAt) " +
                    "VALUES (@id, @completedAt)";
                cmd.Parameters.Add(new SqliteParameter("@id", work.Id));
                cmd.Parameters.Add(new SqliteParameter("@completedAt", work.CompletedAt));

                cmd.ExecuteNonQuery();

                return Task.CompletedTask;
            }
            catch (SqliteException ex)
            {
                if (ex.SqliteExtendedErrorCode == SQLITE_CONSTRAINT_PRIMARYKEY)
                {
                    // in case of a pk error it means that this work is
                    // already completed.
                    throw new WorkCompletedException();
                }
                throw;
            }
        }

        /// <summary>
        /// Get all pending works to put them back in the queue
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<Work>> GetPending()
        {
            using var conn = context.GetConnection(ConnectionTarget.Work);
            var alias = context.Attach(conn, ConnectionTarget.WorkOutbox);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Id, Data FROM Work w WHERE NOT EXISTS " +
                $"(SELECT 1 FROM {alias}.WorkOutbox o WHERE o.Id = w.Id);";

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
