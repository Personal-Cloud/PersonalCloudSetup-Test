using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using DetectDokan;

#if WIX4
using WixToolset.Bootstrapper;
#else

using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

#endif

[assembly: BootstrapperApplication(typeof(BA))]

public class BA : BootstrapperApplication
{
    public static string MainPackageId = "PersonalCloudPackageId";
    public static string Languages = "en-US,zh-CN";

    public CultureInfo SelectedLanguage { get; set; }

    public CultureInfo[] SupportedLanguages => Languages.Split(',')
                                                        .Select(x => new CultureInfo(x))
                                                        .ToArray();

    public BA()
    {
        SelectedLanguage = SupportedLanguages.FirstOrDefault();
        this.Error += (s, e) => MessageBox.Show("Error: " + e.ErrorMessage);
        this.ApplyComplete += (s, e) =>
        {
            Engine.Quit(0);
        };
    }

    PackageState DetectMainPackage()
    {
        var done = new AutoResetEvent(false);

        var packageState = PackageState.Unknown;

        this.DetectPackageComplete += (object sender, DetectPackageCompleteEventArgs e) =>
        {
            if (e.PackageId == BA.MainPackageId)
            {
                packageState = e.State;
                done.Set();
            }
        };

        this.Engine.Detect();

        done.WaitOne();

        return packageState;
    }

    /// <summary>
    /// Entry point that is called when the bootstrapper application is ready to run.
    /// </summary>
    protected override void Run()
    {
        var packageState = this.DetectMainPackage();
        var launchAction = this.Command.Action;
        if (launchAction == LaunchAction.Install && packageState == PackageState.Present)
        {
            MessageBox.Show("Personal Cloud is already installed");
            Engine.Quit(0);
            return;
        }

        if (launchAction == LaunchAction.Install)
        {
            var chooseLanguageView = new ChooseLanguageView() { DataContext = this };
            var result = chooseLanguageView.ShowDialog();

            if (result == true)
            {
                bool dokanInstalled = DokanDriverUtility.QueryVersion(out uint dokanVersion);
                if (!dokanInstalled || dokanVersion < 0x190)
                {
                    Engine.StringVariables["InstallDokanDriver"] = "yes";
                }
            }

            if (result == true)
            {
                Engine.StringVariables["ProductLanguage"] = $"{SelectedLanguage.LCID}";

                Engine.Plan(launchAction);
                Engine.Apply(new WindowInteropHelper(chooseLanguageView).Handle);

                Dispatcher.CurrentDispatcher.VerifyAccess();
                Dispatcher.Run();
            }
        }
        Engine.Quit(0);
    }
}