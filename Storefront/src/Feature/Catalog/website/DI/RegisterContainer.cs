using Microsoft.Extensions.DependencyInjection;
using Sandbox.Feature.Catalog.Repositories;
using Sitecore.DependencyInjection;

namespace Sandbox.Feature.Catalog.DI
{
    public class RegisterContainer : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IProductImagesRepository, ProductImagesRepository>();
        }
    }
}
