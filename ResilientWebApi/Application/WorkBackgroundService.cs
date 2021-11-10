using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResilientWebApi.Domain.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

#nullable enable

namespace ResilientWebApi.Application
{
    internal class WorkBackgroundService : BackgroundService
    {
        private readonly Channel<Work> channel;
        private readonly IServiceProvider provider;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<WorkBackgroundService> logger;

        private const int NUMBER_OF_PARALLEL_WORKERS = 3;
        private const int LOAD_INTERVAL_SECONDS = 30;
        private Timer? timer;

        public WorkBackgroundService(
            Channel<Work> channel,
            IServiceProvider provider,
            ILoggerFactory loggerFactory)
        {
            this.channel = channel
                ?? throw new System.ArgumentNullException(nameof(channel));
            this.provider = provider
                ?? throw new ArgumentNullException(nameof(provider));
            this.loggerFactory = loggerFactory
                ?? throw new ArgumentNullException(nameof(loggerFactory));
            logger = loggerFactory.CreateLogger<WorkBackgroundService>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() => 
                logger.LogDebug($"{nameof(WorkBackgroundService)} is stopping."));

            StartLoader(stoppingToken);
            await StartWorkers(stoppingToken);

            timer?.Change(Timeout.Infinite, 0);
            logger.LogDebug($"{nameof(WorkBackgroundService)} stopped.");
        }

        private void StartLoader(CancellationToken stoppingToken)
        {
            var loader = new LoaderService(channel, provider, loggerFactory);

            // async void should be avoided due to exception handling (if you
            // don't have a task to wrap your work, an exceptions ocurred inside
            // that code will kill main process). But when dealing with event
            // handlers you need void return method, for that you can wrap your
            // async call as stated in this link below.
            // https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming#avoid-async-void
            async Task asyncHandler() => await loader.Run(stoppingToken);
            void handler(object? obj) => _ = asyncHandler();

            timer = new Timer(handler, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(LOAD_INTERVAL_SECONDS));
        }

        private async Task StartWorkers(CancellationToken stoppingToken)
        {
            // instantiating the scope here will make the same services instances be reused
            // by all consumers
            using var scope = provider.CreateScope();

            // run parallel workers
            var workers = Enumerable.Range(1, NUMBER_OF_PARALLEL_WORKERS)
                .Select(i => new WorkerService(i, channel.Reader, scope, loggerFactory)
                .Run(stoppingToken))
                .ToArray();

            await Task.WhenAll(workers);
        }

        public override void Dispose()
        {
            base.Dispose();
            timer?.Dispose();
        }
    }
}
