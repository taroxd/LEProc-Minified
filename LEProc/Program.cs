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
                DefaultCharset = (uint)GetCharsetFromANSICodepage(textInfo.ANSICodePage),
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

        private static int GetCharsetFromANSICodepage(int ansicp)
        {
            const int ANSI_CHARSET = 0;
            const int DEFAULT_CHARSET = 1;
            const int SYMBOL_CHARSET = 2;
            const int SHIFTJIS_CHARSET = 128;
            const int HANGEUL_CHARSET = 129;
            const int HANGUL_CHARSET = 129;
            const int GB2312_CHARSET = 134;
            const int CHINESEBIG5_CHARSET = 136;
            const int OEM_CHARSET = 255;
            const int JOHAB_CHARSET = 130;
            const int HEBREW_CHARSET = 177;
            const int ARABIC_CHARSET = 178;
            const int GREEK_CHARSET = 161;
            const int TURKISH_CHARSET = 162;
            const int VIETNAMESE_CHARSET = 163;
            const int THAI_CHARSET = 222;
            const int EASTEUROPE_CHARSET = 238;
            const int RUSSIAN_CHARSET = 204;
            const int MAC_CHARSET = 77;
            const int BALTIC_CHARSET = 186;

            var charset = ANSI_CHARSET;

            switch (ansicp)
            {
                case 932: // Japanese
                    charset = SHIFTJIS_CHARSET;
                    break;
                case 936: // Simplified Chinese
                    charset = GB2312_CHARSET;
                    break;
                case 949: // Korean
                    charset = HANGEUL_CHARSET;
                    break;
                case 950: // Traditional Chinese
                    charset = CHINESEBIG5_CHARSET;
                    break;
                case 1250: // Eastern Europe
                    charset = EASTEUROPE_CHARSET;
                    break;
                case 1251: // Russian
                    charset = RUSSIAN_CHARSET;
                    break;
                case 1252: // Western European Languages
                    charset = ANSI_CHARSET;
                    break;
                case 1253: // Greek
                    charset = GREEK_CHARSET;
                    break;
                case 1254: // Turkish
                    charset = TURKISH_CHARSET;
                    break;
                case 1255: // Hebrew
                    charset = HEBREW_CHARSET;
                    break;
                case 1256: // Arabic
                    charset = ARABIC_CHARSET;
                    break;
                case 1257: // Baltic
                    charset = BALTIC_CHARSET;
                    break;
            }

            return charset;
        }

        public static bool CheckCoreDLLs()
        {
            string[] dlls = { "LoaderDll.dll", "LocaleEmulator.dll" };
            return dlls.All(dll => File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), dll)));
        }

        public static string EnsureValidPath(string filePath)
        {
            if (!String.Equals(Path.GetExtension(filePath), ".exe", StringComparison.OrdinalIgnoreCase))
            {
                filePath += ".exe";
            }

            if (File.Exists(filePath))
            {
                return Path.GetFullPath(filePath);
            }

            var envPath = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in envPath.Split(';'))
            {
                var fullPath = Path.Combine(path, filePath);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }
    }
}
