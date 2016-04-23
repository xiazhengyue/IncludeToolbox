

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
    internal sealed class TryAndErrorRemoval_CodeWindow : CommandBase<TryAndErrorRemoval_CodeWindow>
    {
        public override CommandID CommandID => new CommandID(CommandSetGuids.MenuGroup, 0x0104);

        private TryAndErrorRemoval impl;

        public TryAndErrorRemoval_CodeWindow()
        {
        }

        protected override void SetupMenuCommand()
        {
            base.SetupMenuCommand();

            impl = new TryAndErrorRemoval();
            menuCommand.BeforeQueryStatus += UpdateVisibility;
        }

        private void UpdateVisibility(object sender, EventArgs e)
        {
            string reason;
            bool isHeader;
            var config = TryAndErrorRemoval.GetFileConfig(VSUtils.GetDTE().ActiveDocument, out reason, out isHeader);
            menuCommand.Visible = (config != null) && !isHeader;
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
            var document = VSUtils.GetDTE().ActiveDocument;
            if (document != null)
            {
                try
                {
                    impl.PerformTryAndErrorRemoval(document);
                }
                catch (Exception ex)
                {
                    Output.Instance.WriteLine("Unexpected error: {0}", ex);
                }
            }
        }
    }
}