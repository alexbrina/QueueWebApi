using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QueueWebApi.Domain.Adapters;
using QueueWebApi.Domain.Models;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace QueueWebApi.Application
{
    internal class WorkerBackgroundService : BackgroundService
    {
        private readonly Channel<Work> channel;
        private readonly IServiceProvider provider;
        private readonly ILogger<WorkerBackgroundService> logger;

        public WorkerBackgroundService(
            Channel<Work> channel,
            IServiceProvider provider,
            ILogger<WorkerBackgroundService> logger)
        {
            this.channel = channel ?? throw new System.ArgumentNullException(nameof(channel));
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogDebug($"{nameof(WorkerBackgroundService)} is starting.");

            stoppingToken.Register(() => logger.LogDebug($"{nameof(WorkerBackgroundService)} is stopping."));

            logger.LogDebug($"{nameof(WorkerBackgroundService)} is loading requested works.");
            await LoadRequestedWorks(stoppingToken);

            logger.LogDebug($"{nameof(WorkerBackgroundService)} is processing requested works.");
            await ProcessWorks(stoppingToken);

            logger.LogDebug($"{nameof(WorkerBackgroundService)} stopped.");
        }

        private async Task LoadRequestedWorks(CancellationToken stoppingToken)
        {
            using var scope = provider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IWorkRepository>();
            var works = await repository.GetRequested();
            foreach (var work in works)
            {
                await channel.Writer.WriteAsync(work, stoppingToken);
            }
        }

        private async Task ProcessWorks(CancellationToken stoppingToken)
        {
            while (!channel.Reader.Completion.IsCompleted && !stoppingToken.IsCancellationRequested)
            {
                using var scope = provider.CreateScope();
                await DoWork(scope, stoppingToken);
            }
        }

        private async Task DoWork(IServiceScope scope, CancellationToken stoppingToken)
        {
            var work = await channel.Reader.ReadAsync(stoppingToken);
            try
            {
                var repository = scope.ServiceProvider.GetRequiredService<IWorkRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                using var conn = unitOfWork.GetConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                // here goes the real work ...
                await Task.Delay(500, stoppingToken);

                work.SetCompleted();
                await repository.Update(work, conn);
                trans.Commit();

                logger.LogInformation($"Work {work.Id} processed. {channel.Reader.Count} work(s) remaining.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error processing work: {work.Id}");
                // TODO ???
            }
        }
    }
}
