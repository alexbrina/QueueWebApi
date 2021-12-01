using System.Data;

namespace Resilient.Domain.Adapters
{
    internal interface IUnitOfWork
    {
        IDbConnection GetConnection(ConnectionTarget target);
    }

    internal enum ConnectionTarget
    {
        Work = 1,
        WorkOutbox = 2
    }
}
