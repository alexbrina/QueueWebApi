using Resilient.Domain.Adapters;
using System.Data;

namespace Resilient.Adapters.Storage
{
    internal interface IDbContext : IUnitOfWork
    {
        string Attach(IDbConnection conn, ConnectionTarget target);
    }
}