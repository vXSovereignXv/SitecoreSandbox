using System.Web.Mvc;
using System.Web.SessionState;
using Sandbox.Feature.Catalog.Repositories;
using Sitecore.Commerce.XA.Foundation.Common.Attributes;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Controllers;
using Sitecore.Commerce.XA.Foundation.Connect;

namespace Sandbox.Feature.Catalog.Controllers
{
    public class CatalogVariantsController : BaseCommerceStandardController
    {
        private readonly IProductImagesRepository productImagesRepository;
        private readonly IVisitorContext visitorContext;

        public CatalogVariantsController(
            IProductImagesRepository productImagesRepository,
            IVisitorContext visitorContext,
            IStorefrontContext storefrontContext,
            IContext sitecoreContext)
            :base(storefrontContext, sitecoreContext)
        {
            this.productImagesRepository = productImagesRepository;
            this.visitorContext = visitorContext;
        }

        [StorefrontSessionState(SessionStateBehavior.ReadOnly)]
        public ActionResult ProductImages()
        {
            return View(GetRenderingView($"{nameof(ProductImages)}Variants"), productImagesRepository.GetProductImagesRenderingModel(visitorContext, null));
        }
    }
}
