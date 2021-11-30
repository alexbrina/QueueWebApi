using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Resilient.Domain.Adapters;
using Resilient.Domain.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Resilient.Application
{
    internal class LoaderService
    {
        private readonly Channel<Work> channel;
        private readonly IServiceProvider provider;
        private readonly ILogger<LoaderService> logger;

        public LoaderService(Channel<Work> channel, IServiceProvider provider,
            ILoggerFactory loggerFactory)
        {
            this.channel = channel;
            this.provider = provider;
            logger = loggerFactory.CreateLogger<LoaderService>();
        }

        public async Task Run(CancellationToken stoppingToken)
        {
            try
            {
                logger.LogDebug($"{nameof(LoaderService)} is loading pending works.");

                using var scope = provider.CreateScope();

                var repository = scope.ServiceProvider.GetRequiredService<IWorkRepository>();
                var works = await repository.GetPending();

                logger.LogDebug($"{nameof(LoaderService)} found " +
                    $"{works.Count()} works pending.");

                if (works.Any())
                {
                    // we clear this channel before reloading it to avoid
                    // putting the same pending works many times repeatedly.
                    // we can do this because we know that all works
                    // are persisted as soon as they arive.
                    var cleared = await channel.Clear(stoppingToken);
                    logger.LogDebug($"{nameof(LoaderService)} cleared " +
                        $"{cleared} previously loaded works.");

                    foreach (var work in works)
                    {
                        logger.LogTrace($"Loading pending work {work.Id}.");
                        await channel.Writer.WriteAsync(work, stoppingToken);
                    }

                    logger.LogDebug($"{nameof(LoaderService)} loaded all pending works.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error loading pending works.");
            }
        }
    }
}
