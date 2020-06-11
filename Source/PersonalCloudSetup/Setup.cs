using Microsoft.Deployment.WindowsInstaller;
using Mono.Options;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using WixSharp;
using WixSharp.Bootstrapper;
using WixSharp.CommonTasks;

namespace PersonalCloudSetup
{
    class Setup
    {
        static int Main(string[] args)
        {
            string savedWorkDir = Environment.CurrentDirectory;

            string dataFolder = null;
            string buildVersion = null;

            var options = new OptionSet
            {
                { "d=|data-folder=",    "Data folder",    o => dataFolder = o },
                { "b=|build-version=",  "Build version",  o => buildVersion = o }
            };

            try
            {
                var extra = options.Parse(args);
                if (extra.Count == 0 && !string.IsNullOrEmpty(dataFolder) && !string.IsNullOrEmpty(buildVersion))
                {
                    var di = new DirectoryInfo(dataFolder);
                    if (!di.Exists)
                    {
                        Console.WriteLine("Data folder not exists!");
                        return -2;
                    }

                    string sAppPath = new Uri(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().
                        GetName().CodeBase)).LocalPath;

                    Environment.SetEnvironmentVariable("WIXSHARP_WIXDIR", Path.Combine(di.FullName, @"wix_bin\tools\bin"));

                    Console.WriteLine($" Wix Folder: {FindWixBinLocation()}");
                    Console.WriteLine($"Data Folder: {di.FullName}");
                    Console.WriteLine($"    Version: {buildVersion}");
                    Environment.CurrentDirectory = sAppPath;
                    {
                        string msiFilename = BuildMSI(Platform.x86, di.FullName, buildVersion);
                        BuildBundle(Platform.x86, di.FullName, msiFilename);
                    }
                    {
                        string msiFilename = BuildMSI(Platform.x64, di.FullName, buildVersion);
                        BuildBundle(Platform.x64, di.FullName, msiFilename);
                    }
                    return 0;
                }
                else
                {
                    Console.WriteLine("PersonalCloudSetup -d <data folder> -b <version number>");
                    return -1;
                }
            }
            catch (OptionException ex)
            {
                Console.WriteLine(ex.Message);
                return -99;
            }
            finally
            {
                Environment.CurrentDirectory = savedWorkDir;
            }
        }

        static string FindWixBinLocation()
        {
            // See if the command line was set for this property
            var msBuildArgument = Environment.GetCommandLineArgs().FirstPrefixedValue("/WIXBIN:");
            if (msBuildArgument.IsNotEmpty() && Directory.Exists(msBuildArgument))
            {
                return Path.GetFullPath(msBuildArgument);
            }

            // Now check to see if the environment variable was set
            var environmentVar = Environment.GetEnvironmentVariable("WIXSHARP_WIXDIR");
            if (environmentVar.IsNotEmpty() && Directory.Exists(environmentVar))
            {
                return Path.GetFullPath(environmentVar);
            }

            // Now check to see if the WIX install set an environment variable
            var wixEnvironmentVariable = Environment.ExpandEnvironmentVariables(@"%WIX%\bin");
            if (wixEnvironmentVariable.IsNotEmpty() && Directory.Exists(wixEnvironmentVariable))
            {
                return Path.GetFullPath(wixEnvironmentVariable);
            }

            // Now try the program files install location
            string wixInstallDir = Directory.GetDirectories(ProgramFilesDirectory, "Windows Installer XML v3*")
                                            .Order()
                                            .LastOrDefault();

            if (wixInstallDir.IsNotEmpty() && Directory.Exists(wixInstallDir))
            {
                return Path.GetFullPath(wixInstallDir.PathJoin("bin"));
            }

            // C:\Program Files (x86)\WiX Toolset v3.11\bin\candle.exe
            // Try a secondary location
            wixInstallDir = Directory.GetDirectories(ProgramFilesDirectory, "WiX Toolset v3*")
                                     .Order()
                                     .LastOrDefault();

            if (wixInstallDir.IsNotEmpty() && Directory.Exists(wixInstallDir))
            {
                return Path.GetFullPath(wixInstallDir.PathJoin("bin"));
            }

            throw new Exception("WiX binaries cannot be found. Wix# is capable of automatically finding WiX tools only if " +
                                "WiX Toolset installed. In all other cases you need to set the environment variable " +
                                "WIXSHARP_WIXDIR or WixSharp.Compiler.WixLocation to the valid path to the WiX binaries.\n" +
                                "WiX binaries can be brought to the build environment by either installing WiX Toolset, " +
                                "downloading Wix# suite or by adding WixSharp.wix.bin NuGet package to your project.");
        }

