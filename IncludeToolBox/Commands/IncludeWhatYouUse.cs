using System;
using System.ComponentModel.Design;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace IncludeToolbox.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class IncludeWhatYouUse
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0103;

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeWhatYouUse"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private IncludeWhatYouUse(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(MenuCommandSet.Guid, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static IncludeWhatYouUse Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new IncludeWhatYouUse(package);
        }
       
        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var document = Utils.GetActiveDocument();
            if (document == null)
            {
                Output.Instance.WriteLine("No active document!");
                return;
            }
            var project = document.ProjectItem.ContainingProject;
            if (project == null)
            {
                Output.Instance.WriteLine("The document {0} is not part of a project.", document.Name);
                return;
            }
            var compilerTool = Utils.GetVCppCompilerTool(project);
            if (compilerTool == null)
                return;

            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.FileName = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "include-what-you-use.exe");
                process.StartInfo.Arguments = "";

                // Includes and Preprocessor.
                var includeEntries = Utils.GetProjectIncludeDirectories(project, false);
                process.StartInfo.Arguments = includeEntries.Aggregate("", (current, inc) => current + ("-I \"" + inc + "\" "));
                process.StartInfo.Arguments = compilerTool.PreprocessorDefinitions.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries).
                    Aggregate(process.StartInfo.Arguments, (current, def) => current + ("-D" + def + " "));
                process.StartInfo.Arguments += " -DM_X64 -DM_AMD64 ";// TODO!!!

                // Clang options
                process.StartInfo.Arguments += "-w -x c++ -std=c++14 -fcxx-exceptions -fexceptions -fms-compatibility -fms-extensions -fmsc-version=1900 -Wno-invalid-token-paste "; // todo fmsc-version!
                // icwyu options
                process.StartInfo.Arguments += "-Xiwyu --verbose=1 -Xiwyu --transitive_includes_only "; // todo: Espose stuff.

                

                // Finally, the file itself.
                process.StartInfo.Arguments += "\"";
                process.StartInfo.Arguments += document.FullName;
                process.StartInfo.Arguments += "\"";

                Output.Instance.Write("Running command '{0}' with following arguments:\n{1}\n\n", process.StartInfo.FileName, process.StartInfo.Arguments);

                // Start the child process.
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += (s, args) => Output.Instance.WriteLine(args.Data);
                process.ErrorDataReceived += (s, args) => Output.Instance.WriteLine(args.Data);
                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                process.CancelOutputRead();
                process.CancelErrorRead();
            }
        }
    }
}
