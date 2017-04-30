// #define ADVANCED_REDIRECTION

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace LEProc
{
    internal static class Program
    {
        static readonly string rootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            if (!(DLLExists("LoaderDll.dll") && DLLExists("LocaleEmulator.dll")))
            {
                MessageBox.Show(
                    "Some of the core Dlls are missing.\r\n" +
                    "\r\n" +
                    "These Dlls are:\r\n" +
                    "LoaderDll.dll\r\n" +
                    "LocaleEmulator.dll",
                    "LEProc",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            if (args.Length == 0)
            {
                MessageBox.Show("Usage: LEProc.exe path [args]", "LEProc");
                Environment.Exit(1);
            }

            var path = EnsureValidPath(args[0]);

            if (path == null)
            {
                MessageBox.Show($"{args[0]}: No such file or directory",
                    "LEProc",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            string commandLine = null;
            if (args.Length == 1)
            {
                commandLine = path;
            }
            else
            {
                args[0] = path;
                commandLine = String.Join(" ", args);
            }

            // var cultureInfo = CultureInfo.GetCultureInfo("ja-JP");
            // var textInfo = cultureInfo.TextInfo;

            var registries = new[]
            {
                new string[] { "HKEY_LOCAL_MACHINE", @"System\CurrentControlSet\Control\Nls\CodePage", "InstallLanguage", "REG_SZ", "1041" },
                new string[] { "HKEY_LOCAL_MACHINE", @"System\CurrentControlSet\Control\Nls\CodePage", "Default", "REG_SZ", "1041" },
                new string[] { "HKEY_LOCAL_MACHINE", @"System\CurrentControlSet\Control\Nls\CodePage", "OEMCP", "REG_SZ", "932" },
                new string[] { "HKEY_LOCAL_MACHINE", @"System\CurrentControlSet\Control\Nls\CodePage", "ACP", "REG_SZ", "932" }
#if ADVANCED_REDIRECTION
                ,
                new string[] { "HKEY_CURRENT_USER", @"Control Panel\International", "Locale", "REG_SZ", "00000411" },
                new string[] { "HKEY_CURRENT_USER", @"Control Panel\International", "LocaleName", "REG_SZ", "ja-JP" },
                new string[] { "HKEY_CURRENT_USER", @"Control Panel\Desktop", "PreferredUILanguages", "REG_MULTI_SZ", "ja-JP" },
                new string[] { "HKEY_CURRENT_USER", @"Control Panel\Desktop\MuiCached", "MachinePreferredUILanguages", "REG_MULTI_SZ", "ja-JP" }
#endif
            };

            // Use default value
            var l = new LoaderWrapper(path, commandLine, Environment.CurrentDirectory)
            {
                // AnsiCodePage = (uint)textInfo.ANSICodePage,
                // OemCodePage = (uint)textInfo.OEMCodePage,
                // LocaleID = (uint)textInfo.LCID,
                // DefaultCharset = 128,  // SHIFT-JIS
                // HookUILanguageAPI = 0,
                // Timezone = "Tokyo Standard Time",
                NumberOfRegistryRedirectionEntries = registries.Length,
                DebugMode = false
            };

            foreach (var item in registries)
            {
                l.AddRegistryRedirectEntry(
                    item[0],
                    item[1],
                    item[2],
                    item[3],
                    item[4]);
            }

            uint ret = l.Start();
            if (ret != 0)
            {
                MessageBox.Show(
                    $"Error Code: {Convert.ToString(ret, 16).ToUpper()}\r\n" +
                    $"Command: {commandLine}",
                    "LEProc");
                Environment.Exit((int)ret);
            }
        }

        private static bool DLLExists(string dllName)
        {
            return File.Exists(Path.Combine(rootDirectory, dllName));
        }

        private static string EnsureValidPath(string path)
        {
            if (!String.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase))
            {
                path += ".exe";
            }

            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }

            var envPath = Environment.GetEnvironmentVariable("PATH");
            foreach (var envPathEntry in envPath.Split(';'))
            {
                var fullPath = Path.Combine(envPathEntry, path);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }
    }
}
