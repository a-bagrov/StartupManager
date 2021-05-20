using System;
using System.Diagnostics;
using StartupManager.Interfaces;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
namespace StartupManager.Implementation.Services
{
    internal class FileInfoProvider : IFileInfoProvider
    {
        public StartupFileInfo GetFileInfo(string path)
        {
            try
            {
                //получение сертификата, если его нет - exception
                using var cert = X509Certificate.CreateFromSignedFile(path);
                string companyName;
                var startCnIndex = cert.Subject.IndexOf("CN=", StringComparison.Ordinal);
                if (cert.Subject[startCnIndex + 3] == '\"')
                    companyName = cert.Subject.Substring(startCnIndex + 4, cert.Subject.IndexOf('\"', startCnIndex + 4) - startCnIndex - 4);
                else
                    companyName = cert.Subject.Substring(startCnIndex + 3, cert.Subject.IndexOf(',', startCnIndex + 3) - startCnIndex - 3);

                //проверка подписи
                var winVerify = WinVerify.VerifyEmbeddedSignature(path);

                //подпись существует и корректна
                if (winVerify is WinVerify.WinVerifyResult.Success)
                    return new StartupFileInfo(companyName, true, true);

                //подпись не существует
                if (winVerify is WinVerify.WinVerifyResult.ActionUnknown or WinVerify.WinVerifyResult.FileNotSigned or WinVerify.WinVerifyResult.SignatureOrFileCorrupt)
                    return new StartupFileInfo(companyName, false, false);

                //подпись существует, но не корректна
                return new StartupFileInfo(companyName, true, false);
            }
            catch
            {
                //вероятно, подписи нет, компания берется из ресурсов
                var info = FileVersionInfo.GetVersionInfo(path);
                return new StartupFileInfo(info.CompanyName, false, false);
            }
        }

        #region Проверка подписи - http://www.pinvoke.net/default.aspx/wintrust.winverifytrust
        private class WinVerify
        {
            #region WinTrustData struct field enums

            private enum WinTrustDataUIChoice : uint
            {
                All = 1,
                None = 2,
                NoBad = 3,
                NoGood = 4
            }

            private enum WinTrustDataRevocationChecks : uint
            {
                None = 0x00000000,
                WholeChain = 0x00000001
            }

            private enum WinTrustDataChoice : uint
            {
                File = 1,
                Catalog = 2,
                Blob = 3,
                Signer = 4,
                Certificate = 5
            }

            private enum WinTrustDataStateAction : uint
            {
                Ignore = 0x00000000,
                Verify = 0x00000001,
                Close = 0x00000002,
                AutoCache = 0x00000003,
                AutoCacheFlush = 0x00000004
            }

            [Flags]
            private enum WinTrustDataProvFlags : uint
            {
                UseIe4TrustFlag = 0x00000001,
                NoIe4ChainFlag = 0x00000002,
                NoPolicyUsageFlag = 0x00000004,
                RevocationCheckNone = 0x00000010,
                RevocationCheckEndCert = 0x00000020,
                RevocationCheckChain = 0x00000040,
                RevocationCheckChainExcludeRoot = 0x00000080,
                SaferFlag = 0x00000100,        // Used by software restriction policies. Should not be used.
                HashOnlyFlag = 0x00000200,
                UseDefaultOsverCheck = 0x00000400,
                LifetimeSigningFlag = 0x00000800,
                CacheOnlyUrlRetrieval = 0x00001000,      // affects CRL retrieval and AIA retrieval
                DisableMD2andMD4 = 0x00002000      // Win7 SP1+: Disallows use of MD2 or MD4 in the chain except for the root
            }

            private enum WinTrustDataUIContext : uint
            {
                Execute = 0,
                Install = 1
            }
            #endregion

