using StartupManager.Implementation.Services;
using StartupManager.Interfaces;

namespace StartupManager.Models
{
    internal class StartupItem : IStartupItem
    {
        public StartupItem(string name, string path, string args, StartupFileInfo fileInfo, StartupType startupType, object icon)
        {
            Path = path;
            CmdArguments = args;
            Name = name;
            IsSignatureExists = fileInfo.SignatureExists;
            IsSignatureValid = fileInfo.SignatureValid;
            StartupType = startupType;
            Icon = icon;
            CompanyName = fileInfo.CompanyName;
        }

        public object Icon { get; }

        public string Name { get; }

        public string Path { get; }

        public string CmdArguments { get; }

        public StartupType StartupType { get; }

        public bool IsSignatureExists { get; }

        public bool IsSignatureValid { get; }

        public string CompanyName { get; }
    }
}
