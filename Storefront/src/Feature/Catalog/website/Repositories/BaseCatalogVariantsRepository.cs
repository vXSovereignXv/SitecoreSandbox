using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Web;
using Sandbox.Feature.Catalog.MockData;
using Sandbox.Feature.Catalog.Models;
using Sandbox.Foundation.CommerceCommon.Models;
using Sitecore.Commerce.XA.Foundation.Catalog.Managers;
using Sitecore.Commerce.XA.Foundation.Common;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Models;
using Sitecore.Commerce.XA.Foundation.Common.Search;
using Sitecore.Commerce.XA.Foundation.Connect;
using Sitecore.Commerce.XA.Foundation.Connect.Entities;
using Sitecore.Commerce.XA.Foundation.Connect.Managers;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.XA.Foundation.RenderingVariants.Repositories;

namespace Sandbox.Feature.Catalog.Repositories
{
    /// <remarks>
    /// Reimplementation of Sitecore.Commerce.XA.Foundation.Common.Repositories.BaseCommerceModelRepository and
    /// Sitecore.Commerce.XA.Feature.Catalog.Repositories.BaseCatalogRepository to support variants
    /// 
    /// *Replace all occurances of BaseCommerceRenderingModel with BaseCommerceVariantsRenderingModel
    /// *Replace all occurances of CatalogItemVariantsRenderingModel with CatalogItemVariantsRenderingModel
    /// *Other changes from OOTB version marked with 'Important:' comment
    /// </summary>
    public class BaseCatalogVariantsRepository : VariantsRepository
    {
        //Important: need to change so we don't collide with OOTB rendering
        protected const string CurrentCatalogItemVariantsRenderingModelKeyName = "CurrentCatalogItemVariantsRenderingModel";

        public BaseCatalogVariantsRepository(
          IModelProvider modelProvider,
          IStorefrontContext storefrontContext,
          ISiteContext siteContext,
          ISearchInformation searchInformation,
          ISearchManager searchManager,
          ICatalogManager catalogManager,
          ICatalogUrlManager catalogUrlManager,
          IContext context)
        {
            Assert.ArgumentNotNull(modelProvider, nameof(modelProvider));
            Assert.ArgumentNotNull(storefrontContext, nameof(storefrontContext));
            Assert.ArgumentNotNull(siteContext, nameof(siteContext));
            Assert.ArgumentNotNull(searchInformation, nameof(searchInformation));
            Assert.ArgumentNotNull(searchManager, nameof(searchManager));
            Assert.ArgumentNotNull(catalogManager, nameof(catalogManager));
            Assert.ArgumentNotNull(catalogUrlManager, nameof(catalogUrlManager));
            Assert.IsNotNull(context, nameof(context));
            ModelProvider = modelProvider;
            StorefrontContext = storefrontContext;
            SiteContext = siteContext;
            SearchInformation = searchInformation;
            SearchManager = searchManager;
            CatalogManager = catalogManager;
            CatalogUrlManager = catalogUrlManager;
            Context = context;
        }

        public ICatalogManager CatalogManager { get; protected set; }

        public ICatalogUrlManager CatalogUrlManager { get; protected set; }

        public new IContext Context { get; }

        public IModelProvider ModelProvider { get; protected set; }

        public ISearchInformation SearchInformation { get; protected set; }

        public ISearchManager SearchManager { get; protected set; }

        public ISiteContext SiteContext { get; protected set; }

        public IStorefrontContext StorefrontContext { get; protected set; }

        private bool IsGiftCardProductPage
        {
            get
            {
                return Convert.ToBoolean(HttpContext.Current.Items[nameof(IsGiftCardProductPage)], CultureInfo.InvariantCulture);
            }
            set
            {
                HttpContext.Current.Items[nameof(IsGiftCardProductPage)] = value;
            }
        }

        public void Init(BaseCommerceVariantsRenderingModel model)
        {
            FillBaseProperties(model);
        }

