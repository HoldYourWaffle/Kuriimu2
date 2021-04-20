﻿using System.Threading.Tasks;
using Kontract.Interfaces.Managers;
using Kontract.Models;
using Kore.Models;
using Zio;

namespace Kore.Managers.Plugins.FileManagement
{
    /// <summary>
    /// Exposes methods to save files from a state.
    /// </summary>
    interface IFileSaver
    {
        /// <summary>
        /// Saves a state of a loaded file to any relative file in a file system.
        /// </summary>
        /// <param name="stateInfo">The <see cref="IStateInfo"/> to save.</param>
        /// <param name="fileSystem">The file system in which to save the file.</param>
        /// <param name="savePath">The virtual path to where the state should be saved t1o in the file system.</param>
        /// <param name="saveInfo">The context for the save operation.</param>
        Task<SaveResult> SaveAsync(IStateInfo stateInfo, IFileSystem fileSystem, UPath savePath, SaveInfo saveInfo);
    }
}
