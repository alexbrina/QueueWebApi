using QueueWebApi.Domain.Adapters;

namespace QueueWebApi.Adapters.Persistence
{
    internal interface IDbContext : IUnitOfWork
    {
    }
}