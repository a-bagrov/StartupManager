# StartupManager

Simple startup manager that allows you to see list of programs running every boot time. Programs are collecting from registry, task scheduler, and startup folder. Also you can see digital signature of startup item and its correctness.

This program has no dependencies, that cant be found is standard VS package, so only COM libs and pinvoking were used.

![program demonstation](https://i.ibb.co/Qp8RWBG/image.jpg)

## Some samples of code
### Get digital signature
In order to get digital signature of executable, *X509Certificate.CreateFromSignedFile* can be used.
```csharp
try
{
    using var cert = X509Certificate.CreateFromSignedFile(exePath);
    //digital signature exists, but it can be self signed
}
catch(CryptographicException)
{
    //no digital signature
}
```
### Validate digital signature
In order to verify digital signature you can [pinvoke wintrust.dll](http://www.pinvoke.net/default.aspx/wintrust.winverifytrust).
So public method encapsulating all pinvoke actions can look like this: 
```csharp
public static WinVerifyResult VerifyEmbeddedSignature(string path)
{
    WinTrustFileInfo winTrustFileInfo = null;
    WinTrustData winTrustData = null;
    try
    {
        // specify the WinVerifyTrust function/action that we want
        var action = new Guid(WINTRUST_ACTION_GENERIC_VERIFY_V2);

        // instantiate our WinTrustFileInfo and WinTrustData data structures
        winTrustFileInfo = new WinTrustFileInfo(path);
        winTrustData = new WinTrustData(winTrustFileInfo);

        // call into WinVerifyTrust
        return WinVerifyTrust(INVALID_HANDLE_VALUE, action, winTrustData);
    }
    finally
    {
        // free the locally-held unmanaged memory in the data structures
        winTrustFileInfo?.Dispose();
        winTrustData?.Dispose();
    }
}
```
### Get exe icon
Pinvoke [SHGetFileInfo](http://www.pinvoke.net/default.aspx/shell32.SHGetFileInfo). So public method encapsulating all pinvoke actions can look like this (no need to specify return type as object, it can be *BitmapSource*)
```csharp
public object GetIcon(string path)
{
    var shinfo = new SHFILEINFO();
    var flags = SHGFI_Flag.SHGFI_ICON | SHGFI_Flag.SHGFI_LARGEICON;
    if (SHGetFileInfo(path, 0, ref shinfo, Marshal.SizeOf(shinfo), flags) == 0)
	throw new FileNotFoundException(path);

    var ico = Imaging.CreateBitmapSourceFromHIcon(shinfo.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
    ico?.Freeze();
    
    DestroyIcon(shinfo.hIcon);

    return ico;
}
```
### Registry startup keys
Common keys:
```csharp
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
```
There will be keys with path and arguments in value.
Path and arguments can be presented in many ways:
1. Only path with no quotes, no arguments: C:\Program Files\exe.exe
2. If path has no spaces, it can be supplied with some arguments:
C:\ProgramFiles\exe.exe args
3. Also path can be without exstension, so windows will add ".exe" extensions:
C:\Program Files\exe
4. If path has spaces and arguments, path must be in quotes:
"C:\Program Files\exe.exe" args

Also exe can startup by *[Active Setup](https://attack.mitre.org/techniques/T1547/014/)*, but it designed to run once.

### Get path from shortcut
Folder can hold some shortcuts, so you need to get path to exe. If you forced to use dependencies that only available in VS, you can add reference to *IWshRuntimeLibrary* and use it.
```csharp
public static string GetShortcutTargetFileAndArgs(string shortcutFilename)
{
    IWshRuntimeLibrary.IWshShell wsh = new IWshRuntimeLibrary.WshShellClass();
    IWshRuntimeLibrary.IWshShortcut sc = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(shortcutFilename);
    if (string.IsNullOrEmpty(sc?.TargetPath))
        return string.Empty;

    return $"\"{sc.TargetPath}\" {sc.Arguments}";
}
```
### Working with Windows Task Scheduler
There a lot of nuget packages that can work pretty well, but COM library *TaskScheduler* can be used too.

This method returns exe path and arguments from all tasks that triggering during boot time.
```csharp
private IEnumerable<string> GetExeFileNames(ITaskFolder taskFolder)
{
    //get all task including hidden
    var tasks = taskFolder.GetTasks((int)_TASK_ENUM_FLAGS.TASK_ENUM_HIDDEN);
    foreach (IRegisteredTask task in tasks)
    {
        var def = task?.Definition;
        if (task == null || !task.Enabled || def == null || def.Triggers.Count == 0 || def.Actions.Count == 0)
            continue;

        //going forward if task triggers contains at least one boot time trigger
        if (!def.Triggers.Cast<ITrigger>().Any(c => c.Enabled && c.Type == _TASK_TRIGGER_TYPE2.TASK_TRIGGER_BOOT))
            continue;

        foreach (IAction action in def.Actions)
        {
            if (action.Type == _TASK_ACTION_TYPE.TASK_ACTION_EXEC && action is IExecAction execAction)
                yield return string.Join(" ", execAction.Path, execAction.Arguments);
        }
    }

    var folders = taskFolder.GetFolders(0);//msdn says int argument is experimental and it should be zere

    if (folders.Count == 0)
        yield break;

    foreach (ITaskFolder folder in folders)
    {
        foreach (var fileName in GetExeFileNames(folder))
            yield return fileName;
    }
}
```
### Open explorer and select file
Simple!

```csharp
public void OpenExplorerAt(string filePath)
{
    if (string.IsNullOrEmpty(filePath))
		throw new ArgumentNullException(nameof(filePath));

    Process.Start("explorer.exe", $"/select, \"{filePath}\"");
}
```



