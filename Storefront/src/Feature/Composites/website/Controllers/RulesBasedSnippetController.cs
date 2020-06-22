using Sandbox.Feature.Composites.Repositories;
using Sitecore.XA.Foundation.Mvc.Controllers;

namespace Sandbox.Feature.Composites.Controllers
{
    public class RulesBasedSnippetController : StandardController
    {
        private readonly IRulesBasedSnippetRepository repository;

        public RulesBasedSnippetController(IRulesBasedSnippetRepository repository)
        {
            this.repository = repository;
        }

        protected override object GetModel()
        {
            return repository.GetModel();
        }
    }
}