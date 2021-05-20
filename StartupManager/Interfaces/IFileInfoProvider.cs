using StartupManager.Implementation.Services;

namespace StartupManager.Interfaces
{
    internal interface IFileInfoProvider
    {
        StartupFileInfo GetFileInfo(string path);
    }
}
