using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using EnvDTE;
using Microsoft.VisualStudio.Text;

namespace IncludeToolbox.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TryAndErrorRemoval_Project : CommandBase<TryAndErrorRemoval_Project>
    {
        public override CommandID CommandID => new CommandID(CommandSetGuids.ProjectGroup, 0x0100);

        private TryAndErrorRemoval impl;
        private TryAndErrorRemovalOptionsPage settings;

        private IVCCollection fileCollection = null;
        private int fileIdx = 0;
        private int numTotalRemovedIncludes = 0;


        public TryAndErrorRemoval_Project()
        {
        }

        protected override void SetupMenuCommand()
        {
            base.SetupMenuCommand();

            impl = new TryAndErrorRemoval();
            impl.OnFileFinished += OnDocumentIncludeRemovalFinished;
            menuCommand.BeforeQueryStatus += UpdateVisibility;

            settings = (TryAndErrorRemovalOptionsPage)Package.GetDialogPage(typeof(TryAndErrorRemovalOptionsPage));
        }

        private void OnDocumentIncludeRemovalFinished(int removedIncludes, bool canceled)
        {
            numTotalRemovedIncludes += removedIncludes;
            if (canceled || !ProcessNextFile())
            {
                Output.Instance.InfoMsg("Removed total of {0} #include directives from project.", numTotalRemovedIncludes);
                fileIdx = 0;
                numTotalRemovedIncludes = 0;
            }
        }

        private void UpdateVisibility(object sender, EventArgs e)
        {
            string reason;
            var project = GetSelectedCppProject(out reason);
            menuCommand.Visible = project != null;
        }

        static VCProject GetSelectedCppProject(out string reasonForFailure)
        {
            reasonForFailure = "";

            var selectedItems = VSUtils.GetDTE().SelectedItems;
            if (selectedItems.Count < 1)
            {
                reasonForFailure = "Selection is empty!";
                return null;
            }

            // Reading .Item(object) behaves weird, but iterating works.
            foreach (SelectedItem item in selectedItems)
            {
                VCProject vcProject = item?.Project?.Object as VCProject;
                if (vcProject != null)
                {
                    return vcProject;
                }
            }

            reasonForFailure = "Selection does not contain a C++ project!";
            return null;
        }

        private bool ProcessNextFile()
        {
            if (fileCollection == null)
                return false;

            for (; fileIdx < fileCollection.Count; ++fileIdx)
            {
                var vcFile = fileCollection.Item(fileIdx) as VCFile;
                if (vcFile == null)
                    continue;

                var projectItem = vcFile.Object as ProjectItem;
                if (projectItem == null)
                    continue;

                bool isHeader;
                string reason;

                EnvDTE.Document document = null;
                try
                {
                    document = projectItem.Open().Document;
                }
                catch (Exception)
                {
                }
                if (document == null)
                    continue;

                var fileConfig = TryAndErrorRemoval.GetFileConfig(document, out reason, out isHeader);
                if (isHeader && fileConfig != null)
                    continue;

                impl.PerformTryAndErrorRemoval(document, settings);
                ++fileIdx;
                return true;
            }

            fileCollection = null;
            return false;
        }

        private void PerformTryAndErrorRemoval(VCProject project)
        {
            fileCollection = project.Files as IVCCollection;
            if (fileCollection == null)
            {
                Output.Instance.WriteLine("Project has no file collection.");
                return;
            }

            if (fileCollection.Count > 2)
            {
                int result = VsShellUtilities.ShowMessageBox(Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider, 
                                            "Attention! Try and error include removal on large projects make take up to several hours! In this time you will not be able to use Visual Studio. Are you sure you want to continue?",
                                            "Include Toolbox", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                if (result != 6)
                {
                    return;
                }
            }

            fileIdx = 0;
            numTotalRemovedIncludes = 0;
            ProcessNextFile();
        }
    

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        protected override void MenuItemCallback(object sender, EventArgs e)
        {
            if (TryAndErrorRemoval.WorkInProgress)
            {
                Output.Instance.ErrorMsg("Try and error include removal already in progress!");
                return;
            }

            try
            {
                string reasonForFailure;
                VCProject project = GetSelectedCppProject(out reasonForFailure);
                if (project == null)
                {
                    Output.Instance.WriteLine(reasonForFailure);
                    return;
                }

                PerformTryAndErrorRemoval(project);
            }
            catch (Exception ex)
            {
                Output.Instance.WriteLine("Unexpected error: {0}", ex);
                fileCollection = null;
            }
        }
    }
}
