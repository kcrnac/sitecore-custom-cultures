namespace Foundation.Sitecore.Extensions.Model
{
    /// <summary>
    /// Stores custom region names.
    /// </summary>
    public class RegionNames
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegionNames"/> class.
        /// </summary>
        /// <param name="englishName">English name of the region.</param>
        /// <param name="nativeName">Native name of the region.</param>
        /// <param name="displayName">Display name of the region.</param>
        public RegionNames(string englishName, string nativeName, string displayName)
        {
            EnglishName = englishName;
            NativeName = nativeName;
            DisplayName = displayName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionNames"/> class.
        /// Sets <see cref="EnglishName"/>, <see cref="NativeName"/> and <see cref="DisplayName"/> to <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The region name.</param>
        public RegionNames(string name)
        {
            EnglishName = name;
            NativeName = name;
            DisplayName = name;
        }

        /// <summary>
        /// Gets or sets the english name of the region.
        /// </summary>
        /// <value>
        /// The english name of the region.
        /// </value>
        public string EnglishName { get; set; }

        /// <summary>
        /// Gets or sets the native name of the region.
        /// </summary>
        /// <value>
        /// The native name of the region.
        /// </value>
        public string NativeName { get; set; }

        /// <summary>
        /// Gets or sets the display name of the region.
        /// </summary>
        /// <value>
        /// The display name of the region.
        /// </value>
        public string DisplayName { get; set; }
    }
}