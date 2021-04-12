using Eto.Drawing;
using Kontract.Models.Archive;
using Kuriimu2.EtoForms.Support;
using Zio;

namespace Kuriimu2.EtoForms.Forms.Models
{
    public class FileElement
    {
        public IArchiveFileInfo ArchiveFileInfo { get; }

        public string Name => ArchiveFileInfo.FilePath.GetName();

        public long Size => ArchiveFileInfo.FileSize;

        public Color Color { get; }

        public FileElement(IArchiveFileInfo afi):this(afi,KnownColors.Black)
        {
        }

        public FileElement(IArchiveFileInfo afi, Color color)
        {
            ArchiveFileInfo = afi;
            Color = color;
        }
    }
}
