using Sandbox.Feature.Catalog.Models;
using Sitecore.Commerce.XA.Foundation.Catalog.Managers;
using Sitecore.Commerce.XA.Foundation.Common;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Models;
using Sitecore.Commerce.XA.Foundation.Common.Search;
using Sitecore.Commerce.XA.Foundation.Connect;
using Sitecore.Commerce.XA.Foundation.Connect.Managers;

namespace Sandbox.Feature.Catalog.Repositories
{
    /// <remarks>
    /// Implement from new BaseCatalogVariantsRepository instead of BaseCatalogRepository to support rendering variants
    /// </remarks>
    public class ProductImagesRepository : BaseCatalogVariantsRepository, IProductImagesRepository
    {
        public ProductImagesRepository(
            IModelProvider modelProvider,
            IStorefrontContext storefrontContext,
            ISiteContext siteContext,
            ISearchInformation searchInformation,
            ISearchManager searchManager,
            ICatalogManager catalogManager,
            ICatalogUrlManager catalogUrlManager,
            IContext context)
      : base(modelProvider, storefrontContext, siteContext, searchInformation, searchManager, catalogManager, catalogUrlManager, context)
        {
        }

        public CatalogItemVariantsRenderingModel GetProductImagesRenderingModel(IVisitorContext visitorContext, StringPropertyCollection propertyBag = null)
        {
            return GetProduct(visitorContext);
        }
    }
}
