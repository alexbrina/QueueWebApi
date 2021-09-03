using QueueWebApi.Domain.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace QueueWebApi.Domain.Adapters
{
    internal interface IWorkRepository
    {
        Task SaveRequested(Work work, IDbConnection conn);

        Task SetCompleted(Work work, IDbConnection conn);

        Task<IEnumerable<Work>> GetPending();
    }
}
