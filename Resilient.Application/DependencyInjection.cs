using Resilient.Application;
using Resilient.Domain.Models;
using System;
using System.Threading.Channels;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var channelOptions = new UnboundedChannelOptions()
            {
                SingleReader = false,
                SingleWriter = false
            };

            services.AddSingleton(Channel.CreateUnbounded<Work>(channelOptions));
            services.AddScoped<IWorkUseCase, WorkUseCase>();
            services.AddScoped<IWorkOperator, WorkOperator>();
            services.AddHostedService<WorkBackgroundService>();

            return services;
        }
    }
}
