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

                using var conn = unitOfWork.GetConnection(ConnectionTarget.WorkCompleted);
                conn.Open();

                // notice that, by using a db transaction you are creating a global
                // lock point, so all your workers will be serialized in this point.
                // On the other hand, a transaction will be desirable if you are
                // updating 2+ different tables (in the same database) and want
                // it to be an atomic operation.
                using var trans = conn.BeginTransaction();

                await workOperator.Execute(work, stoppingToken);

                work.SetCompleted();
                await repository.SetCompleted(work, conn);
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
