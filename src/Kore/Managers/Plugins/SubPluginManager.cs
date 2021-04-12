﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.Managers;
using Kontract.Models;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Zio;

namespace Kore.Managers.Plugins
{
    /// <summary>
    /// A nested <see cref="IPluginManager"/> for passing into plugins and controlling their behaviour.
    /// </summary>
    class SubPluginManager : IPluginManager
    {
        private readonly IInternalPluginManager _parentPluginManager;
        private IStateInfo _stateInfo;

        private readonly IList<IStateInfo> _loadedFiles;

        public SubPluginManager(IInternalPluginManager parentPluginManager)
        {
            ContractAssertions.IsNotNull(parentPluginManager, nameof(parentPluginManager));

            _parentPluginManager = parentPluginManager;

            _loadedFiles = new List<IStateInfo>();
        }

        public void RegisterStateInfo(IStateInfo stateInfo)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));

            _stateInfo = stateInfo;
        }

        #region Check

        /// <inheritdoc />
        public bool IsLoading(UPath filePath)
        {
            return _parentPluginManager.IsLoading(filePath);
        }

        /// <inheritdoc />
        public bool IsLoaded(UPath filePath)
        {
            return _parentPluginManager.IsLoaded(filePath);
        }

        /// <inheritdoc />
        public bool IsSaving(IStateInfo stateInfo)
        {
            return _parentPluginManager.IsSaving(stateInfo);
        }

        /// <inheritdoc />
        public bool IsClosing(IStateInfo stateInfo)
        {
            return _parentPluginManager.IsClosing(stateInfo);
        }

        #endregion

        #region Load File

        #region Load FileSystem

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path)
        {
            return LoadFile(fileSystem, path, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId)
        {
            return LoadFile(fileSystem, path, new LoadFileContext { PluginId = pluginId });
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, LoadFileContext loadFileContext)
        {
            ContractAssertions.IsNotNull(_stateInfo, "stateInfo");

            // If the same file is passed to another plugin, take the parent of the current state
            var parent = _stateInfo;
            var statePath = _stateInfo.AbsoluteDirectory / _stateInfo.FilePath.ToRelative();
            if (fileSystem.ConvertPathToInternal(path) == statePath)
                parent = _stateInfo.ParentStateInfo;

            // 1. Load file
            var loadResult = await _parentPluginManager.LoadFile(fileSystem, path, parent, loadFileContext);
            if (!loadResult.IsSuccessful)
                return loadResult;

            // 2. Add file to loaded files
            _loadedFiles.Add(loadResult.LoadedState);

            return loadResult;
        }

        #endregion

        #region Load ArchiveFileInfo

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IStateInfo stateInfo, IArchiveFileInfo afi)
        {
            return _parentPluginManager.LoadFile(stateInfo, afi);
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IStateInfo stateInfo, IArchiveFileInfo afi, Guid pluginId)
        {
            return _parentPluginManager.LoadFile(stateInfo, afi, new LoadFileContext { PluginId = pluginId });
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IStateInfo stateInfo, IArchiveFileInfo afi, LoadFileContext loadFileContext)
        {
            return _parentPluginManager.LoadFile(stateInfo, afi, loadFileContext);
        }

        #endregion

        #region Load Stream

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(StreamFile streamFile)
        {
            return LoadFile(streamFile, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(StreamFile streamFile, Guid pluginId)
        {
            return LoadFile(streamFile, new LoadFileContext { PluginId = pluginId });
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(StreamFile streamFile, LoadFileContext loadFileContext)
        {
            ContractAssertions.IsNotNull(_stateInfo, "stateInfo");

            // 1. Load file
            var loadResult = await _parentPluginManager.LoadFile(streamFile, loadFileContext);
            if (!loadResult.IsSuccessful)
                return loadResult;

            // 2. Add file to loaded files
            _loadedFiles.Add(loadResult.LoadedState);

            return loadResult;
        }

        #endregion

        #endregion

        #region Save File

        public Task<SaveResult> SaveFile(IStateInfo stateInfo)
        {
            return _parentPluginManager.SaveFile(stateInfo);
        }

        public Task<SaveResult> SaveFile(IStateInfo stateInfo, IFileSystem fileSystem, UPath savePath)
        {
            return _parentPluginManager.SaveFile(stateInfo, fileSystem, savePath);
        }

        #endregion

        #region Save Stream

        public Task<SaveStreamResult> SaveStream(IStateInfo stateInfo)
        {
            return _parentPluginManager.SaveStream(stateInfo);
        }

        #endregion

        #region Close file

        public CloseResult Close(IStateInfo stateInfo)
        {
            ContractAssertions.IsElementContained(_loadedFiles, stateInfo, "loadedFiles", nameof(stateInfo));

            var closeResult = _parentPluginManager.Close(stateInfo);
            _loadedFiles.Remove(stateInfo);

            return closeResult;
        }

        public void CloseAll()
        {
            foreach (var loadedFile in _loadedFiles)
                _parentPluginManager.Close(loadedFile);

            _loadedFiles.Clear();
        }

        #endregion
    }
}
