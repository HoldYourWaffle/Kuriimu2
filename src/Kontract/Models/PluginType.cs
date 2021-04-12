namespace Kontract.Models
{
    /// <summary>
    /// The type of file a plugin can handle.
    /// </summary>
    /// TODO: misleading name, should be changed to something file FileType, but backwards compatibility
    public enum PluginType
    {
        /// <summary>
        /// Defines the type of file as an archive.
        /// </summary>
        Archive,

        /// <summary>
        /// Defines the type of file as images.
        /// </summary>
        Image,

        /// <summary>
        /// Defines the type of file as text.
        /// </summary>
        Text,

        /// <summary>
        /// Defines the type of file as font.
        /// </summary>
        Font,

        /// <summary>
        /// Defines the type of file as raw hex data.
        /// May only be used in internal code.
        /// </summary>
        Hex = int.MaxValue
    }
}
