﻿using System;
using System.Threading.Tasks;
using Kontract.Interfaces.Managers;
using Kontract.Models.Archive;
using Zio;

namespace Kuriimu2.EtoForms.Forms.Interfaces
{
    interface IMainForm
    {
        Task<bool> OpenFile(IStateInfo stateInfo, IArchiveFileInfo file, Guid pluginId);
        Task<bool> SaveFile(IStateInfo stateInfo, bool saveAs);
        Task<bool> CloseFile(IStateInfo stateInfo, IArchiveFileInfo file);
        void RenameFile(IStateInfo stateInfo, IArchiveFileInfo file, UPath newPath);

        void Update(IStateInfo stateInfo, bool updateParents, bool updateChildren);

        void ReportStatus(bool isSuccessful, string message);
    }
}
