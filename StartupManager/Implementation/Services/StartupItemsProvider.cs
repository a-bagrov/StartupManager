using StartupManager.Interfaces;
using StartupManager.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Windows;

namespace StartupManager.Implementation.Services
{
    internal class StartupItemsProvider : IStartupItemsProvider
    {
        public event ProgressChangedEventHandler ProgressChanged;

        private readonly string _exExtension = ".exe";
        private readonly object _lockObj = new();

        /// <inheritdoc/>
        /// <remarks>
        /// Возможные варианты строки, содержащей путь к существующему исполняемому файлу и аргументы, которая будет корректно обработана:
        /// <list type="number">
        /// <item>Путь без пробелов с указанием расширения: C:\ProgramFiles\4G\4G.exe</item>
        /// <item>Путь без пробелов с указанием расширения с аргументами: C:\ProgramFiles\4G\4G.exe аргумент1 123</item>
        /// <item>Путь без пробелов без указания расширения, если файл 4G.exe существует: C:\ProgramFiles\4G\4G</item>
        /// <item>Путь без пробелов без указания расширения c аргументами, если файл 4G.exe существует: C:\ProgramFiles\4G\4G аргумент1 123</item>
        /// <item>Путь с пробелами без указания расширения в кавычках, если файл 4G.exe существует: "C:\Program Files\4G\4G"</item>
        /// <item>Путь с пробелами без указания расширения в кавычках c аргументами, если файл 4G.exe существует: "C:\Program Files\4G\4G" аргумент1 123</item>
        /// <item>Путь с пробелами с указанием расширения в кавычках: "C:\Program Files\4G\4G.exe"</item>
        /// <item>Путь с пробелами с указанием расширения в кавычках с аргументами: "C:\Program Files\4G\4G.exe" аргумент1 123</item>
        /// </list>
        /// В пути допускаются системные переменные. Любые остальные варианты будут проигнорированы.
        /// </remarks>
        public async Task<IEnumerable<IStartupItem>> GetStartupItemsAsync(IFileIconProvider iconProvider, IFileInfoProvider fileInfoProvider, IEnumerable<IStartupExeValuesProvider> exeNameProviders)
        {
            if (iconProvider == null)
                throw new ArgumentNullException(nameof(iconProvider));

            if (fileInfoProvider == null)
                throw new ArgumentNullException(nameof(fileInfoProvider));

            if (exeNameProviders == null)
                throw new ArgumentNullException(nameof(exeNameProviders));

            var result = new List<IStartupItem>();
            ConcurrentBag<string> errors = new();
            await Task.Run(() =>
            {
                ProgressChanged?.Invoke(this, new(0, null));

                var exeNameProvidersList = exeNameProviders.ToList();
                var progressStep = 100d / exeNameProvidersList.Count;
                var currProgress = 0d;
                
                foreach (var provider in exeNameProvidersList)
                {
                    var values = provider.GetValues();

                    Parallel.ForEach(values, value =>
                    {
                        if(value.IsSuccess == false)
                        {
                            errors.Add(value.Result);
                            return;
                        }
                        
                        IStartupItem si = null;
                        try
                        {
                            si = CreateStartupItem(iconProvider, fileInfoProvider, value.Result, provider.StartupType);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.Message);
                            //System.Diagnostics.Debugger.Break();
                        }
                        finally
                        {
                            lock (_lockObj)
                            {
                                if (si != null)
                                    result.Add(si);
                            }
                        }
                    });

                    currProgress += progressStep;
                    ProgressChanged?.Invoke(this, new((int)currProgress, null));
                }
            });

            if (errors.Count != 0)
            {
                StringBuilder builder = new();
                foreach (var error in errors)
                {
                    builder.AppendLine(error);
                }

                MessageBox.Show("Failed to get information about the following autorun items:", builder.ToString());
            }

            return result;
        }

        private IStartupItem CreateStartupItem(IFileIconProvider iconProvider, IFileInfoProvider fileInfoProvider, string value, StartupType startupType)
        {
            string path, args;
            int pathEndIndex;
            var pathIsCorrect = false;

            value = Environment.ExpandEnvironmentVariables(value);

            if (value.StartsWith("\""))
            {
                //путь с пробелами или без, заключенный в кавычки
                pathEndIndex = value.IndexOf("\"", 1);
                if (pathEndIndex < 0)
                    throw new FileNotFoundException(value);

                path = value.Substring(1, pathEndIndex - 1);
                args = value.Substring(pathEndIndex + 1);
            }
            else if (value.IndexOfAny(Path.GetInvalidPathChars()) != -1 || !string.IsNullOrEmpty(Path.GetExtension(value)) || value.IndexOf(' ') != -1)
            {
                path = string.Empty;
                args = string.Empty;
                //Путь без кавычек, значит либо нет пробелов в пути, либо нет аргументов.
                //1. Предположим, что есть аргументы
                pathEndIndex = value.IndexOf(' ');
                if (pathEndIndex != -1)
                {
                    path = value.Substring(0, pathEndIndex);
                    if (File.Exists(path))
                    {
                        pathIsCorrect = true;
                        args = value.Length > pathEndIndex ? value.Substring(pathEndIndex + 1) : string.Empty;
                    }
                }

                //2. Аргументов нет. Значит вся строка это путь с пробелами.
                if (!pathIsCorrect)
                {
                    path = value;
                    args = string.Empty;
                    pathIsCorrect = File.Exists(path);
                }
            }
            else
            {
                path = value;
                args = string.Empty;
            }

            if (!TryCorrectPathAndGetname(path, pathIsCorrect, out path, out var fileName))
                throw new FileNotFoundException(path);

            GetFileInfoAndIcon(path, iconProvider, fileInfoProvider, out StartupFileInfo fileInfo, out object icon);

            return new StartupItem(fileName, path, args, fileInfo, startupType, icon);
        }

        private bool TryCorrectPathAndGetname(string path, bool pathIsCorrect, out string correctedPath, out string name)
        {
            correctedPath = path;
            name = string.Empty;
            if (!pathIsCorrect && !File.Exists(path) && !TryCorrectPath(path, out correctedPath))
                return false;

            name = Path.GetFileName(correctedPath);
            return true;
        }

        /// <summary>
        /// Совершает попытку скорректировать путь, у которого отсутсвует расширение файла, например "C:\Program Files (x86)\4G Wi-Fi router\4G Wi-Fi router".
        /// </summary>
        /// <param name="path">Путь для корректировки.</param>
        /// <param name="correctPath">Исправленный путь.</param>
        /// <returns><i>True</i>, если удалось исправить путь и найти соответсвующий файл; иначе, <i>false</i>.</returns>
        private bool TryCorrectPath(string path, out string correctPath)
        {
            correctPath = path;
            foreach (var extension in _exExtension)
            {
                correctPath = string.Join("", path, extension);
                if (File.Exists(correctPath))
                    return true;
            }

            return false;
        }

        private void GetFileInfoAndIcon(string path, IFileIconProvider iconProvider, IFileInfoProvider fileInfoProvider,
            out StartupFileInfo fileInfo, out object icon)
        {
            lock (_lockObj)
            {
                icon = iconProvider.GetIcon(path);
            }

            fileInfo = fileInfoProvider.GetFileInfo(path);
        }
    }
}
