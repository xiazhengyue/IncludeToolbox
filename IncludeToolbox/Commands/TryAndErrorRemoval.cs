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
    internal sealed class TryAndErrorRemoval : CommandBase<TryAndErrorRemoval>
    {
        public override CommandID CommandID => new CommandID(CommandSetGuids.MenuGroup, 0x0104);

        public TryAndErrorRemoval()
        {
        }

        private volatile bool lastBuildSuccessful;
        private AutoResetEvent outputWaitEvent = new AutoResetEvent(false);
        private const int timeoutMS = 30000; // 30 seconds

        private static VCFileConfiguration GetFileConfig(EnvDTE.Document document, out string reasonForFailure)
        {
            if (document == null)
            {
                reasonForFailure = "No document.";
                return null;
            }

            var project = document.ProjectItem?.ContainingProject;
            VCProject vcProject = project.Object as VCProject;
            if (vcProject == null)
            {
                reasonForFailure = "The given document does not belong to a VC++ Project.";
                return null;
            }

            VCFile vcFile = document.ProjectItem?.Object as VCFile;
            if (vcFile == null)
            {
                reasonForFailure = "The given document is not a VC++ file.";
                return null;
            }
            IVCCollection fileConfigCollection = vcFile?.FileConfigurations;
            VCFileConfiguration fileConfig = fileConfigCollection?.Item(vcProject.ActiveConfiguration.Name);
            if (fileConfig == null)
            {
                reasonForFailure = "Failed to retrieve file config from document.";
                return null;
            }

            reasonForFailure = "";
            return fileConfig;
        }


        private void PerformTryAndErrorRemoval(EnvDTE.Document document)
        {
            if (document == null)
                return;

            string errorMessage;
            var fileConfig = GetFileConfig(document, out errorMessage);
            if (fileConfig == null)
            {
                Output.Instance.WriteLine(errorMessage);
                return;
            }

            // Start wait dialog.
            IVsThreadedWaitDialog2 progressDialog = null;
            {
                var dialogFactory = ServiceProvider.GetService(typeof(SVsThreadedWaitDialogFactory)) as IVsThreadedWaitDialogFactory;
                if (dialogFactory == null)
                {
                    Output.Instance.WriteLine("Failed to get Dialog Factory for wait dialog.");
                    return;
                }
                dialogFactory.CreateInstance(out progressDialog);
                if (progressDialog == null)
                {
                    Output.Instance.WriteLine("Failed to get create wait dialog.");
                    return;
                }
                progressDialog.StartWaitDialogWithPercentageProgress(
                    szWaitCaption: "Include Toolbox",
                    szWaitMessage: "Running Try & Error Removal - Parsing",
                    szProgressText: null,
                    varStatusBmpAnim: null,
                    szStatusBarText: "Running Try & Error Removal",
                    fIsCancelable: true,
                    iDelayToShowDialog: 0,
                    iTotalSteps: 10,    // Will be replaced.
                    iCurrentStep: 0);
            }

            // Extract all includes.
            IncludeFormatter.IncludeLineInfo[] documentLines = null;
            ITextBuffer textBuffer = null;
            try
            {
                document.Activate();
                var documentTextView = VSUtils.GetCurrentTextViewHost();
                textBuffer = documentTextView.TextView.TextBuffer;
                string documentText = documentTextView.TextView.TextSnapshot.GetText();
                documentLines = IncludeFormatter.IncludeLineInfo.ParseIncludes(documentText, false, null);
            }
            catch (Exception ex)
            {
                Output.Instance.WriteLine("Unexpected error: {0}", ex);
                progressDialog.EndWaitDialog();
                return;
            }
            int numIncludes = documentLines.Count(x => x.LineType != IncludeFormatter.IncludeLineInfo.Type.NoInclude);



            // Hook into build events.
            document.DTE.Events.BuildEvents.OnBuildProjConfigDone += OnBuildConfigFinished;
            document.DTE.Events.BuildEvents.OnBuildDone += OnBuildFinished;


            // The rest runs in a separate thread sicne the compile function is non blocking and we want to use BuildEvents
            // We are not using Task, since we want to make use of WaitHandles - using this together with Task is a bit more complicated to get right.
            outputWaitEvent.Reset();
            new System.Threading.Thread(() =>
            {
                try
                {
                    int currentStep = 0;

                    // For ever include line..
                    for (int line = documentLines.Length - 1; line >= 0; --line)
                    {
                        if (documentLines[line].LineType == IncludeFormatter.IncludeLineInfo.Type.NoInclude)
                            continue;

                        // Update progress.
                        string waitMessage = string.Format("Trying to remove '{0}'", documentLines[line].IncludeContent);
                        bool canceled = false;
                        progressDialog.UpdateProgress(
                            szUpdatedWaitMessage: "Running Try & Error Removal - Removing Includes",
                            szProgressText: waitMessage,
                            szStatusBarText: waitMessage,
                            iCurrentStep: currentStep + 1,
                            iTotalSteps: numIncludes + 1,
                            fDisableCancel: false,
                            pfCanceled: out canceled);
                        if (canceled)
                            break;

                        ++currentStep;

                        // Remove include - this needs to be done on the main thread.
                        int currentLine = line;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            using (var edit = textBuffer.CreateEdit())
                            {
                                edit.Delete(edit.Snapshot.Lines.ElementAt(currentLine).ExtentIncludingLineBreak);
                                edit.Apply();
                            }
                            outputWaitEvent.Set();
                        });
                        outputWaitEvent.WaitOne();

                        // Compile - In rare cases VS tells us that we are still building which should not be possible because we have received OnBuildFinished
                        // As a workaround we just try again a few times.
                        {
                            const int maxNumCompileAttempts = 3;
                            bool fail = false;
                            for (int numCompileFails = 0; numCompileFails < maxNumCompileAttempts; ++ numCompileFails)
                            {
                                try
                                {
                                    fileConfig.Compile(true, false); // WaitOnBuild==true always fails.    
                                }
                                catch (Exception e)
                                {
                                    if (numCompileFails == maxNumCompileAttempts - 1)
                                    {
                                        Output.Instance.WriteLine("Compile Failed:\n{0}", e);
                                        fail = true;
                                    }
                                    else
                                    {
                                        // Try again.
                                        System.Threading.Thread.Sleep(1);
                                        continue;
                                    }
                                }
                                break;
                            }
                            if (fail) break;
                        }

                        // Wait till woken.
                        bool noTimeout = outputWaitEvent.WaitOne(timeoutMS);

                        // Undo removal if compilation failed.
                        if (!noTimeout || !lastBuildSuccessful)
                        {
                            Output.Instance.WriteLine("Could not remove #include: '{0}'", documentLines[line].IncludeContent);
                            document.Undo();
                            if (!noTimeout)
                            {
                                Output.Instance.ErrorMsg("Compilation of {0} timeouted!", document.Name);
                                break;
                            }
                        }
                        else
                        {
                            Output.Instance.WriteLine("Successfully removed #include: '{0}'", documentLines[line].IncludeContent);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Output.Instance.WriteLine("Unexpected error: {0}", ex);
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Close Progress bar.
                        progressDialog.EndWaitDialog();
                        // Remove build hook again.
                        document.DTE.Events.BuildEvents.OnBuildDone -= OnBuildFinished;
                        document.DTE.Events.BuildEvents.OnBuildProjConfigDone -= OnBuildConfigFinished;
                    });
                }
            }).Start();
        }

        private void OnBuildFinished(vsBuildScope scope, vsBuildAction action)
        {
            outputWaitEvent.Set();
        }

        private void OnBuildConfigFinished(string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            lastBuildSuccessful = success;
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
                    PerformTryAndErrorRemoval(document);
                }
                catch (Exception ex)
                {
                    Output.Instance.WriteLine("Unexpected error: {0}", ex);
                }
            }
        }
    }
}
