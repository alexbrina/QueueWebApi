using ResilientWebApi.Domain.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ResilientWebApi.Domain.Adapters
{
    internal interface IWorkRepository
    {
        Task SaveRequested(Work work);

        Task SetCompleted(Work work, IDbConnection conn);

        Task<IEnumerable<Work>> GetPending();
    }
}
