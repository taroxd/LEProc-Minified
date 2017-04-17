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
                    MessageBoxIcon.Error
                    );
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

            var cultureInfo = CultureInfo.GetCultureInfo("ja-JP");
            var textInfo = cultureInfo.TextInfo;

            var registries = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => type.Namespace.StartsWith("LEProc.RegistryEntries"))
                .Select(type => (IRegistryEntry)Activator.CreateInstance(type))
                .Where(entry => !entry.IsAdvanced)
                .ToArray();

            var l = new LoaderWrapper
            {
                ApplicationName = path,
                CommandLine = commandLine,
                CurrentDirectory = Environment.CurrentDirectory,
                AnsiCodePage = (uint)textInfo.ANSICodePage,
                OemCodePage = (uint)textInfo.OEMCodePage,
                LocaleID = (uint)textInfo.LCID,
                DefaultCharset = 128,  // SHIFT-JIS
                HookUILanguageAPI = 0,
                Timezone = "Tokyo Standard Time",
                NumberOfRegistryRedirectionEntries = registries.Length,
                DebugMode = false
            };

            foreach (var item in registries)
            {
                l.AddRegistryRedirectEntry(
                    item.Root,
                    item.Key,
                    item.Name,
                    item.Type,
                    item.GetValue(cultureInfo));
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

        static bool CheckCoreDLLs()
        {
            string[] dlls = { "LoaderDll.dll", "LocaleEmulator.dll" };
            var dirName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return dlls.All(dll => File.Exists(Path.Combine(dirName, dll)));
        }

        static string EnsureValidPath(string path)
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
