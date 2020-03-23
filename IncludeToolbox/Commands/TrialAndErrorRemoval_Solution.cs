using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.Commands
{
    internal sealed class TrialAndErrorRemoval_Solution : CommandBase<TrialAndErrorRemoval_Solution>
    {
        public override CommandID CommandID => new CommandID(CommandSetGuids.SolutionGroup, 0x0100);

        private TrialAndErrorRemovalProject projectProcessor = null;
        private Queue<Project> vcProjects = null;
        int numTotoalProcessedFiles = 0;
        int numTotalRemovedIncludes = 0;
        private List<string> selectedProjects = null;
        protected override void SetupMenuCommand()
        {
            base.SetupMenuCommand();

            projectProcessor = new TrialAndErrorRemovalProject((TrialAndErrorRemovalOptionsPage)Package.GetDialogPage(typeof(TrialAndErrorRemovalOptionsPage)));
            vcProjects = new Queue<Project>();
            //selectedProjects = Utils.LoadProjectsConfig();
            projectProcessor.OnProjectFinished += HandleProject_OnProjectFinished;
        }

        private void HandleProject_OnProjectFinished(int numProcessedFiles, int numRemovedIncludes, string projectName)
        {
            _ = Task.Run(async () => 
            {
                numTotoalProcessedFiles += numProcessedFiles;
                numTotalRemovedIncludes += numRemovedIncludes;
                if (!await ProcessNextProject())
                {
                    Output.Instance.WriteLine("{0} -- {1} files, {2} #include directives", "Total", numTotoalProcessedFiles, numTotalRemovedIncludes);
                }
            });          
        }

        protected override async Task MenuItemCallback(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (TrialAndErrorRemovalWithoutDialog.WorkInProgress)
            {
                await Output.Instance.InfoMsg("Trial and error includes are in progress!");
                return;
            }

            await PerformanceTrialAndErrorRemovel();

            return;
        }

        private async Task PerformanceTrialAndErrorRemovel()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            List<string> proNames = new List<string>();
            vcProjects.Clear();
            Projects allProjs = VSUtils.GetDTE().Solution.Projects;
            foreach (Project proj in allProjs)
            {
                if(VSUtils.VCUtils.IsVCProject(proj))
                {
                    proNames.Add(proj.Name);
                    vcProjects.Enqueue(proj);
                }
                    
            }
            {// to delete

                Utils.SaveProjectsConfig(proNames);
            }

            if (await Output.Instance.YesNoMsg("Attention! Trial and error include removal on large solution make take up to several hours! In this time you will not be able to use Visual Studio. Are you sure you want to continue?")
                   != Output.MessageResult.Yes)
            {
                return;
            }

            numTotoalProcessedFiles = 0;
            numTotalRemovedIncludes = 0;

            await ProcessNextProject();
        }

        private async Task<bool> ProcessNextProject()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if(vcProjects.Count > 0)
            {
                Project project = vcProjects.Dequeue();
                await projectProcessor.PerformTrialAndErrorRemoval(project);

                return true;
            }

            return false;
        }
    }
}
