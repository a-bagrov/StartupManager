using StartupManager.Models;
using System.Collections.Generic;

namespace StartupManager.Interfaces
{
    internal interface IStartupExeValuesProvider
    {
        /// <summary>
        /// Тип источника элементов <see cref="IStartupItem"/>.
        /// </summary>
        public StartupType StartupType { get; }
        /// <summary>
        /// Возвращает перечисление элементов <see cref="IStartupItem"/>.
        /// </summary>
        public IEnumerable<string> GetValues();
    }
}