            #region WinTrust structures
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private class WinTrustFileInfo
            {
                private readonly UInt32 StructSize = (UInt32)Marshal.SizeOf(typeof(WinTrustFileInfo));
                private IntPtr pszFilePath;                     // required, file name to be verified
                private readonly IntPtr hFile = IntPtr.Zero;             // optional, open handle to FilePath
                private readonly IntPtr pgKnownSubject = IntPtr.Zero;    // optional, subject type if it is known

                public WinTrustFileInfo(String _filePath)
                {
                    pszFilePath = Marshal.StringToCoTaskMemAuto(_filePath);
                }
                public void Dispose()
                {
                    if (pszFilePath != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(pszFilePath);
                        pszFilePath = IntPtr.Zero;
                    }
                }
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private class WinTrustData
            {
                private readonly UInt32 StructSize = (UInt32)Marshal.SizeOf(typeof(WinTrustData));
                private readonly IntPtr PolicyCallbackData = IntPtr.Zero;
                private readonly IntPtr SIPClientData = IntPtr.Zero;

                // required: UI choice
                private readonly WinTrustDataUIChoice UIChoice = WinTrustDataUIChoice.None;

                // required: certificate revocation check options
                private readonly WinTrustDataRevocationChecks RevocationChecks = WinTrustDataRevocationChecks.None;

                // required: which structure is being passed in?
                private readonly WinTrustDataChoice UnionChoice = WinTrustDataChoice.File;
                // individual file
                private IntPtr FileInfoPtr;
                private readonly WinTrustDataStateAction StateAction = WinTrustDataStateAction.Ignore;
                private readonly IntPtr StateData = IntPtr.Zero;
                private readonly String URLReference = null;
                private readonly WinTrustDataProvFlags ProvFlags = WinTrustDataProvFlags.RevocationCheckChainExcludeRoot;
                private readonly WinTrustDataUIContext UIContext = WinTrustDataUIContext.Execute;

                // constructor for silent WinTrustDataChoice.File check
                public WinTrustData(WinTrustFileInfo _fileInfo)
                {
                    // On Win7SP1+, don't allow MD2 or MD4 signatures
                    if ((Environment.OSVersion.Version.Major > 6) ||
                        ((Environment.OSVersion.Version.Major == 6) && (Environment.OSVersion.Version.Minor > 1)) ||
                        ((Environment.OSVersion.Version.Major == 6) && (Environment.OSVersion.Version.Minor == 1) && !String.IsNullOrEmpty(Environment.OSVersion.ServicePack)))
                    {
                        ProvFlags |= WinTrustDataProvFlags.DisableMD2andMD4;
                    }

                    WinTrustFileInfo wtfiData = _fileInfo;
                    FileInfoPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WinTrustFileInfo)));
                    Marshal.StructureToPtr(wtfiData, FileInfoPtr, false);
                }
                public void Dispose()
                {
                    if (FileInfoPtr != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(FileInfoPtr);
                        FileInfoPtr = IntPtr.Zero;
                    }
                }
            }
            #endregion

            #region DllImport And WinVerifyTrustResult

            public enum WinVerifyResult : uint
            {
                Success = 0,
                ProviderUnknown = 0x800b0001,           // Trust provider is not recognized on this system
                ActionUnknown = 0x800b0002,         // Trust provider does not support the specified action
                SubjectFormUnknown = 0x800b0003,        // Trust provider does not support the form specified for the subject
                SubjectNotTrusted = 0x800b0004,         // Subject failed the specified verification action
                FileNotSigned = 0x800B0100,         // TRUST_E_NOSIGNATURE - File was not signed
                SubjectExplicitlyDistrusted = 0x800B0111,   // Signer's certificate is in the Untrusted Publishers store
                SignatureOrFileCorrupt = 0x80096010,    // TRUST_E_BAD_DIGEST - file was probably corrupt
                SubjectCertExpired = 0x800B0101,        // CERT_E_EXPIRED - Signer's certificate was expired
                SubjectCertificateRevoked = 0x800B010C,     // CERT_E_REVOKED Subject's certificate was revoked
                UntrustedRoot = 0x800B0109          // CERT_E_UNTRUSTEDROOT - A certification chain processed correctly but terminated in a root certificate that is not trusted by the trust provider.
            }

            private static readonly IntPtr INVALID_HANDLE_VALUE = new(-1);
            // GUID of the action to perform
            private const string WINTRUST_ACTION_GENERIC_VERIFY_V2 = "{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}";

            [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
            private static extern WinVerifyResult WinVerifyTrust(
                [In] IntPtr hwnd,
                [In][MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID,
                [In] WinTrustData pWVTData
            );

            #endregion

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

        }
        #endregion
    }

    internal readonly struct StartupFileInfo
    {
        public StartupFileInfo(string manufacter, bool signatureExists, bool signatureValid)
        {
            CompanyName = manufacter;
            SignatureExists = signatureExists;
            SignatureValid = signatureValid;
        }

        public string CompanyName { get; }
        public bool SignatureExists { get; }
        public bool SignatureValid { get; }
    }
}
