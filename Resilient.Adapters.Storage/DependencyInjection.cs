using Resilient.Adapters.Storage;
using Resilient.Domain.Adapters;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjection
    {
        public static IServiceCollection AddStorageAdapter(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddScoped<IUnitOfWork, SqliteDbContext>();
            services.AddScoped<IDbContext, SqliteDbContext>();

            services.AddScoped<IWorkRepository, WorkRepositoryBetterApproach>();
            DatabaseSchema.Setup(new SqliteDbContext());

            return services;
        }
    }
}
