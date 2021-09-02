using QueueWebApi.Adapters.Persistence;
using QueueWebApi.Domain.Adapters;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjection
    {
        public static IServiceCollection AddAdapters(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddScoped<IWorkRepository, WorkRepository>();
            services.AddScoped<IUnitOfWork, SqliteDbContext>();
            services.AddScoped<IDbContext, SqliteDbContext>();

            // ensure local file database and table
            DatabaseSchema.Setup(new SqliteDbContext());

            return services;
        }
    }
}
