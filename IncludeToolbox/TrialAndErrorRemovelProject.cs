using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox
{
    public sealed class TrialAndErrorRemovalProject
    {
        private TrialAndErrorRemovalWithoutDialog impl;
        private TrialAndErrorRemovalOptionsPage settings;
        private string projectName = "";
        private ProjectItems projectItems = null;
        private Queue<ProjectItem> projectFiles;
        private int numTotoalProcessedFiles = 0;
        private int numTotalRemovedIncludes = 0;

        public delegate void ProjectFinishedHandler(int numProcessedFiles, int numRemovedIncludes, string projectName);
        public event ProjectFinishedHandler OnProjectFinished;

        public TrialAndErrorRemovalProject(TrialAndErrorRemovalOptionsPage settings)
        {
            this.settings = settings;
            projectFiles = new Queue<ProjectItem>();
            impl = new TrialAndErrorRemovalWithoutDialog();
            impl.OnFileFinished += OnDocumentIncludeRemovalFinished;
        }

        public async Task PerformTrialAndErrorRemoval(Project project)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (TrialAndErrorRemovalWithoutDialog.WorkInProgress)
            {
                await Output.Instance.ErrorMsg("Trial and error include removal already in progress!");
                return;
            }

            try
            {
                if (project == null)
                {
                    Output.Instance.WriteLine("Project is null!");
                    return;
                }
                projectName = project.Name;
                projectItems = project.ProjectItems;

                projectFiles.Clear();
                RecursiveFindFilesInProject(projectItems, ref projectFiles);

                //if (projectFiles.Count > 2)
                //{
                //    if (await Output.Instance.YesNoMsg("Attention! Trial and error include removal on large projects make take up to several hours! In this time you will not be able to use Visual Studio. Are you sure you want to continue?")
                //        != Output.MessageResult.Yes)
                //    {
                //        return;
                //    }
                //}

                numTotalRemovedIncludes = 0;
                await ProcessNextFile();
            }
            finally
            {
                projectItems = null;
            }
        }

        private void OnDocumentIncludeRemovalFinished(int removedIncludes, bool canceled)
        {
            _ = Task.Run(async () =>
            {
                numTotoalProcessedFiles++;
                numTotalRemovedIncludes += removedIncludes;
                if (canceled || !await ProcessNextFile())
                {
                    Output.Instance.WriteLine("{0} -- {1} files, {2} #include directives", projectName, numTotoalProcessedFiles, numTotalRemovedIncludes);
                    OnProjectFinished.Invoke(numTotoalProcessedFiles, numTotalRemovedIncludes, projectName);
                    numTotoalProcessedFiles = 0;
                    numTotalRemovedIncludes = 0;
                }
            });
        }

        private async Task<bool> ProcessNextFile()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            while (projectFiles.Count > 0)
            {
                ProjectItem projectItem = projectFiles.Dequeue();

                Document document = null;
                try
                {
                    string name = projectItem.Name;
                    if (!projectItem.IsOpen)
                        document = projectItem.Open().Document;
                    else
                        document = projectItem.Document;
                }
                catch (Exception)
                {
                }
                if (document == null)
                    continue;

                bool started = await impl.PerformTrialAndErrorIncludeRemoval(document, settings);
                if (started)
                    return true;
            }
            return false;
        }

        private static void RecursiveFindFilesInProject(ProjectItems items, ref Queue<ProjectItem> projectFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var e = items.GetEnumerator();
            while (e.MoveNext())
            {
                var item = e.Current;
                if (item == null)
                    continue;
                var projectItem = item as ProjectItem;
                if (projectItem == null)
                    continue;

                Guid projectItemKind = new Guid(projectItem.Kind);
                if (projectItemKind == VSConstants.GUID_ItemType_VirtualFolder ||
                    projectItemKind == VSConstants.GUID_ItemType_PhysicalFolder)
                {
                    RecursiveFindFilesInProject(projectItem.ProjectItems, ref projectFiles);
                }
                else if (projectItemKind == VSConstants.GUID_ItemType_PhysicalFile)
                {
                    if (projectItem.Name.EndsWith(".cpp"))
                        projectFiles.Enqueue(projectItem);
                }
                else
                {
                    Output.Instance.WriteLine("Unexpected Error: Unknown projectItem {0} of Kind {1}", projectItem.Name, projectItem.Kind);
                }
            }
        }
    }
}
