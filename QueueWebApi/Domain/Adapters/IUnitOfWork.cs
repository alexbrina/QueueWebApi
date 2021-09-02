using System.Data;

namespace QueueWebApi.Domain.Adapters
{
    internal interface IUnitOfWork
    {
        IDbConnection GetConnection();
    }
}
