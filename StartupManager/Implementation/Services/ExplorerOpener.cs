using StartupManager.Interfaces;
using System;
using System.Diagnostics;
using System.IO;

namespace StartupManager.Implementation.Services
{
    internal class ExplorerOpener : IExplorerOpener
    {
        public void OpenExplorerAt(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            Process.Start("explorer.exe", $"/select, \"{filePath}\"");
        }
    }
}
