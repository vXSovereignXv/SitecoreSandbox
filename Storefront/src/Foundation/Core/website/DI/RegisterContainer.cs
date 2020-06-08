
using Sandbox.Foundation.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;

namespace Sandbox.Foundation.Core.DI
{
    public class RegisterContainer : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IMediatorService, MediatorService>();
        }
    }
}
