using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Globalization;
using Sitecore.Pipelines;
using Sitecore.Pipelines.GetRenderingDatasource;
using Sitecore.Rules.RuleMacros;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using Sitecore.XA.Foundation.Abstractions;
using Sitecore.XA.Foundation.SitecoreExtensions.Repositories;

namespace Sandbox.Feature.Composites.Rules
{
    public class SelectSnippetMacro : IRuleMacro
    {
        protected IContext context => ServiceLocator.ServiceProvider.GetService<IContext>();

        protected IContentRepository contentRepository => ServiceLocator.ServiceProvider.GetService<IContentRepository>();

        public void Execute(XElement element, string name, UrlString parameters, string value)
        {
            Item contextItem = GetContextItem();
            if (contextItem == null)
            {
                SheerResponse.Alert(Translate.Text("Context item not found."), true);
                return;
            }

            var renderingItem = contentRepository.GetItem(Renderings.Snippet.ID);
            if (renderingItem == null)
            {
                SheerResponse.Alert(Translate.Text("Rendering item not found."), true);
            }

            var args = CreatePipelineArgs(renderingItem, contextItem);
            CorePipeline.Run("getRenderingDatasource", args);
            if (string.IsNullOrEmpty(args.DialogUrl))
            {
                SheerResponse.Alert("An error occurred.");
            }
            else
            {
                SheerResponse.ShowModalDialog(args.DialogUrl, "1200px", "660px", string.Empty, true);
            }
        }

        protected virtual Item GetContextItem()
        {
            NameValueCollection queryString1 = HttpUtility.ParseQueryString(WebUtil.GetQueryString());
            if (!queryString1.AllKeys.Contains("hdl"))
            {
                return null;
            }
            string index = queryString1["hdl"];
            if (context.HttpContext.Session[index] != null)
            {
                NameValueCollection queryString2 = HttpUtility.ParseQueryString((string)context.HttpContext.Session[index]);
                ID result;
                if (queryString2.AllKeys.Contains("ContextItemID") && ID.TryParse(queryString2["ContextItemID"], out result))
                {
                    return contentRepository.GetItem(result);
                }
            }
            return null;
        }

        private GetRenderingDatasourceArgs CreatePipelineArgs(Item renderingItem, Item contextItem)
        {
            GetRenderingDatasourceArgs renderingDatasourceArgs = new GetRenderingDatasourceArgs(renderingItem)
            {
                FallbackDatasourceRoots = new List<Item>()
                {
                    context.ContentDatabase.GetRootItem()
                },
                ContentLanguage = contextItem.Language,
                ContextItemPath = contextItem != null ? contextItem.Paths.FullPath : string.Empty,
                ShowDialogIfDatasourceSetOnRenderingItem = true
            };
            return renderingDatasourceArgs;
        }
    }
}