        /// <summary>
        /// Gets the program files directory.
        /// </summary>
        /// <value>
        /// The program files directory.
        /// </value>
        internal static string ProgramFilesDirectory
        {
            get
            {
                string programFilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                if ("".GetType().Assembly.Location.Contains("Framework64"))
                    programFilesDir += " (x86)"; //for x64 systems
                return programFilesDir;
            }
        }

        private static string BuildMSI(Platform platform, string dataFolder, string versionStr)
        {
            string platformString;
            string platformString2;
            if (platform == Platform.x86)
            {
                platformString = "x86";
                platformString2 = "Intel;";
            }
            else if (platform == Platform.x64)
            {
                platformString = "x64";
                platformString2 = "x64;";
            }
            else
            {
                throw new NotSupportedException("Unsupported Platform");
            }

            string outputFilename = $"PersonalCloud-{versionStr}-{platformString}";

            var project =

                new Project("!(loc.PersonalCloudFolderName)",

                    new Dir(@"%ProgramFiles%\Personal Cloud",

                        new Dir("Service",

                            new WixSharp.Files(Path.Combine(dataFolder, $@"Service\{platformString}\*.*"),
                                f => !f.EndsWith(".obj") &&
                                     !f.EndsWith(".pdb"))
                            {
                                AttributesDefinition = "ReadOnly=no"
                            },

                            new WixSharp.Files(Path.Combine(dataFolder, $@"dokan_bin\{platformString}\*.*"),
                                f => !f.EndsWith(".obj") &&
                                     !f.EndsWith(".pdb"))
                            {
                                AttributesDefinition = "ReadOnly=no"
                            },

                            new WixSharp.Files(Path.Combine(dataFolder, $@"ffmpeg_bin\{platformString}\*.*"),
                                f => !f.EndsWith(".obj") &&
                                     !f.EndsWith(".pdb"))
                            {
                                AttributesDefinition = "ReadOnly=no"
                            }
                        ),

                        new Dir("GUI",

                            new WixSharp.Files(Path.Combine(dataFolder, @"GUI\*.*"),
                                f => !f.EndsWith(".obj") &&
                                     !f.EndsWith(".pdb"))
                            {
                                AttributesDefinition = "ReadOnly=no"
                            }
                        )
                    )
                );

            project.AddProperty(new Property("WIXUI_EXITDIALOGOPTIONALCHECKBOX", "1"));
            project.AddProperty(new Property("WIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT", "!(loc.LaunchConfigurator)"));
            project.AddAction(new ManagedAction(CustomActions.LaunchApplication) { Execute = Execute.immediate });

            project.GUID = new Guid("B8B67678-128E-47D8-BE23-90132BCF220F");
            project.UpgradeCode = new Guid("B8B67678-128E-47D8-BE23-90132BCF1058");

            project.Platform = platform;

            project.ResolveWildCards(ignoreEmptyDirectories: true);

            var personalCloudConfigExeFile = project.AllFiles.Single(f => f.Name.EndsWith("PersonalCloud.WindowsConfigurator.exe"));
            personalCloudConfigExeFile
                .AddShortcut(new FileShortcut("!(loc.PersonalCloudConfiguratorShortcutTitle)", @"%ProgramMenu%\!(loc.PersonalCloudFolderName)"))
                .AddShortcut(new FileShortcut("!(loc.PersonalCloudConfiguratorShortcutTitle)", @"%Desktop%"))
                .AddShortcut(new FileShortcut("!(loc.PersonalCloudConfiguratorShortcutTitle)", @"%Startup%") { Arguments = "/Startup" });

            var personalCloudServiceExeFile = project.AllFiles.Single(f => f.Name.EndsWith("PersonalCloud.WindowsService.exe"));

            personalCloudServiceExeFile.Add(new FirewallException { Name = "Personal Cloud Service", Scope = FirewallExceptionScope.any });

            personalCloudServiceExeFile.ServiceInstaller = new ServiceInstaller
            {
                Name = "PersonalCloud.WindowsService",
                DisplayName = "Personal Cloud Service",
                StartOn = SvcEvent.Install,
                StopOn = SvcEvent.InstallUninstall_Wait,
                RemoveOn = SvcEvent.Uninstall_Wait,
                DelayedAutoStart = true,
                ServiceSid = ServiceSid.none,
                FirstFailureActionType = FailureActionType.restart,
                SecondFailureActionType = FailureActionType.restart,
                ThirdFailureActionType = FailureActionType.restart,
                RestartServiceDelayInSeconds = 30,
                ResetPeriodInDays = 1,
            };

            project.UI = WUI.WixUI_InstallDir;
            project.Version = new Version(versionStr);
            project.InstallScope = InstallScope.perMachine;
            project.PreserveTempFiles = true;
            project.OutFileName = outputFilename;
            project.BackgroundImage = Path.Combine(dataFolder, "dlgbmp.png");
            project.BannerImage = Path.Combine(dataFolder, "bannerbmp.bmp");

            project.Include(WixExtension.Util);
            project.WixSourceGenerated += Project_WixSourceGenerated;

            project.Language = "en-US";
            project.LocalizationFile = @"Localization\en-US.wxl";
            project.LicenceFile = Path.Combine(dataFolder, "License.en-US.rtf");
            string productMsi = project.BuildMsi();

            project.Language = "zh-CN";
            project.LicenceFile = Path.Combine(dataFolder, "License.zh-CN.rtf");
            string mstFile = project.BuildLanguageTransform(productMsi, project.Language, @"Localization\zh-CN.wxl");

            productMsi.EmbedTransform(mstFile);
            using (var database = new Database(productMsi, DatabaseOpenMode.Direct))
            {
                database.SummaryInfo.Template = platformString2 + BA.Languages.ToLcidList();
            }

            return productMsi;
        }

