using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data;

namespace Sandbox.Feature.Composites
{
  public struct Templates
  {
        public struct RulesBasedSnippetSnippet
        {
            public static ID ID = ID.Parse("{018D3494-C787-4474-8FED-7A786310346A}");

            public struct Fields
            {
                public static ID SnippetRules = ID.Parse("{B0C87169-75D8-4F6E-A14D-F71B1049848C}");
            }
        }
    }
}