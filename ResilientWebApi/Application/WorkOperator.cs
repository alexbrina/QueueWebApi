using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using ResilientWebApi.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ResilientWebApi.Application
{
    internal interface IWorkOperator
    {
        Task Execute(Work work, CancellationToken stoppingToken);
    }

    internal class WorkOperator : IWorkOperator
    {
        private const string RETRY_COUNT_KEY = "retryCount";
        private readonly ILogger<WorkOperator> logger;
        private readonly AsyncRetryPolicy retryPolicy;

        public WorkOperator(ILogger<WorkOperator> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // this is our basic policy
            retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: retryCount =>
                    {
                        var timeToWait = TimeSpan.FromSeconds(1);
                        logger.LogDebug($"Waiting {timeToWait.TotalSeconds} " +
                            $"seconds before retry #{retryCount}");
                        return timeToWait;
                    },
                    onRetryAsync: (exception, timeSpan, retryCount, context) =>
                    {
                        logger.LogDebug($"Proceeding to retry #{retryCount}");

                        // update context with current retry count
                        context[RETRY_COUNT_KEY] = retryCount;
                        return Task.CompletedTask;
                    }
                );
        }

        public async Task Execute(Work work, CancellationToken stoppingToken)
        {
            // create a context for this operation
            var context = new Context { { RETRY_COUNT_KEY, 0 } };

            await retryPolicy.ExecuteAsync(async (context) =>
            {
                var attempt = (int)context[RETRY_COUNT_KEY] + 1;
                logger.LogDebug($"This is attempt #{attempt}");

                // here goes the real work ...
                await Task.Delay(100, stoppingToken);

                // just a random exception generator
                var random = new Random();
                if (random.Next(1, 3) == 2)
                {
                    logger.LogDebug($"An error ocurred!");
                    throw new InvalidOperationException("Whatever!");
                }

                logger.LogDebug($"Work is done!");

            }, context);
        }
    }
}
