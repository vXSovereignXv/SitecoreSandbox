using System.Web;
using Sandbox.Feature.Catalog.Models;
using Sitecore;
using Sitecore.Commerce.Entities.Inventory;
using Sitecore.Commerce.XA.Feature.Catalog;
using Sitecore.Commerce.XA.Foundation.Connect.Entities;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Sandbox.Feature.Catalog.MockData
{
    public static class CatalogItemVariantsRenderingModelMockData
    {
        public static CatalogItemVariantsRenderingModel InitializeMockData(CatalogItemVariantsRenderingModel model)
        {
            Assert.ArgumentNotNull(model, nameof(model));
            model.Initialize(GetProductEntity(), true);
            model.DisplayName = "Lorem ipsum dolor sit amet";
            model.DisplayNameRender = new HtmlString(model.DisplayName);
            model.Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed non ultrices risus. Mauris tincidunt urna at ligula viverra mollis. Pellentesque nulla velit, fermentum ac sollicitudin ut, tincidunt non nisl. Ut sagittis faucibus nibh id finibus. Praesent finibus sed ex id rhoncus. Praesent quam elit, accumsan sit amet tincidunt pulvinar, feugiat vitae velit. Aenean sed vestibulum eros.";
            model.DescriptionRender = new HtmlString(model.Description);
            model.Link = "/";
            MediaItem mediaItem = Context.Database.GetItem(CatalogFeatureConstants.MockDataItems.MockProductId);
            model.Images.Add(mediaItem);
            return model;
        }

        private static ProductEntity GetProductEntity()
        {
            ProductEntity productEntity = new ProductEntity()
            {
                ListPrice = new decimal?(new decimal(1495, 0, 0, false, 2)),
                AdjustedPrice = new decimal?(new decimal(1295, 0, 0, false, 2)),
                LowestPricedVariantListPrice = new decimal?(new decimal(1285, 0, 0, false, 2)),
                LowestPricedVariantAdjustedPrice = new decimal?(new decimal(1295, 0, 0, false, 2))
            };
            productEntity.LowestPricedVariantListPrice = new decimal?(new decimal(1495, 0, 0, false, 2));
            productEntity.HighestPricedVariantAdjustedPrice = new decimal?(new decimal(1595, 0, 0, false, 2));
            productEntity.ProductId = "12345";
            productEntity.StockStatus = StockStatus.InStock;
            productEntity.StockStatusName = "In Stock";
            productEntity.CustomerAverageRating = new decimal(3);
            return productEntity;
        }
    }
}
