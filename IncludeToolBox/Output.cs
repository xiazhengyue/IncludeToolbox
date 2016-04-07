using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace IncludeFormatter
{
    static internal class Output
    {
        static public void Error(string message)
        {
            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, message, "Include Formatter", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
        static public void Info(string message)
        {
            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, message, "Include Formatter", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
