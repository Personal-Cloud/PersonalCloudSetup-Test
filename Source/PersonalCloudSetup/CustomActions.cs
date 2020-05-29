using Microsoft.Deployment.WindowsInstaller;
using System.Diagnostics;
using System.IO;
using WixSharp;

public class CustomActions
{
    [CustomAction]
    public static ActionResult LaunchApplication(Session session)
    {
        return session.HandleErrors(() =>
        {
            Process proc = new Process();
            proc.StartInfo.FileName = Path.Combine(session.Property("INSTALLDIR"), @"GUI\PersonalCloud.WindowsConfigurator.exe");
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            proc.Start();
        });
    }
}
