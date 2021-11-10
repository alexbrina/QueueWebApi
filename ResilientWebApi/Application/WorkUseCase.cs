using Microsoft.Extensions.Logging;
using ResilientWebApi.Domain.Adapters;
using ResilientWebApi.Domain.Models;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ResilientWebApi.Application
{
    public interface IWorkUseCase
    {
        Task Execute(WorkRequest request);
    }

    internal class WorkUseCase : IWorkUseCase
    {
        private readonly IWorkRepository repository;
        private readonly Channel<Work> channel;
        private readonly ILogger<WorkUseCase> logger;

        public WorkUseCase(
            IWorkRepository repository,
            Channel<Work> channel,
            ILogger<WorkUseCase> logger)
        {
            this.repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
            this.channel = channel
                ?? throw new ArgumentNullException(nameof(channel));
            this.logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Execute(WorkRequest request)
        {
            var work = new Work { Data = request.Data };
            try
            {
                // when persisting requested work we should not make it dependent
                // of channel publishing success, it is better to save the request
                // no matter what happens next
                await repository.SaveRequested(work);
                await channel.Writer.WriteAsync(work);
            }
            catch (Exception)
            {
                // we log details about failed request but won't log the exception
                // itself in here. We will let it bubble up because we want the
                // caller to know that this request failed.
                // Also because most apis have a central exception handler anyway!
                logger.LogError($"Error trying to save requested work " +
                    $"{work.Id} with data {work.Data}");
                throw;
            }
        }
    }
}
