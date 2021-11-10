using ResilientWebApi.Domain.Adapters;
using System.Data;

namespace ResilientWebApi.Adapters.Persistence
{
    internal interface IDbContext : IUnitOfWork
    {
        string Attach(IDbConnection conn, ConnectionTarget target);
    }
}