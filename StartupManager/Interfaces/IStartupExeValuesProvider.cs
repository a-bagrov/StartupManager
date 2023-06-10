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
        public IEnumerable<StartupResult> GetValues();
    }
    
    internal readonly struct StartupResult
    {
        public StartupResult(string result, bool isSuccess) : this()
        {
            Result = result;
            IsSuccess = isSuccess;
        }

        public string Result { get; }
        public bool IsSuccess { get; }

        public static bool operator ==(StartupResult a, StartupResult b) => a.Result == b.Result && a.IsSuccess == b.IsSuccess;
        public static bool operator !=(StartupResult a, StartupResult b) => !(a == b);
    }
}
