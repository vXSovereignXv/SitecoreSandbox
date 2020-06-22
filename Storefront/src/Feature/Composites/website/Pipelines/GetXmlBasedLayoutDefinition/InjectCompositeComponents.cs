using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Sandbox.Feature.Composites.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Layouts;
using Sitecore.Mvc.Extensions;
using Sitecore.Mvc.Pipelines.Response.GetXmlBasedLayoutDefinition;
using Sitecore.Pipelines;
using Sitecore.Pipelines.ResolveRenderingDatasource;
using Sitecore.XA.Feature.Composites.Services;
using Sitecore.XA.Foundation.Abstractions;
using Sitecore.XA.Foundation.Caching;
using Sitecore.XA.Foundation.Multisite;
using Sitecore.XA.Foundation.Presentation.Layout;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
using Sitecore.Xml;
using Constants = Sitecore.XA.Feature.Composites.Constants;

namespace Sandbox.Feature.Composites.Pipelines.GetXmlBasedLayoutDefinition
{
    public class InjectCompositeComponents : Sitecore.XA.Feature.Composites.Pipelines.GetXmlBasedLayoutDefinition.InjectCompositeComponents
    {
        private readonly IRulesBasedSnippetRepository rulesBasedSnippetRepository;
        private readonly ICompositeService compositeService;
        private readonly IPageMode pageMode;

        public InjectCompositeComponents(
            IRulesBasedSnippetRepository rulesBasedSnippetRepository, 
            ICompositeService compositeService,
            IPageMode pageMode)
        {
            this.rulesBasedSnippetRepository = rulesBasedSnippetRepository;
            this.compositeService = compositeService;
            this.pageMode = pageMode;
        }

        public override void Process(GetXmlBasedLayoutDefinitionArgs args)
        {
            Item currentItem = args.ContextItem ?? args.PageContext.Item ?? Sitecore.Mvc.Presentation.PageContext.Current.Item;
            XElement result = args.Result;
            if (result == null || !currentItem.Paths.IsContentItem)
            {
                return;
            }

            Item siteItem = ServiceLocator.ServiceProvider.GetService<IMultisiteContext>().GetSiteItem(currentItem);
            if (siteItem == null)
            {
                return;
            }

            List<XElement> compositeComponents = GetCompositeComponents(result).ToList();
            if (!compositeComponents.Any())
            {
                return;
            }

            var renderingIds = compositeComponents.Select(c => c.ToXmlNode()).Select(n => ParseRenderingId(n)).ToList();
            DictionaryCacheValue dictionaryCacheValue = DictionaryCache.Get(CreateCompositesXmlCacheKey(args, currentItem, siteItem, renderingIds));
            if (PageMode.IsNormal && dictionaryCacheValue != null && dictionaryCacheValue.Properties.ContainsKey(Constants.CompositesXmlPropertiesKey))
            {
                args.Result = XElement.Parse(dictionaryCacheValue.Properties[Constants.CompositesXmlPropertiesKey].ToString());
            }
            else
            {
                if (!args.CustomData.ContainsKey(Constants.CompositeRecursionLevel))
                {
                    args.CustomData.Add(Constants.CompositeRecursionLevel, 1);
                }
                else
                {
                    args.CustomData[Constants.CompositeRecursionLevel] = (int)args.CustomData[Constants.CompositeRecursionLevel] + 1;
                }

                LayoutDefinition definition = null;
                bool hasPersonalizedRenderings = false;
                if (ShouldMigratePersonalizationSettings())
                {
                    definition = GetLayoutDefinitionWithPersonalizationOnly(result, out hasPersonalizedRenderings);
                }

                foreach (XElement rendering in compositeComponents)
                {
                    ProcessCompositeComponent(args, rendering, result);
                }

                List<XElement> xelementList = result.Descendants("d").ToList();
                if (ShouldMigratePersonalizationSettings() & hasPersonalizedRenderings)
                {
                    xelementList = MigratePersonalizationSettings(result, definition);
                }

                args.Result.Descendants("d").Remove();
                args.Result.Add(xelementList);
                if (!PageMode.IsNormal || HasPersonalizationRules(args) || HasMVTests(args))
                {
                    return;
                }

                StoreValueInCache(CreateCompositesXmlCacheKey(args, currentItem, siteItem, renderingIds), args.Result.ToString());
            }
        }

        protected override Item ResolveCompositeDatasource(string datasource, Item contextItem)
        {
            ID datasourceId;
            if (ID.TryParse(datasource, out datasourceId))
            {
                var datasourceItem = Context.Database.GetItem(datasourceId);
                if(datasourceItem.TemplateID == Templates.RulesBasedSnippetSnippet.ID)
                {
                    var rulesBasedDatasource = rulesBasedSnippetRepository.GetRulesBasedSnippetDataSource(datasourceItem, contextItem);
                    return rulesBasedDatasource ?? datasourceItem;
                }
                else
                {
                    return datasourceItem;
                }
            }

            ResolveRenderingDatasourceArgs renderingDatasourceArgs = new ResolveRenderingDatasourceArgs(datasource);
            if (contextItem != null)
            {
                renderingDatasourceArgs.CustomData.Add(nameof(contextItem), contextItem);
            }

            CorePipeline.Run("resolveRenderingDatasource", renderingDatasourceArgs);
            return Context.Database.GetItem(renderingDatasourceArgs.Datasource);
        }

