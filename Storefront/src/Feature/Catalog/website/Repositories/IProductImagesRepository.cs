using Sandbox.Feature.Catalog.Models;
using Sitecore.Commerce.XA.Foundation.Common;
using Sitecore.Commerce.XA.Foundation.Connect;

namespace Sandbox.Feature.Catalog.Repositories
{
    public interface IProductImagesRepository
    {
        CatalogItemVariantsRenderingModel GetProductImagesRenderingModel(IVisitorContext visitorContext, StringPropertyCollection propertyBag = null);
    }
}
