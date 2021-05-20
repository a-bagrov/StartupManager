using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace StartupManager.Interfaces
{
    internal interface IStartupItemsProvider
    {
        /// <summary>
        /// Возвращает существующие исполняемые файлы в формате <see cref="IStartupItem"/> из <paramref name="exeNameProviders"/>.
        /// </summary>
        /// <param name="iconProvider">Сервис для получения иконки исполняемого файла.</param>
        /// <param name="fileInfoProvider">Сервис для получения <see cref="Implementation.Services.StartupFileInfo"/>.</param>
        /// <param name="exeNameProviders">Сервисы для получения строк, содержащих путь к исполняемому файлу и аргументы.</param>
        /// <returns>Перечисление <see cref="IStartupItem"/>.</returns>
        Task<IEnumerable<IStartupItem>> GetStartupItemsAsync(IFileIconProvider iconProvider, IFileInfoProvider fileInfoProvider, IEnumerable<IStartupExeValuesProvider> exeNameProviders);

        event ProgressChangedEventHandler ProgressChanged;
    }
}