        static int Run(string exe, string args)
            => new ExternalTool { ExePath = exe, Arguments = args }.ConsoleRun();

        static void Project_WixSourceGenerated(XDocument document)
        {
            var product = document.Root.Select("Product");
            if (product != null)
            {
                var xe = XElement.Parse(
                    @"<UI>
                        <Publish Dialog=""ExitDialog""
                            Control=""Finish""
                            Event=""DoAction""
                            Value=""LaunchApplication""> WIXUI_EXITDIALOGOPTIONALCHECKBOX = 1 and NOT Installed </Publish>
                    </UI>");
                product.Add(xe);
                product.Select("InstallExecuteSequence").Remove();

                xe = WixExtension.Util.XElement("PermissionEx",
                    "User=Everyone; ServiceQueryStatus=yes; ServiceQueryConfig=yes; ServiceStart=yes; ServiceStop=yes");

                var items = document.Root.Descendants(XName.Get("ServiceInstall", "http://schemas.microsoft.com/wix/2006/wi"));
                foreach (var item in items)
                {
                    item.Add(xe);
                }
            }
        }

        private static void BuildBundle(Platform platform, string dataFolder, string msiFile)
        {
            var bootstrapper =
                new Bundle("Personal Cloud",
                    new PackageGroupRef("NetFx40Web"),
                    new ExePackage(Path.Combine(dataFolder, "DokanSetup_redist.exe"))
                    {
                        Vital = true,
                        InstallCondition = new Condition("InstallDokanDriver=\"yes\""),
                        InstallCommand = "/install /passive /norestart"
                    },
                    new MsiPackage(msiFile)
                    {
                        Id = BA.MainPackageId,
                        DisplayInternalUI = true,
                        Visible = true,
                        MsiProperties = "ProductLanguage=[ProductLanguage]"
                    });

            bootstrapper.SetVersionFromFile(msiFile);
            bootstrapper.UpgradeCode = new Guid("821F3AF0-1F3B-4211-8D5F-E0C076111058");
            bootstrapper.Application = new ManagedBootstrapperApplication("%this%", "BootstrapperCore.config");

            bootstrapper.SuppressWixMbaPrereqVars = true;
            bootstrapper.PreserveTempFiles = true;

            bootstrapper.DisableModify = "yes";
            bootstrapper.DisableRemove = true;

            bootstrapper.Build(msiFile.PathChangeExtension(".exe"));
        }
    }
}
