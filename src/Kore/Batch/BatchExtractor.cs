﻿using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem.EventArgs;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kore.Managers.Plugins;
using Serilog;
using Zio;

namespace Kore.Batch
{
    public class BatchExtractor : BaseBatchProcessor
    {
        private readonly object _lock = new object();
        private readonly IList<UPath> _openedFiles = new List<UPath>();

        public BatchExtractor(IInternalPluginManager pluginManager, ILogger logger) :
            base(pluginManager, logger)
        {
        }

        protected override async Task ProcessInternal(IFileSystem sourceFileSystem, UPath filePath, IFileSystem destinationFileSystem)
        {
            Logger.Information("Extract '{0}'.", filePath.FullName);

            IStateInfo loadedState;
            lock (_lock)
            {
                _openedFiles.Clear();

                // Load file
                SourceFileSystemWatcher.Opened += SourceFileSystemWatcher_Opened;
                loadedState = LoadFile(sourceFileSystem, filePath).Result;
                SourceFileSystemWatcher.Opened -= SourceFileSystemWatcher_Opened;

                // If file could not be loaded successfully
                if (loadedState == null)
                    return;

                // If one of the opened files was already batched, stop execution
                if (_openedFiles.Any(IsFileBatched))
                {
                    PluginManager.Close(loadedState);

                    Logger.Information("'{0}' is/was already processed.", filePath.FullName);
                    return;
                }

                // Add opened files to batched files
                foreach (var openedFile in _openedFiles)
                    AddBatchedFile(openedFile);
            }

            switch (loadedState.PluginState)
            {
                case IArchiveState archiveState:
                    await ExtractArchive(archiveState, destinationFileSystem, filePath);
                    break;

                case IImageState imageState:
                    ExtractImage(imageState, destinationFileSystem, filePath);
                    break;

                default:
                    Logger.Error("'{0}' is not supported.", filePath.FullName);
                    PluginManager.Close(loadedState);
                    return;
            }

            PluginManager.Close(loadedState);

            Logger.Information("Extracted '{0}'.", filePath.FullName);
        }

        private void SourceFileSystemWatcher_Opened(object sender, FileOpenedEventArgs e)
        {
            _openedFiles.Add(e.OpenedPath);
        }

        private async Task ExtractArchive(IArchiveState archiveState, IFileSystem destinationFileSystem, UPath filePath)
        {
            if (archiveState.Files.Count > 0)
                CreateDirectory(destinationFileSystem, filePath);

            foreach (var afi in archiveState.Files)
            {
                var newFileStream = destinationFileSystem.OpenFile(filePath / afi.FilePath.ToRelative(), FileMode.Create, FileAccess.Write);
                (await afi.GetFileData()).CopyTo(newFileStream);

                newFileStream.Close();
            }
        }

        private void ExtractImage(IImageState imageState, IFileSystem destinationFileSystem, UPath filePath)
        {
            if (imageState.Images.Count > 0)
                CreateDirectory(destinationFileSystem, filePath);

            var index = 0;
            foreach (var img in imageState.Images)
            {
                var fileStream = destinationFileSystem.OpenFile(filePath / (img.Name ?? $"{index:00}") + ".png", FileMode.Create, FileAccess.Write);
                img.GetImage().Save(fileStream, ImageFormat.Png);

                fileStream.Close();

                index++;
            }
        }

        private void CreateDirectory(IFileSystem fileSystem, UPath path)
        {
            if (!fileSystem.DirectoryExists(path))
                fileSystem.CreateDirectory(path);
        }
    }
}
