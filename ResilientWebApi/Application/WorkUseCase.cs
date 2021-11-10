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
            this.repository = repository ?? throw new System.ArgumentNullException(nameof(repository));
            this.channel = channel ?? throw new System.ArgumentNullException(nameof(channel));
            this.logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public async Task Execute(WorkRequest request)
        {
            var work = new Work { Data = request.Data };
            try
            {
                // when persisting requested work we should not make it dependent of channel
                // publishing success, it is better to save the request no matter what happens next
                await repository.SaveRequested(work);
                await channel.Writer.WriteAsync(work);
            }
            catch (Exception)
            {
                // we won't log the exception here and will let it bubble up because we want the
                // caller to know that this request failed. And so because most apis have a central
                // exception handler anyway!
                logger.LogError($"Error trying to save requested work {work.Id} with data {work.Data}");
                throw;
            }
        }
    }
}
