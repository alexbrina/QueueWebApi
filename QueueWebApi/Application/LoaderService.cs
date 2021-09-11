using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueueWebApi.Domain.Adapters;
using QueueWebApi.Domain.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace QueueWebApi.Application
{
    internal class LoaderService
    {
        private readonly ChannelWriter<Work> writer;
        private readonly IServiceProvider provider;
        private readonly ILogger<LoaderService> logger;

        public LoaderService(
            ChannelWriter<Work> writer, IServiceProvider provider, ILoggerFactory loggerFactory)
        {
            this.writer = writer;
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

                logger.LogDebug($"{nameof(LoaderService)} found {works.Count()} works pending.");

                foreach (var work in works)
                {
                    logger.LogTrace($"Loading pending work {work.Id}.");
                    await writer.WriteAsync(work, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error loading pending works.");
            }
        }
    }
}
