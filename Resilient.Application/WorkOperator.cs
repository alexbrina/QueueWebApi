using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Resilient.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resilient.Application
{
    internal interface IWorkOperator
    {
        Task Execute(Work work, int workerId, CancellationToken stoppingToken);
    }

    internal class WorkOperator : IWorkOperator
    {
        private const string RETRY_COUNT_KEY = "retryCount";
        private const byte RETRY_COUNT_TOTAL = 2;
        private readonly ILogger<WorkOperator> logger;
        private readonly AsyncRetryPolicy retryPolicy;

        public WorkOperator(ILogger<WorkOperator> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // this is our basic policy
            retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: RETRY_COUNT_TOTAL,
                    sleepDurationProvider: retryCount =>
                    {
                        var timeToWait = TimeSpan.FromSeconds(Math.Pow(2, retryCount - 1));
                        logger.LogDebug($"Waiting {timeToWait.TotalSeconds} " +
                            $"seconds before retry #{retryCount}");
                        return timeToWait;
                    },
                    onRetryAsync: (exception, timeSpan, retryCount, context) =>
                    {
                        logger.LogDebug($"Proceeding to retry #{retryCount} of " +
                            $"{RETRY_COUNT_TOTAL}");

                        // update context with current retry count
                        context[RETRY_COUNT_KEY] = retryCount;
                        return Task.CompletedTask;
                    }
                );
        }

        /// <summary>
        /// Executes real work
        /// </summary>
        /// <remarks>Here we show an example of a simple retry policy using
        /// Polly</remarks>
        /// <param name="work">Work instance</param>
        /// <param name="workerId">Id of worker executing this work</param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task Execute(Work work, int workerId, CancellationToken stoppingToken)
        {
            // create a context for this operation
            var context = new Context { { RETRY_COUNT_KEY, 0 } };

            await retryPolicy.ExecuteAsync(async (context) =>
            {
                var attempt = (int)context[RETRY_COUNT_KEY] + 1;
                logger.LogDebug($"Worker {workerId}: This is attempt #{attempt}");

                // here goes the real work ...
                await Task.Delay(100, stoppingToken);

                // just a random exception generator
                var random = new Random();
                if (random.Next(1, 3) == 2)
                {
                    logger.LogDebug($"Worker {workerId}: An error ocurred!");
                    throw new InvalidOperationException("Whatever!");
                }

                logger.LogDebug($"Worker {workerId}: Work is done!");

            }, context);
        }
    }
}
