namespace StartupManager
{
    public static class Common
    {
        /// <summary>
        /// Возвращает информацию о ярлыке <paramref name="shortcutFilename"/> в формате <i>"путь к обьекту" аргументы</i>.
        /// </summary>
        /// <param name="shortcutFilename">Путь ярлыка.</param>
        /// <returns>Объект, на который указывает ярлык и аргументы.</returns>
        public static string GetShortcutTargetFileAndArgs(string shortcutFilename)
        {
            IWshRuntimeLibrary.IWshShell wsh = new IWshRuntimeLibrary.WshShellClass();
            IWshRuntimeLibrary.IWshShortcut sc = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(shortcutFilename);
            if (string.IsNullOrEmpty(sc?.TargetPath))
                return string.Empty;

            return $"\"{sc.TargetPath}\" {sc.Arguments}";
        }
    }
}
