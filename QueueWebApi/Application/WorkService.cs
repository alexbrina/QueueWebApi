using QueueWebApi.Domain.Adapters;
using QueueWebApi.Domain.Models;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace QueueWebApi.Application
{
    public interface IWorkService
    {
        Task Execute(WorkRequest request);
    }

    internal class WorkService : IWorkService
    {
        private readonly IWorkRepository repository;
        private readonly IUnitOfWork unitOfWork;
        private readonly Channel<Work> channel;

        public WorkService(IWorkRepository repository, IUnitOfWork unitOfWork, Channel<Work> channel)
        {
            this.repository = repository ?? throw new System.ArgumentNullException(nameof(repository));
            this.unitOfWork = unitOfWork ?? throw new System.ArgumentNullException(nameof(unitOfWork));
            this.channel = channel ?? throw new System.ArgumentNullException(nameof(channel));
        }

        public async Task Execute(WorkRequest request)
        {
            var work = new Work { Data = request.Data };

            using var conn = unitOfWork.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            await repository.Save(work, conn);
            await channel.Writer.WriteAsync(work);
            trans.Commit();
        }
    }
}
