using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Context;
using Zio;

namespace Kore.Models.UnsupportedPlugin
{
    public class HexState : IHexState, ILoadFiles
    {
        public Stream FileStream { get; private set; }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            FileStream = await fileSystem.OpenFileAsync(filePath);
        }
    }
}
