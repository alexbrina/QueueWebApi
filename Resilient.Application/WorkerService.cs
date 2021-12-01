using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Resilient.Domain.Adapters;
using Resilient.Domain.Exceptions;
using Resilient.Domain.Models;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Resilient.Application
{
    internal class WorkerService
    {
        private readonly int workerId;
        private readonly ChannelReader<Work> reader;
        private readonly IServiceScope scope;
        private readonly ILogger<WorkerService> logger;

        public WorkerService(
            int workerId, ChannelReader<Work> reader, IServiceScope scope,
            ILoggerFactory loggerFactory)
        {
            this.workerId = workerId;
            this.reader = reader;
            this.scope = scope;
            logger = loggerFactory.CreateLogger<WorkerService>();
        }

        public async Task Run(CancellationToken stoppingToken)
        {
            logger.LogDebug($"Worker {workerId} starting.");

            await foreach (var work in reader.ReadAllAsync(stoppingToken))
            {
                await DoWork(work, stoppingToken);
            }
        }

        private async Task DoWork(Work work, CancellationToken stoppingToken)
        {
            try
            {
                logger.LogInformation($"Worker {workerId} processing Work {work.Id}.");

                var repository = scope.ServiceProvider.GetRequiredService<IWorkRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var workOperator = scope.ServiceProvider.GetRequiredService<IWorkOperator>();

                using var conn = unitOfWork.GetConnection(ConnectionTarget.WorkOutbox);
                conn.Open();

                // notice that, by using a db transaction you are creating a global
                // lock point, so all your workers will be serialized in this point.
                // On the other hand, a transaction is desirable if you are
                // updating 2+ different tables (in the same database) and want
                // it to be an atomic operation, or, if you want to make database
                // commit dependent of some other operation (which is the case
                // in this example).
                using var trans = conn.BeginTransaction();

                // first we try setting it to completed, it will fail if already
                // completed. This is our deduplication strategy
                work.SetCompleted();
                await repository.SetCompleted(work, conn, trans);

                // then we execute the operation inside transaction.
                // Be cautious, it can be a problem if the operation is too
                // expensive, because all workers will be blocked waiting for
                // this transaction to finish. Tradeoffs!
                await workOperator.Execute(work, workerId, stoppingToken);

                // if everything is ok we commit!
                trans.Commit();
            }
            catch (WorkCompletedException)
            {
                // Deduplication logic is ALWAYS required! This is mandatory
                // because we cannot guarantee that the same work won't be
                // queued 2+ times. In our case, when repository reports that
                // this work has already completed, we generate a log and simply
                // ignore the exception.
                logger.LogDebug($"Worker {workerId} tried to complete already " +
                    $"completed Work {work.Id}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error processing work {work.Id} in worker {workerId}");
            }
        }
    }
}
