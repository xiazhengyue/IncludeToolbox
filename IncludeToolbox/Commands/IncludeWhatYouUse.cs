using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.VCProjectEngine;

namespace IncludeToolbox.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class IncludeWhatYouUse : CommandBase<IncludeWhatYouUse>
    {
        public override CommandID CommandID => new CommandID(CommandSetGuids.MenuGroup, 0x0103);

        public IncludeWhatYouUse()
        {
        }

        protected override void SetupMenuCommand()
        {
            base.SetupMenuCommand();
            menuCommand.BeforeQueryStatus += UpdateVisibility;
        }

        private void UpdateVisibility(object sender, EventArgs e)
        {
            // Needs to be part of a VCProject to be aplicable.
            var document = VSUtils.GetDTE()?.ActiveDocument;
            menuCommand.Visible = VSUtils.VCUtils.IsVCProject(document?.ProjectItem?.ContainingProject);
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
            var settings = (IncludeWhatYouUseOptionsPage)Package.GetDialogPage(typeof(IncludeWhatYouUseOptionsPage));
            Output.Instance.Clear();

            var document = VSUtils.GetDTE().ActiveDocument;
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

            // Save all documents.
            document.DTE.Documents.SaveAll();

            // Start wait dialog.
            var dialogFactory = ServiceProvider.GetService(typeof(SVsThreadedWaitDialogFactory)) as IVsThreadedWaitDialogFactory;
            IVsThreadedWaitDialog2 dialog = null;
            if (dialogFactory != null)
            {
                dialogFactory.CreateInstance(out dialog);
            }
            dialog?.StartWaitDialog("Include Toolbox", "Running Include-What-You-Use", null, null, "Running Include-What-You-Use", 0, false, true);

            var iwyu = new IncludeToolbox.IncludeWhatYouUse();
            string output = iwyu.RunIncludeWhatYouUse(document.FullName, project, settings);
            if (settings.ApplyProposal && output != null)
            {
                iwyu.Apply(output);
            }

            dialog?.EndWaitDialog();
        }
    }
}
