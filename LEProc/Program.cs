using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using LECommonLibrary;

namespace LEProc
{
    internal static class Program
    {
        internal static string[] Args;

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            SystemHelper.DisableDPIScale();

            if (!GlobalHelper.CheckCoreDLLs())
            {
                MessageBox.Show(
                    "Some of the core Dlls are missing.\r\n" +
                    "Please whitelist these Dlls in your antivirus software, then download and re-install LE.\r\n"
                    +
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
            Args = args;

            var path = SystemHelper.EnsureAbsolutePath(args[0]);
            DoRunWithLEProfile(path, 1, new LEProfile(true));
        }

        private static void DoRunWithLEProfile(string absPath, int argumentsStart, LEProfile profile)
        {
            try
            {
                var applicationName = string.Empty;
                var commandLine = string.Empty;

                if (Path.GetExtension(absPath).ToLower() == ".exe")
                {
                    applicationName = absPath;

                    commandLine = absPath.StartsWith("\"")
                        ? $"{absPath} "
                        : $"\"{absPath}\" ";

                    // use arguments in le.config, prior to command line arguments
                    commandLine += string.IsNullOrEmpty(profile.Parameter) && Args.Skip(argumentsStart).Any()
                        ? Args.Skip(argumentsStart).Aggregate((a, b) => $"{a} {b}")
                        : profile.Parameter;
                }
                else
                {
                    var jb = AssociationReader.GetAssociatedProgram(Path.GetExtension(absPath));

                    if (jb == null)
                        return;

                    applicationName = jb[0];

                    commandLine = jb[0].StartsWith("\"")
                        ? $"{jb[0]} "
                        : $"\"{jb[0]}\" ";
                    commandLine += jb[1].Replace("%1", absPath).Replace("%*", profile.Parameter);
                }

                var currentDirectory = Environment.CurrentDirectory;
                var ansiCodePage = (uint)CultureInfo.GetCultureInfo(profile.Location).TextInfo.ANSICodePage;
                var oemCodePage = (uint)CultureInfo.GetCultureInfo(profile.Location).TextInfo.OEMCodePage;
                var localeID = (uint)CultureInfo.GetCultureInfo(profile.Location).TextInfo.LCID;
                var defaultCharset = (uint)
                    GetCharsetFromANSICodepage(CultureInfo.GetCultureInfo(profile.Location)
                        .TextInfo.ANSICodePage);

                var registries = profile.RedirectRegistry
                    ? new RegistryEntriesLoader().GetRegistryEntries(profile.IsAdvancedRedirection)
                    : null;

                var l = new LoaderWrapper
                {
                    ApplicationName = applicationName,
                    CommandLine = commandLine,
                    CurrentDirectory = currentDirectory,
                    AnsiCodePage = ansiCodePage,
                    OemCodePage = oemCodePage,
                    LocaleID = localeID,
                    DefaultCharset = defaultCharset,
                    HookUILanguageAPI = profile.IsAdvancedRedirection ? (uint)1 : 0,
                    Timezone = profile.Timezone,
                    NumberOfRegistryRedirectionEntries = registries?.Length ?? 0,
                    DebugMode = profile.RunWithSuspend
                };

                registries?.ToList()
                    .ForEach(
                        item =>
                            l.AddRegistryRedirectEntry(item.Root,
                                item.Key,
                                item.Name,
                                item.Type,
                                item.GetValue(CultureInfo.GetCultureInfo(profile.Location))));

                uint ret;
                if ((ret = l.Start()) != 0)
                {
                    GlobalHelper.ShowErrorDebugMessageBox(commandLine, ret);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private static void ElevateProcess()
        {
            try
            {
                SystemHelper.RunWithElevatedProcess(
                    Path.Combine(
                        Path.GetDirectoryName(
                            Assembly.GetExecutingAssembly()
                                .Location),
                        "LEProc.exe"),
                    Args);
            }
            catch (Exception)
            {
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
    }
}