        protected override void ProcessCompositeComponent(
            GetXmlBasedLayoutDefinitionArgs args,
            XElement rendering,
            XElement layoutXml)
        {
            var xmlNode = rendering.ToXmlNode();
            var renderingModel = new RenderingModel(xmlNode);
            var contextItem = args.PageContext.Item;
            var renderingReference = new RenderingReference(xmlNode, contextItem.Language, contextItem.Database);
            string datasource = renderingModel.DataSource;
            if (TestMVTestEnabled(renderingReference) && ContentTestingService != null && renderingModel.MultiVariateTestId != (ID)null)
            {
                args.CustomData.Add(Constants.MultivariateTestEnabled, true);
                var multivariateTestDatasource = ContentTestingService.GetMultivariateTestDatasource(renderingModel.MultiVariateTestId, args.PageContext.Item, args.PageContext.Device.DeviceItem.ID);
                if (multivariateTestDatasource != null)
                {
                    datasource = multivariateTestDatasource;
                }
            }

            if (TestPersonalizationEnabled(renderingReference))
            {
                datasource = GetCustomizedRenderingDataSource(renderingModel, contextItem);
            }

            if (string.IsNullOrEmpty(datasource))
            {
                Log.Warn("Composite component datasource is empty", rendering);
            }
            else
            {
                if (datasource.StartsWith("local:", StringComparison.OrdinalIgnoreCase))
                {
                    contextItem = SwitchContextItem(rendering, contextItem);
                }
                if (datasource.StartsWith("page:", StringComparison.OrdinalIgnoreCase))
                {
                    contextItem = Context.Item;
                }
                var datasourceItem = ResolveCompositeDatasource(datasource, contextItem) ?? ResolveCompositeDatasource(datasource, Context.Item);
                if (datasourceItem == null)
                {
                    Log.Error("Composite component has a reference to non-existing datasource : " + datasource, this);
                }
                else
                {
                    var dynamicPlaceholderId = ExtractDynamicPlaceholderId(args, rendering);
                    var sid = string.Empty;
                    if (rendering.Attribute("sid") != null)
                    {
                        sid = rendering.Attribute("sid").Value;
                    }

                    var placeholder = renderingModel.Placeholder;
                    if (!DetectDatasourceLoop(args, datasourceItem))
                    {
                        var composites = compositeService.GetCompositeItems(datasourceItem).Select((item, idx) => new KeyValuePair<int, Item>(idx + 1, item)).ToList();
                        foreach (KeyValuePair<int, Item> composite in composites)
                        {
                            if (!TryMergeComposites(args, rendering, layoutXml, composite, dynamicPlaceholderId, placeholder, sid))
                            {
                                break;
                            } 
                        }
                    }
                    else
                    {
                        AbortRecursivePipeline(args, rendering);
                    }
                        
                    RollbackAntiLoopCollection(args, datasourceItem);
                }
            }
        }

        protected virtual internal string CreateCompositesXmlCacheKey(GetXmlBasedLayoutDefinitionArgs args, Item currentItem, Item siteItem, IList<ID> renderingIds)
        {
            var pagePath = args.PageContext.RequestContext.HttpContext.Request.Path;
            //Cache the layout for each path if it's a wildcard page and there is a rules based snippet on the page
            if (currentItem.Name == "*" && renderingIds.Any(r => r == Renderings.RulesBasedSnippet.ID))
            {
                return $"SXA::{Constants.CompositesXmlPropertiesKey}::{siteItem.ID}::{Context.Database.Name}::{Context.Device.ID}::{Context.Language.Name}::{currentItem.ID}::{pagePath}";
            }
            else
            {
                return $"SXA::{Constants.CompositesXmlPropertiesKey}::{siteItem.ID}::{Context.Database.Name}::{Context.Device.ID}::{Context.Language.Name}::{currentItem.ID}";
            }
        }

        protected virtual ID ParseRenderingId(XmlNode configNode)
        {
            if (configNode == null)
            {
                return ID.Null;
            }

            return MainUtil.GetID(XmlUtil.GetAttribute("id", configNode), ID.Null);
        }

        protected virtual internal void SetAttribute(XElement rendering, string name, string value)
        {
            var attribute = rendering.Attribute(name);
            if (attribute != null)
            {
                attribute.Value = value;
            }
            else
            {
                attribute = new XAttribute(name, value);
                rendering.Add(attribute);
            }
        }

        protected override List<XElement> MigratePersonalizationSettings(XElement layoutXml, LayoutDefinition definition)
        {
            LayoutDefinition layoutDefinition = LayoutDefinition.Parse(layoutXml.ToString());
            foreach (DeviceDefinition device in definition.Devices)
            {
                foreach (RenderingDefinition rendering in device.Renderings)
                {
                    var deviceRendering = layoutDefinition?.GetDevice(device.ID)?.GetRenderingByUniqueId(rendering.UniqueId);
                    if(deviceRendering != null)
                    {
                        deviceRendering.Rules = rendering.Rules;
                    }
                    
                }
            }
            return layoutDefinition.ToXml().ToXmlNode().ToXElement().Descendants((XName)"d").ToList();
        }
    }
}