        protected virtual CatalogItemVariantsRenderingModel GetProduct(
          IVisitorContext visitorContext)
        {
            Item currentCatalogItem = SiteContext.CurrentCatalogItem;
            CatalogItemVariantsRenderingModel itemRenderingModel;
            if (currentCatalogItem != null)
            {
                Item productItem = currentCatalogItem;
                itemRenderingModel = GetCatalogItemVariantsRenderingModel(visitorContext, productItem);
                itemRenderingModel.Item = currentCatalogItem;
            }
            else
            {
                //Important: need to get the new variant model
                var model = ModelProvider.GetModel<CatalogItemVariantsRenderingModel>();
                Init(model);

                //Important: need to create a variant version of mock data rendering model
                itemRenderingModel = CatalogItemVariantsRenderingModelMockData.InitializeMockData(model);
                itemRenderingModel.Item = Context.Item; //Need a mock catalog item
            }
            return itemRenderingModel;
        }

        protected virtual CatalogItemVariantsRenderingModel GetCatalogItemVariantsRenderingModel(
          IVisitorContext visitorContext,
          Item productItem)
        {
            Assert.ArgumentNotNull(visitorContext, nameof(visitorContext));
            //Important: make sure to use the CurrentCatalogItemVariantsRenderingModelKeyName constant here
            if (SiteContext.Items[CurrentCatalogItemVariantsRenderingModelKeyName] != null)
                return (CatalogItemVariantsRenderingModel)SiteContext.Items[CurrentCatalogItemVariantsRenderingModelKeyName];
            CommerceStorefront currentStorefront = StorefrontContext.CurrentStorefront;
            List<VariantEntity> variantEntityList = new List<VariantEntity>();
            if (productItem != null && productItem.HasChildren)
            {
                foreach (Item child in productItem.Children)
                {
                    VariantEntity model = ModelProvider.GetModel<VariantEntity>();
                    model.Initialize(child);
                    variantEntityList.Add(model);
                }
            }
            ProductEntity productEntity = ModelProvider.GetModel<ProductEntity>();
            productEntity.Initialize(currentStorefront, productItem, variantEntityList);
            CatalogItemVariantsRenderingModel catalogModel = ModelProvider.GetModel<CatalogItemVariantsRenderingModel>();
            Init(catalogModel);
            if (SiteContext.UrlContainsCategory)
            {
                catalogModel.ParentCategoryId = CatalogUrlManager.ExtractCategoryNameFromCurrentUrl();
                Item category = SearchManager.GetCategory(catalogModel.ParentCategoryId, currentStorefront.Catalog);
                if (category != null)
                    catalogModel.ParentCategoryName = category.DisplayName;
            }
            if (string.Equals(catalogModel.ProductId, currentStorefront.GiftCardProductId, StringComparison.Ordinal))
            {
                catalogModel.GiftCardAmountOptions = GetGiftCardAmountOptions(visitorContext, currentStorefront, productEntity);
            }
            else
            {
                CatalogManager.GetProductPrice(currentStorefront, visitorContext, productEntity, null);
                catalogModel.CustomerAverageRating = CatalogManager.GetProductRating(productItem, null);
            }
            catalogModel.Initialize(productEntity, false);
            SiteContext.Items["CurrentCatalogItemVariantsRenderingModel"] = catalogModel;
            return catalogModel;
        }

        protected SearchResults GetChildProducts(
          CommerceSearchOptions searchOptions,
          Item item)
        {
            Assert.ArgumentNotNull(item, nameof(item));
            Assert.ArgumentNotNull(searchOptions, nameof(searchOptions));
            SearchResults searchResults = null;
            string str = string.Format(CultureInfo.InvariantCulture, "ChildProductSearch_{0}", item.ID.ToString());
            if (!SiteContext.Items.Contains(str))
            {
                if (item != null)
                    searchResults = !string.IsNullOrWhiteSpace(searchOptions.SearchKeyword) ? SearchManager.SearchCatalogItemsByKeyword(searchOptions.SearchKeyword, searchOptions.CatalogName, searchOptions) : SearchManager.GetCategoryProducts(item.ID, searchOptions);
                SiteContext.Items[str] = searchResults;
            }
            return (SearchResults)SiteContext.Items[str];
        }

