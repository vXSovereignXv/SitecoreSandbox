using System.Xml.Linq;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Rules;
using Sitecore.XA.Foundation.Abstractions;
using Sitecore.XA.Foundation.DynamicPlaceholders.Repositories;
using Sitecore.XA.Foundation.IoC;
using Sitecore.XA.Foundation.Mvc.Repositories.Base;
using Sandbox.Feature.Composites.Rules;
using Sandbox.Feature.Composites.Models;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Constants = Sitecore.XA.Feature.Composites.Constants;

namespace Sandbox.Feature.Composites.Repositories
{
    public class RulesBasedSnippetRepository : DynamicPlaceholdersRepository, IRulesBasedSnippetRepository, IModelRepository, IControllerRepository, IAbstractRepository<IRenderingModelBase>
    {
        private readonly IPageMode pageMode;
        private readonly ISiteContext siteContext;

        protected virtual bool HasLoopInDatasource => !string.IsNullOrEmpty(Rendering.Properties[Constants.CompositeLoopAttr]);
        protected virtual Item CompositeItem => Rendering.DataSourceItem;

        public RulesBasedSnippetRepository(IPageMode pageMode, ISiteContext siteContext)
        {
            this.pageMode = pageMode;
            this.siteContext = siteContext;
        }

        public override IRenderingModelBase GetModel()
        {
            var renderingModel = new RulesBasedSnippetRenderingModel();
            FillBaseProperties(renderingModel);

            return renderingModel;
        }

        public virtual Item GetRulesBasedSnippetDataSource(Item rulesBasedSnippetSnippetItem, Item contextItem)
        {
            var rulesValue = rulesBasedSnippetSnippetItem[Templates.RulesBasedSnippetSnippet.Fields.SnippetRules];
            if(string.IsNullOrWhiteSpace(rulesValue))
            {
                return null;
            }

            var commerceContextItem = siteContext.CurrentCatalogItem ?? contextItem;
            var rules = RuleFactory.ParseRules<RuleContext>(contextItem.Database, XElement.Parse(rulesValue));
            var ruleContext = new RuleContext()
            {
                Item = commerceContextItem
            };

            if (rules.Rules.Any())
            {
                foreach (var rule in rules.Rules)
                {
                    if (rule.Condition != null)
                    {
                        var stack = new RuleStack();
                        rule.Condition.Evaluate(ruleContext, stack);

                        if (ruleContext.IsAborted)
                        {
                            continue;
                        }
                        if ((stack.Count != 0) && ((bool)stack.Pop()))
                        {
                            rule.Execute(ruleContext);
                            var action = rule.Actions.FirstOrDefault();
                            var snippetId = action is SelectSnippet<RuleContext> ? ((SelectSnippet<RuleContext>)action)?.SnippetId : string.Empty;
                            return !string.IsNullOrEmpty(snippetId) ? contextItem.Database.GetItem(ID.Parse(snippetId)) : null;
                        }
                    }
                    else
                    {
                        rule.Execute(ruleContext);
                    }
                }
            }

            return null;
        }

        protected override void FillBaseProperties(object model)
        {
            base.FillBaseProperties(model);
            var renderingModel = (RulesBasedSnippetRenderingModel)model;
            if (Rendering.DataSourceItem != null)
            {
                renderingModel.CompositeItem = CompositeItem;
            }
            renderingModel.HasCompositeLoop = HasLoopInDatasource;
            renderingModel.ShowPlacholderMessage = !pageMode.IsNormal;
        }
    }
}