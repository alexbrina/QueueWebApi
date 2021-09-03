using System.Data;

namespace QueueWebApi.Domain.Adapters
{
    internal interface IUnitOfWork
    {
        IDbConnection GetConnection(ConnectionTarget target);
    }

    internal enum ConnectionTarget
    {
        WorkRequested = 1,
        WorkCompleted = 2
    }
}
