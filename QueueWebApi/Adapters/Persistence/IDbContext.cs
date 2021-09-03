using QueueWebApi.Domain.Adapters;
using System.Data;

namespace QueueWebApi.Adapters.Persistence
{
    internal interface IDbContext : IUnitOfWork
    {
        string Attach(IDbConnection conn, ConnectionTarget target);
    }
}