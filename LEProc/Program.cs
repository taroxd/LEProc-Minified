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
    
        const string LOCATION = "ja-JP";
        const string TIMEZONE = "Tokyo Standard Time";

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {

            if (!CheckCoreDLLs())
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

                return;
            }

            if (args.Length == 0)
            {
                MessageBox.Show("Usage: LEProc.exe path [args]", "LEProc");
                return;
            }

            var path = EnsureValidPath(args[0]);

            if (path == null)
            {
                MessageBox.Show($"{args[0]}: No such file or directory",
                    "LEProc",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                    );
                return;
            }

            var commandLine = path;

            if (args.Length > 1)
            {
                commandLine += " " + String.Join(" ", args.Skip(1));
            }

            var cultureInfo = CultureInfo.GetCultureInfo(LOCATION);
            var textInfo = cultureInfo.TextInfo;

            var registries = new RegistryEntriesLoader().GetRegistryEntries(false);

            var l = new LoaderWrapper
            {
                ApplicationName = path,
                CommandLine = commandLine,
                CurrentDirectory = Environment.CurrentDirectory,
                AnsiCodePage = (uint)textInfo.ANSICodePage,
                OemCodePage = (uint)textInfo.OEMCodePage,
                LocaleID = (uint)textInfo.LCID,
                DefaultCharset = 128,
                HookUILanguageAPI = 0,
                Timezone = TIMEZONE,
                NumberOfRegistryRedirectionEntries = registries?.Length ?? 0,
                DebugMode = false
            };

            registries?.ToList()
                .ForEach(
                    item =>
                        l.AddRegistryRedirectEntry(item.Root,
                            item.Key,
                            item.Name,
                            item.Type,
                            item.GetValue(cultureInfo)));

            var ret = l.Start();
            if (ret != 0)
            {
                MessageBox.Show(
                    $"Error Code: {Convert.ToString(ret, 16).ToUpper()}\r\n"
                    + $"Command: {commandLine}",
                    "LEProc");
            }
        }

        public static bool CheckCoreDLLs()
        {
            string[] dlls = { "LoaderDll.dll", "LocaleEmulator.dll" };
            return dlls.All(dll => File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), dll)));
        }

        public static string EnsureValidPath(string path)
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
