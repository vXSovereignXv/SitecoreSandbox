using Sitecore.Data;
using Sitecore.Rules;
using Sitecore.Rules.Actions;

namespace Sandbox.Feature.Composites.Rules
{
    public class SelectSnippet<T> : RuleAction<T> where T : RuleContext
    {
        public string SnippetId { get; set; }

        public override void Apply(T ruleContext)
        {
            SelectSnippetRuleContext dataSourceRuleContext = ruleContext as SelectSnippetRuleContext;
            ID result;
            if (dataSourceRuleContext == null || !ID.TryParse(SnippetId, out result))
            {
                return;
            }
            dataSourceRuleContext.SnippetId = result;
        }
    }
}