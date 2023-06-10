using System.Collections.Generic;
using StartupManager.Interfaces;
using StartupManager.Models;
using Microsoft.Win32;
namespace StartupManager.Implementation.ExeNamesProviders
{
    /// <summary>
    /// Представляет доступ к элементам из реестра.
    /// </summary>
    internal class RegistryStartupProvider : IStartupExeValuesProvider
    {
        public StartupType StartupType => StartupType.Registry;

        private readonly string[] _localMachineSubKeys = new[]
        {
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
            @"Software\Microsoft\Windows\CurrentVersion\RunServices",
            @"Software\Microsoft\Windows\CurrentVersion\RunServicesOnce",
            @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon\Userinit",
        };

        private readonly string[] _currUserSubKeys = new[]
        {
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
            @"Software\Microsoft\Windows\CurrentVersion\RunServices",
            @"Software\Microsoft\Windows\CurrentVersion\RunServicesOnce",
            @"Software\Microsoft\Windows NT\CurrentVersion\Windows",

        };

        public IEnumerable<StartupResult> GetValues()
        {
            foreach (var c in GetFileNames(Registry.LocalMachine, _localMachineSubKeys))
                yield return new(c, true);

            foreach (var c in GetFileNames(Registry.CurrentUser, _currUserSubKeys))
                yield return new(c, true);
        }

        private IEnumerable<string> GetFileNames(RegistryKey entryKey, string[] paths)
        {
            foreach (var key in paths)
            {
                using var r = entryKey.OpenSubKey(key);
                if (r == null)
                    continue;

                foreach (var name in r.GetValueNames())
                {
                    var value = r.GetValue(name, string.Empty);
                    if (value is not string stringValue)
                        continue;

                    if (string.IsNullOrEmpty(stringValue))
                        continue;

                    yield return stringValue;
                }
            }
        }
    }
}
