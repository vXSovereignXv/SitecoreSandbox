using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Web;
using Sandbox.Foundation.CommerceCommon.Models;
using Sitecore;
using Sitecore.Commerce.Entities.Inventory;
using Sitecore.Commerce.XA.Feature.Catalog.Models;
using Sitecore.Commerce.XA.Foundation.Catalog.Models;
using Sitecore.Commerce.XA.Foundation.Catalog.Utils;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Entities;
using Sitecore.Commerce.XA.Foundation.Common.ExtensionMethods;
using Sitecore.Commerce.XA.Foundation.Common.Models;
using Sitecore.Commerce.XA.Foundation.Common.Providers;
using Sitecore.Commerce.XA.Foundation.Connect.Entities;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Links;
using Sitecore.Mvc;
using Sitecore.Mvc.Presentation;
using Sitecore.Web;

namespace Sandbox.Feature.Catalog.Models
{
    /// <remarks>
    /// Reimplementation of Sitecore.Commerce.XA.Feature.Catalog.Models.CatalogItemRenderingModel to support variants
    /// </remarks>
    public class CatalogItemVariantsRenderingModel : BaseCommerceVariantsRenderingModel
    {
        public CatalogItemVariantsRenderingModel(
          IStorefrontContext storefrontContext,
          IItemTypeProvider itemTypeProvider,
          IModelProvider modelProvider,
          IVariantDefinitionProvider variantDefinitionProvider)
        {
            Assert.ArgumentNotNull(storefrontContext, nameof(storefrontContext));
            Assert.ArgumentNotNull(itemTypeProvider, nameof(itemTypeProvider));
            Assert.ArgumentNotNull(modelProvider, nameof(modelProvider));
            Assert.ArgumentNotNull(variantDefinitionProvider, nameof(variantDefinitionProvider));
            StorefrontContext = storefrontContext;
            CurrentStorefront = storefrontContext.CurrentStorefront;
            ItemTypeProvider = itemTypeProvider;
            ModelProvider = modelProvider;
            VariantDefinitionProvider = variantDefinitionProvider;
        }

        public decimal? AdjustedPrice { get; set; }

        public string AdjustedPriceWithCurrency
        {
            get
            {
                return !AdjustedPrice.HasValue ? string.Empty : AdjustedPrice.ToCurrency();
            }
        }

        public Item CatalogItem { get; set; }

        public string CatalogName { get; protected set; }

        public string CurrencySymbol { get; set; }

        public CommerceStorefront CurrentStorefront { get; protected set; }

        public decimal CustomerAverageRating { get; set; }

        public string Description { get; set; }

        public HtmlString DescriptionRender { get; set; }

        public string DisplayName { get; set; }

        public HtmlString DisplayNameRender { get; set; }

        public string Features { get; set; }

        public IEnumerable<KeyValuePair<string, decimal?>> GiftCardAmountOptions { get; set; }

