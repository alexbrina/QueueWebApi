using Resilient.Domain.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Resilient.Domain.Adapters
{
    internal interface IWorkRepository
    {
        Task SaveRequested(Work work);

        Task SetCompleted(Work work, IDbConnection conn, IDbTransaction trans);

        Task<IEnumerable<Work>> GetPending();
    }
}
