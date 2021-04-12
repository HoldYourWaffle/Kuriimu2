using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace Kontract.Interfaces.Plugins.State
{
    /// <summary>
    /// Base interface for all plugin state interfaces.
    /// </summary>
    /// <see cref="PluginType"/>
    public interface IPluginState
    {
        #region Optional feature support checks
        
        public bool CanSave => this is ISaveFiles;
        public bool CanLoad => this is ILoadFiles;

        #endregion
        
        #region Optional feature casting defaults

        bool TryContentChanged => ((ISaveFiles) this).ContentChanged;

        Task TrySave(IFileSystem fileSystem, UPath savePath, SaveContext saveContext) =>
            ((ISaveFiles) this).Save(fileSystem, savePath, saveContext);

        Task TryLoad(IFileSystem fileSystem, UPath filePath, LoadContext loadContext) =>
            ((ILoadFiles) this).Load(fileSystem, filePath, loadContext);

        #endregion
    }
}
