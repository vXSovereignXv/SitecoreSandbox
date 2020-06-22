using Sitecore.Data.Items;
using Sitecore.XA.Foundation.IoC;
using Sitecore.XA.Foundation.Mvc.Repositories.Base;

namespace Sandbox.Feature.Composites.Repositories
{
    public interface IRulesBasedSnippetRepository : IModelRepository, IControllerRepository, IAbstractRepository<IRenderingModelBase>
    {
        Item GetRulesBasedSnippetDataSource(Item rulesBasedSnippetSnippetItem, Item contextItem);
    }
}