        protected virtual int GetDefaultItemsPerPage(
          int? pageSize,
          CategorySearchInformation searchInformation)
        {
            Assert.ArgumentNotNull(searchInformation, nameof(searchInformation));
            int defaultValue = StorefrontContext.CurrentStorefront.DefaultItemsPerPage;
            if (defaultValue <= 0)
                defaultValue = searchInformation.ItemsPerPage;
            if (defaultValue <= 0)
                defaultValue = 12;
            return pageSize.GetValueOrDefault(defaultValue);
        }

        protected virtual ICollection<KeyValuePair<string, decimal?>> GetGiftCardAmountOptions(
          IVisitorContext visitorContext,
          CommerceStorefront storefront,
          ProductEntity productEntity)
        {
            Dictionary<string, decimal?> source = new Dictionary<string, decimal?>();
            if (productEntity == null || productEntity.Variants == null)
                return null;
            CatalogManager.GetProductPrice(storefront, visitorContext, productEntity, null);
            foreach (VariantEntity variant in productEntity.Variants)
                source.Add(variant.VariantId, new decimal?(Math.Round(variant.AdjustedPrice ?? decimal.Zero, 2)));
            return source.ToList();
        }

        [SuppressMessage("Microsoft.Design", "CA1045: Do not pass types by reference")]
        protected virtual void GetSortParameters(
          CategorySearchInformation categorySearchInformation,
          ref string sortField,
          ref SortDirection? sortOrder)
        {
            Assert.ArgumentNotNull(categorySearchInformation, nameof(categorySearchInformation));
            if (!string.IsNullOrWhiteSpace(sortField))
                return;
            IList<CommerceQuerySort> sortFields = categorySearchInformation.SortFields;
            if (sortFields == null || sortFields.Count <= 0)
                return;
            sortField = sortFields[0].Name;
            sortOrder = new SortDirection?(SortDirection.Asc);
        }

        protected virtual bool IsGiftCardPageRequest()
        {
            if (IsGiftCardProductPage)
                return true;
            string lowerInvariant = Context.Language.ToString().ToLowerInvariant();
            string str1 = HttpContext.Current.Request.Url.AbsolutePath.ToLowerInvariant().Replace(".aspx", string.Empty);
            if (str1.Contains(lowerInvariant))
                str1 = str1.Replace("/" + lowerInvariant, string.Empty);
            string str2 = StorefrontContext.CurrentStorefront.GiftCardPageLink?.ToLowerInvariant().Replace(".aspx", string.Empty);
            bool flag = str2 != null && str2.EndsWith(str1, StringComparison.OrdinalIgnoreCase);
            IsGiftCardProductPage = flag;
            return flag;
        }

        protected virtual void UpdateOptionsWithFacets(
          IEnumerable<CommerceQueryFacet> facets,
          string valueQueryString,
          CommerceSearchOptions productSearchOptions)
        {
            Assert.ArgumentNotNull(productSearchOptions, nameof(productSearchOptions));
            if (facets == null || !facets.Any())
                return;
            if (!string.IsNullOrEmpty(valueQueryString))
            {
                string str1 = HttpContext.Current.Server.UrlDecode(valueQueryString);
                if (str1 != null)
                {
                    string str2 = str1;
                    char[] chArray1 = new char[1] { '&' };
                    foreach (string str3 in str2.Split(chArray1))
                    {
                        string[] strArray = str3.Split('=');
                        string name = strArray[0];
                        CommerceQueryFacet commerceQueryFacet = facets.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                        if (commerceQueryFacet != null)
                        {
                            string str4 = strArray[1];
                            char[] chArray2 = new char[1] { '|' };
                            foreach (string str5 in str4.Split(chArray2))
                                commerceQueryFacet.Values.Add(str5);
                        }
                    }
                }
            }
            productSearchOptions.FacetFields = facets;
        }

        protected virtual void UpdateOptionsWithSorting(
          string sortField,
          SortDirection? sortDirection,
          CommerceSearchOptions productSearchOptions)
        {
            Assert.ArgumentNotNull(productSearchOptions, nameof(productSearchOptions));
            if (string.IsNullOrEmpty(sortField))
                return;
            productSearchOptions.SortField = sortField;
            if (!sortDirection.HasValue)
                return;
            productSearchOptions.SortDirection = sortDirection.Value;
        }
    }
}