        public decimal? HighestPricedVariantAdjustedPrice { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public ICollection<MediaItem> Images { get; protected set; }

        public bool IsCategory { get; set; }

        public bool IsOnSale
        {
            get
            {
                return CatalogItemHelper.IsOnSale(AdjustedPrice, ListPrice);
            }
        }

        public IItemTypeProvider ItemTypeProvider { get; protected set; }

        public string Link { get; set; }

        public decimal? ListPrice { get; set; }

        public string ListPriceWithCurrency
        {
            get
            {
                return !ListPrice.HasValue ? string.Empty : ListPrice.ToCurrency();
            }
        }

        public decimal? LowestPricedVariantAdjustedPrice { get; set; }

        public string LowestPricedVariantAdjustedPriceWithCurrency
        {
            get
            {
                return !LowestPricedVariantAdjustedPrice.HasValue ? string.Empty : LowestPricedVariantAdjustedPrice.ToCurrency();
            }
        }

        public decimal? LowestPricedVariantListPrice { get; set; }

        public string LowestPricedVariantListPriceWithCurrency
        {
            get
            {
                return !LowestPricedVariantListPrice.HasValue ? string.Empty : LowestPricedVariantListPrice.ToCurrency();
            }
        }

        public IModelProvider ModelProvider { get; protected set; }

        public string ParentCategoryId { get; set; }

        public string ParentCategoryName { get; set; }

        public string ProductId { get; set; }

        public decimal? Quantity { get; set; }

        public decimal SavingsPercentage
        {
            get
            {
                return CalculateSavingsPercentage(AdjustedPrice, ListPrice);
            }
        }

        public string StockAvailabilityDate { get; set; }

        public StockStatus StockStatus { get; set; }

        public string StockStatusName { get; set; }

        public IStorefrontContext StorefrontContext { get; protected set; }

        public IEnumerable<VariantViewModel> Variants { get; protected internal set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public List<BundledItemViewModel> BundledItems { get; protected internal set; }

        public IEnumerable<VariantDefinitionEntity> VariantDefinitions { get; protected internal set; }

        public bool IsBundle { get; set; }

        public IVariantDefinitionProvider VariantDefinitionProvider { get; protected set; }

        public ICollection<string> GetDistinctVariantPropertyValues(string propertyName)
        {
            return Variants.Select(variant => variant.Item[propertyName]).Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public void Initialize(ProductEntity product, bool initializeAsMock = false)
        {
            Assert.ArgumentNotNull((object)product, nameof(product));
            CatalogItem = product.Item;
            ProductId = product.ProductId;
            if (!initializeAsMock)
            {
                IsBundle = ItemTypeProvider.IsBundle(product.Item);
                IsCategory = ItemTypeProvider.GetItemType(CatalogItem) == Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes.Category;
                SetImages();
                string str = product.Item[FieldIDs.DisplayName.ToString()];
                DisplayName = string.IsNullOrEmpty(str) ? string.Empty : str;
                Features = CatalogItem["Features"];
                CatalogName = StorefrontContext.CurrentStorefront.Catalog;
                Description = CatalogItem["Description"];
                StockStatus = product.StockStatus;
                StockStatusName = product.StockStatusName;
                DisplayNameRender = PageContext.Current.HtmlHelper.Sitecore().Field(FieldIDs.DisplayName.ToString(), product.Item);
                DescriptionRender = PageContext.Current.HtmlHelper.Sitecore().Field("Description", product.Item);
                SetLink();
                if (IsBundle)
                {
                    List<BundledItemViewModel> bundledItemViewModelList = new List<BundledItemViewModel>();
                    NameValueCollection urlParameters = WebUtil.ParseUrlParameters(CatalogItem[Sitecore.Commerce.XA.Foundation.Common.Constants.ItemFieldNames.BundleItems]);
                    foreach (string index in urlParameters)
                    {
                        BundledItemViewModel model = ModelProvider.GetModel<BundledItemViewModel>();
                        Item obj = Context.Database.GetItem(ID.Parse(index));
                        model.Initialize(obj, System.Convert.ToDecimal(urlParameters[index], CultureInfo.InvariantCulture));
                        bundledItemViewModelList.Add(model);
                    }
                    BundledItems = bundledItemViewModelList;
                }
            }
            else
            {
                IsCategory = false;
                StockStatus = product.StockStatus;
                StockStatusName = product.StockStatusName;
                Images = new List<MediaItem>();
            }
            CurrencySymbol = CurrentStorefront.SelectedCurrency;
            AdjustedPrice = product.AdjustedPrice;
            HighestPricedVariantAdjustedPrice = product.HighestPricedVariantAdjustedPrice;
            ListPrice = product.ListPrice;
            LowestPricedVariantAdjustedPrice = product.LowestPricedVariantAdjustedPrice;
            LowestPricedVariantListPrice = product.LowestPricedVariantListPrice;
            CustomerAverageRating = product.CustomerAverageRating;
            if (product.Variants != null && product.Variants.Any<VariantEntity>())
            {
                List<VariantViewModel> variantViewModelList = new List<VariantViewModel>();
                foreach (VariantEntity variant in product.Variants)
                {
                    VariantViewModel model = ModelProvider.GetModel<VariantViewModel>();
                    model.Initialize(variant);
                    variantViewModelList.Add(model);
                }
                Variants = variantViewModelList;
            }
            if (initializeAsMock)
                return;
            VariantDefinitions = VariantDefinitionProvider.GetVariantDefinitions(CatalogItem);
        }

        public HtmlString RenderField(string fieldName)
        {
            Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));
            HtmlString htmlString = PageContext.Current.HtmlHelper.Sitecore().Field(fieldName, CatalogItem);
            if (fieldName.Equals("Features", StringComparison.OrdinalIgnoreCase) && (htmlString.ToString().Equals("Default", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(htmlString.ToString())) && (CatalogItem.HasChildren && CatalogItem.Children[0] != null))
                htmlString = PageContext.Current.HtmlHelper.Sitecore().Field("VariantFeatures", CatalogItem.Children[0]);
            return htmlString;
        }

        protected virtual decimal CalculateSavingsPercentage(
          decimal? adjustedPrice,
          decimal? listPrice)
        {
            return CatalogItemHelper.GetSavingsPercentage(adjustedPrice, listPrice);
        }

        protected virtual void SetImages()
        {
            if (Images != null)
                return;
            Images = new List<MediaItem>();
            MultilistField field = CatalogItem.Fields["Images"];
            if (field != null)
            {
                foreach (ID targetId in field.TargetIDs)
                    Images.Add(CatalogItem.Database.GetItem(targetId));
            }
            else
                Images.Add(CatalogItem.Database.GetItem(ID.Parse("{8B33A7DC-8680-46AC-A199-1419AF50C330}")));
        }

        protected virtual void SetLink()
        {
            Link = ProductId.Equals(CurrentStorefront.GiftCardProductId, StringComparison.OrdinalIgnoreCase) ? CurrentStorefront.GiftCardPageLink : LinkManager.GetItemUrl(CatalogItem);
        }
    }
}
