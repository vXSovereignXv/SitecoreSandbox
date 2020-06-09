using Sitecore.XA.Foundation.Variants.Abstractions.Models;

namespace Sandbox.Foundation.CommerceCommon.Models
{
    /// <remarks>
    /// Reimplementation of Sitecore.Commerce.XA.Foundation.Common.Models.BaseCommerceRenderingModel to support variants
    /// </remarks>
    public class BaseCommerceVariantsRenderingModel : VariantsRenderingModel
    {
        public string ErrorMessage { get; set; }
    }
}
