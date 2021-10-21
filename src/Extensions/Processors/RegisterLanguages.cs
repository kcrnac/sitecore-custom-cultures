using Foundation.Sitecore.Extensions.Managers;
using Sitecore.Data.Managers;
using Sitecore.Pipelines;

namespace Foundation.Sitecore.Extensions.Processors
{
    public class RegisterLanguages
    {
        /// <summary>
        /// Registers the custom languages in the <see cref="LanguageManager"/> and clears language caches.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public void Process(PipelineArgs args)
        {
            CustomLanguageManager.RegisterCustomLanguagesAndClearCache();
        }
    }
}