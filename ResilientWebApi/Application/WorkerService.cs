using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResilientWebApi.Domain.Adapters;
using ResilientWebApi.Domain.Exceptions;
using ResilientWebApi.Domain.Models;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ResilientWebApi.Application
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

                using var conn = unitOfWork.GetConnection(ConnectionTarget.WorkCompleted);
                conn.Open();

                // by using a transaction here you are actually creating a lock point
                // for all workers to compete for. The beavior will depend on how
                // your dbms works, in Sqlite for instance, it will lock the entire
                // db file, so all workers will be serialized. On the other hand,
                // a transaction will be desirable if you are updating different
                // tables (in the same database) and want it to be an atomic
                // operation. There's always a trade off!
                using var trans = conn.BeginTransaction();

                await Task.Delay(100, stoppingToken); // here goes the real work ...

                work.SetCompleted();
                await repository.SetCompleted(work, conn);
                trans.Commit();
            }
            catch (WorkCompletedException)
            {
                // Deduplication logic is ALWAYS required!
                // In our case, we ignore if repository reports work as already
                // completed. This is mandatory because we cannot guarantee that
                // the same work won't be queued 2+ times.
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
