using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncludeToolbox.GraphWindow.Commands
{
    class SaveDGML : IncludeToolbox.Commands.CommandBase<SaveDGML>
    {
        public override CommandID CommandID { get; } = new CommandID(IncludeToolbox.Commands.CommandSetGuids.GraphWindowToolbarCmdSet, 0x104);

        private IncludeGraphViewModel viewModel;

        public void SetViewModel(IncludeGraphViewModel viewModel)
        {
            this.viewModel = viewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            menuCommand.Enabled = viewModel.NumIncludes > 0;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(IncludeGraphViewModel.NumIncludes))
            {
                menuCommand.Enabled = viewModel.NumIncludes > 0;
            }
        }

        protected override void MenuItemCallback(object sender, EventArgs e)
        {
            if (viewModel == null)
                return;

            if (viewModel.NumIncludes <= 0)
            {
                Output.Instance.ErrorMsg("There is no include tree to save!");
                return;
            }

            // Show save dialog.
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = ".dgml";
            dlg.DefaultExt = ".dgml";
            dlg.Filter = "Text documents (.dgml)|*.dgml";
            bool? result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result ?? false)
            {
                try
                {
                    viewModel.SaveGraph(dlg.FileName);
                }
                catch
                {
                    Output.Instance.ErrorMsg($"Failed to safe dgml to {dlg.FileName}");
                    return;
                }

                if (Output.Instance.YesNoMsg("Saved dgml successfully. Do you want to open it in Visual Studio?") == Output.MessageResult.Yes)
                {
                    VSUtils.OpenFileAndShowDocument(dlg.FileName);
                }
            }
        }
    }
}
