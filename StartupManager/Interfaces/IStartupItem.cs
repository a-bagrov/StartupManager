using StartupManager.Models;

namespace StartupManager.Interfaces
{
    internal interface IStartupItem
    {
        object Icon { get; }
        string Name { get; }
        string Path { get; }
        string CmdArguments { get; }
        StartupType StartupType { get; }
        bool IsSignatureExists { get; }
        bool IsSignatureValid { get; }
        string CompanyName { get; }
    }
}
