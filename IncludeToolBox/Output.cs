using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace IncludeToolbox
{
    public class Output
    {
        static public Output Instance { private set; get; } = new Output();

        private Output()
        {
        }

        private OutputWindowPane outputWindowPane = null;

        public void Init()
        {
            EnvDTE.DTE dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            if (dte?.Windows.Count > 0)
            {
                Window window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
                OutputWindow outputWindow = (OutputWindow) window.Object;
                outputWindowPane = outputWindow.OutputWindowPanes.Add("IncludeToolBox");
            }
        }

        public void WriteLine(string line)
        {
            if (outputWindowPane == null)
            {
                Init();
            }
            if (outputWindowPane != null)
            {
                System.Diagnostics.Debug.Assert(outputWindowPane != null);
                outputWindowPane.OutputString(line);
            }
        }

        public void WriteLine(string line, params object[] stringParams)
        {
            string output = string.Format(line, stringParams);
            WriteLine(output);
        }

        public void ErrorMsg(string message, params object[] stringParams)
        {
            string output = string.Format(message, stringParams);
            WriteLine(output);
            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, output, "Include Toolbox", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }


        public void InfoMsg(string message, params object[] stringParams)
        {
            string output = string.Format(message, stringParams);
            WriteLine(output);
            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, output, "Include Toolbox", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
