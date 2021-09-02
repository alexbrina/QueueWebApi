using QueueWebApi.Domain.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace QueueWebApi.Domain.Adapters
{
    internal interface IWorkRepository
    {
        Task Save(Work work, IDbConnection conn);

        Task Update(Work work, IDbConnection conn);

        Task<IEnumerable<Work>> GetRequested();
    }
}
