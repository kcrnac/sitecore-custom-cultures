using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Foundation.Sitecore.Extensions.Model;
using Sitecore.Abstractions;
using Sitecore.Caching;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.DataProviders.Sql;
using Sitecore.Data.Managers;
using Sitecore.DependencyInjection;
using Sitecore.Globalization;

namespace Foundation.Sitecore.Extensions.Managers
{
    public static class CustomLanguageManager
    {
        private const string LanguageCacheName = "LanguageProvider - Languages";

        private static readonly MethodInfo LanguageProviderMarkLanguageAsRegisteredMethod;
        private static readonly FieldInfo CultureDataDisplayNameField;
        private static readonly FieldInfo CultureDataEnglishNameField;
        private static readonly FieldInfo CultureDataNativeNameField;

        static CustomLanguageManager()
        {
            // It is used to get its type that would be inaccessible otherwise due to it being internal.
            var dummyCultureData = GetDummyCultureData();

            LanguageProviderMarkLanguageAsRegisteredMethod = typeof(LanguageProvider).GetMethod("MarkLanguageAsRegistered", BindingFlags.Instance | BindingFlags.NonPublic);
            CultureDataDisplayNameField = dummyCultureData.GetType().GetField("sLocalizedDisplayName", BindingFlags.Instance | BindingFlags.NonPublic);
            CultureDataEnglishNameField = dummyCultureData.GetType().GetField("sEnglishDisplayName", BindingFlags.Instance | BindingFlags.NonPublic);
            CultureDataNativeNameField = dummyCultureData.GetType().GetField("sNativeDisplayName", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Registers the custom languages and clears the related caches.
        /// </summary>
        public static void RegisterCustomLanguagesAndClearCache()
        {
            RegisterCustomLanguagesFromDefinitions();
            ClearDatabaseProviderLanguageCache();
            ClearLanguageCache();
            BuildAndInitializeCultureInfoCache();
        }

        /// <summary>
        /// Returns dummy CultureData instance
        /// </summary>
        /// <returns>Culture data object</returns>
        private static object GetDummyCultureData()
        {
            var cultureInfo = new CultureInfo("du-my");

            return cultureInfo.GetType().GetField("m_cultureData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cultureInfo);

        }

        /// <summary>
        /// Clears the Sitecore language cache.
        /// </summary>
        private static void ClearLanguageCache()
        {
            var languageCache = CacheManager.FindCacheByName<string>(LanguageCacheName);
            languageCache.Clear();
        }

        /// <summary>
        /// Registers the custom languages from LanguageDefinitions.config in the <see cref="LanguageManager"/>.
        /// </summary>
        private static void RegisterCustomLanguagesFromDefinitions()
        {
            var languageProvider = GetLanguageProvider();

            foreach (var languageItem in LanguageDefinitions.Definitions)
            {
                if (!LanguageManager.LanguageRegistered(languageItem.Name))
                {
                    LanguageProviderMarkLanguageAsRegisteredMethod.Invoke(languageProvider, new[] { languageItem.Name });
                }
            }
        }

        /// <summary>
        /// Gets the current <see cref="LanguageProvider"/> instance.
        /// </summary>
        /// <returns>The current <see cref="LanguageProvider"/> instance.</returns>
        private static LanguageProvider GetLanguageProvider()
        {
            var languageManagerInstance = ((LazyResetable<BaseLanguageManager>)typeof(LanguageManager).GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(typeof(LanguageManager))).Value;
            return (LanguageProvider)languageManagerInstance.GetType().GetField("provider", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(languageManagerInstance);
        }

        /// <summary>
        /// Clears the internal language cache of the database provider.
        /// </summary>
        /// <remarks>
        /// It's necessary to clear the cache of the database provider so it gets refreshed and picks up the new languages.
        /// Setting the cache variable to <c>null</c> is how the cache is cleared internally in <see cref="SqlDataProvider"/>,
        /// hence it's safe to do so.
        /// </remarks>
        private static void ClearDatabaseProviderLanguageCache()
        {
            foreach (var database in Factory.GetDatabases())
            {
                var databaseProviders = (DataProviderCollection)database.GetType().GetProperty("DataProviders", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(database);
                foreach (var databaseProvider in databaseProviders.Where(x => x is SqlDataProvider))
                {
                    databaseProvider.GetType().GetProperty("Languages", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(databaseProvider, null);
                }
            }
        }

        /// <summary>
        /// Rebuilds the internal cache of the <see cref="CultureData"/> class and sets the correct culture names.
        /// </summary>
        /// <seealso cref="InitializeCustomCulture"/>
        private static void BuildAndInitializeCultureInfoCache()
        {
            Database database;
            try
            {
                var role = ConfigurationManager.AppSettings["role:define"];
                database = Factory.GetDatabase(role == "ContentDelivery" ? GetCdDatabaseName() : "master");
            }
            catch (Exception ex)
            {
                // Log here
                database = Factory.GetDatabase(GetCdDatabaseName());
            }

            var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            var customCultureNames = LanguageManager.GetLanguages(database)
                .Where(language => allCultures.All(culture => !culture.Name.Equals(language.Name, StringComparison.InvariantCultureIgnoreCase)))
                .Select(language => language.Name.ToLower())
                .ToList();

            foreach (var cultureName in customCultureNames)
            {
                InitializeCustomCulture(cultureName);
            }
        }

        /// <summary>
        /// Returns correct database name if db name is not same across CDs
        /// </summary>
        /// <returns></returns>
        private static string GetCdDatabaseName()
        {
            return string.IsNullOrEmpty(ConfigurationManager.ConnectionStrings["web2"]?.ConnectionString) ? "web" : "web2";
        }

        /// <summary>
        /// Initializes the custom culture.
        /// </summary>
        /// <remarks>
        /// This is needed because normally the unregistered cultures have "Unknown locale" as display name.
        /// This method modifies the internal cache of the <see cref="CultureData"/> class, so the next time
        /// these names are requested, our custom values are returned.
        /// </remarks>
        /// <param name="cultureName">Name of the culture.</param>
        private static void InitializeCustomCulture(string cultureName)
        {
            var cultureInfo = CultureInfo.GetCultureInfo(cultureName);
            var cultureData = cultureInfo.GetType().GetField("m_cultureData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cultureInfo);

            var regionNames = GetRegionNames(cultureName.Split('-')[1]);

            var displayName = $"{cultureInfo.Parent.DisplayName} ({regionNames.DisplayName})";
            CultureDataDisplayNameField.SetValue(cultureData, displayName);

            var englishName = $"{cultureInfo.Parent.EnglishName} ({regionNames.EnglishName})";
            CultureDataEnglishNameField.SetValue(cultureData, englishName);
            CultureDataNativeNameField.SetValue(cultureData, englishName);
        }

        /// <summary>
        /// Gets the English, native and display names of the <paramref name="region"/>.
        /// Uses custom regions first then <see cref="RegionInfo"/> with a fallback to using <paramref name="region"/> as is.
        /// </summary>
        /// <param name="region">The region.</param>
        /// <returns>The English, native and display names of the <paramref name="region"/>.</returns>
        private static RegionNames GetRegionNames(string region)
        {
            RegionNames regionNames;

            try
            {
                var regionInfo = new RegionInfo(region);
                regionNames = new RegionNames(regionInfo.EnglishName, regionInfo.NativeName, regionInfo.DisplayName);
            }
            catch (ArgumentException)
            {
                regionNames = new RegionNames(region);
            }

            return regionNames;
        }
    }
}