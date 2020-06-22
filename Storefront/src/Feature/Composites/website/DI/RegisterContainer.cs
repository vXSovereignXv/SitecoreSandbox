using Microsoft.Extensions.DependencyInjection;
using Sandbox.Feature.Composites.Repositories;
using Sitecore.DependencyInjection;

namespace Sandbox.Feature.Composites.DI
{
    public class RegisterContainer : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IRulesBasedSnippetRepository, RulesBasedSnippetRepository>();
        }
    }
}
