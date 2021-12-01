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
    internal sealed class WorkRepository : IWorkRepository
    {
        private readonly IDbContext context;

        public WorkRepository(IDbContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Save requested work for async execution
        /// </summary>
        /// <remarks>Here we show an example of the Outbox Pattern, used to
        /// control the work execution and set its completion only if related
        /// operation(s) succeed.
        /// These operations could be publishing a message, calling a HTTP or
        /// SOAP API, or any other type of remote service call</remarks>
        /// <param name="work"></param>
        /// <returns></returns>
        public Task SaveRequested(Work work)
        {
            using var conn = context.GetConnection(ConnectionTarget.Work);
            context.Attach(conn, ConnectionTarget.WorkOutbox);

            using var wCmd = conn.CreateCommand();
            wCmd.CommandText = "INSERT INTO Work (Id, Data) VALUES (@id, @data)";
            wCmd.Parameters.Add(new SqliteParameter("@id", work.Id));
            wCmd.Parameters.Add(new SqliteParameter("@data", work.Data));

            using var oCmd = conn.CreateCommand();
            oCmd.CommandText = "INSERT INTO WorkOutbox (Id, RequestedAt) " +
                "VALUES (@id, @requestedAt)";
            oCmd.Parameters.Add(new SqliteParameter("@id", work.Id));
            oCmd.Parameters.Add(new SqliteParameter("@requestedAt", work.RequestedAt));

            conn.Open();

            using var trans = conn.BeginTransaction();
            wCmd.Transaction = trans;
            wCmd.ExecuteNonQuery();
            oCmd.Transaction = trans;
            oCmd.ExecuteNonQuery();
            trans.Commit();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Set the status of an existing work to completed
        /// </summary>
        /// <remarks>Transaction is controlled by caller</remarks>
        /// <param name="work"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public Task SetCompleted(Work work, IDbConnection conn, IDbTransaction trans)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;

            // updates only if not already completed
            cmd.CommandText = @"
                UPDATE WorkOutbox
                   SET CompletedAt = @completedAt
                 WHERE Id = @id
                   AND CompletedAt IS NULL";

            cmd.Parameters.Add(new SqliteParameter("@id", work.Id));
            cmd.Parameters.Add(new SqliteParameter("@completedAt", work.CompletedAt));
            var affected = cmd.ExecuteNonQuery();

            if (affected == 0)
            {
                CouldNotUpdateWorkState(work, conn);
            }

            return Task.CompletedTask;
        }

        private static void CouldNotUpdateWorkState(Work work, IDbConnection conn)
        {
            // we check if a record for this work exists so we know for
            // sure that it is already completed and then we can send
            // back a meaningful exception to domain.
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT count(1) FROM WorkOutbox WHERE Id = @id";
            cmd.Parameters.Add(new SqliteParameter("@id", work.Id));
            var count = (Int64)cmd.ExecuteScalar();
            if (count == 1)
            {
                // we could simply ignore it here, but it is kind of a domain
                // concern so we let the domain decide for itself.

                // we could also return some error code to express this cenario,
                // but I see exceptions as a more versatile way of expressing
                // exceptional paths.
                // I know about "don't use exceptions for control flow" stuff.
                // It's a legitimate claim indeed, but it applies in a
                // different degree of abuse!
                throw new WorkCompletedException();
            }

            // if it gets here, we are missing the outbox record for this work
            // due to same storage inconsistency
            throw new MissingWorkExecutionStateException();
        }

        public Task<IEnumerable<Work>> GetPending()
        {
            using var conn = context.GetConnection(ConnectionTarget.Work);
            var alias = context.Attach(conn, ConnectionTarget.WorkOutbox);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
                SELECT w.Id, w.Data
                  FROM Work w
                  JOIN {alias}.WorkOutbox o ON o.Id = w.Id
                 WHERE o.CompletedAt IS NULL;";

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
