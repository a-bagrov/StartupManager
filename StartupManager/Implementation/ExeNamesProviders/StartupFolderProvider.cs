using StartupManager.Interfaces;
using StartupManager.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace StartupManager.Implementation.ExeNamesProviders
{
    /// <summary>
    /// Представляет доступ к элементам из папки автозапуска.
    /// </summary>
    internal class StartupFolderProvider : IStartupExeValuesProvider
    {
        private const string CommonStartup = @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp";
        private const string UserStartupStartingAfterAppData = @"Microsoft\Windows\Start Menu\Programs\Startup";
        public StartupType StartupType => StartupType.StartMenu;

        public IEnumerable<string> GetValues()
        {
            foreach (var c in GetValues(CommonStartup))
                yield return c;

            foreach (var c in GetValues(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), UserStartupStartingAfterAppData)))
                yield return c;
        }

        private IEnumerable<string> GetValues(string path)
        {
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                if (Path.GetExtension(file).ToLower() != ".lnk") 
                    continue;

                var value = Common.GetShortcutTargetFileAndArgs(file);
                if (string.IsNullOrEmpty(value))
                    continue;

                yield return value;
            }

        }
    }
}
