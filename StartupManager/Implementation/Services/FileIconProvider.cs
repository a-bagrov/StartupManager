using StartupManager.Interfaces;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace StartupManager.Implementation.Services
{
    //http://www.pinvoke.net/default.aspx/shell32.SHGetFileInfo

    internal class FileIconProvider : IFileIconProvider
    {
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

        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        [DllImport("user32.dll")]
        private static extern int DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll", CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("Shell32.dll", BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern int SHGetFileInfo(string pszPath, int dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, SHGFI_Flag uFlags);

        [DllImport("Shell32.dll")]
        private static extern int SHGetFileInfo(IntPtr pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, SHGFI_Flag uFlags);

        private enum SHGFI_Flag : uint
        {
            SHGFI_ATTR_SPECIFIED = 0x000020000,
            SHGFI_OPENICON = 0x000000002,
            SHGFI_USEFILEATTRIBUTES = 0x000000010,
            SHGFI_ADDOVERLAYS = 0x000000020,
            SHGFI_DISPLAYNAME = 0x000000200,
            SHGFI_EXETYPE = 0x000002000,
            SHGFI_ICON = 0x000000100,
            SHGFI_ICONLOCATION = 0x000001000,
            SHGFI_LARGEICON = 0x000000000,
            SHGFI_SMALLICON = 0x000000001,
            SHGFI_SHELLICONSIZE = 0x000000004,
            SHGFI_LINKOVERLAY = 0x000008000,
            SHGFI_SYSICONINDEX = 0x000004000,
            SHGFI_TYPENAME = 0x000000400
        }
    }
}
