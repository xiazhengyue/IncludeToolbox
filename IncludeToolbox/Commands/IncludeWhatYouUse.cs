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
            var project = document?.ProjectItem?.ContainingProject?.Object as VCProject;
            menuCommand.Visible = project != null;
        }

        class FormatTask
        {
            public override string ToString()
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("Remove:\n");
                foreach(int i in linesToRemove)
                    stringBuilder.AppendFormat("{0},", i);
                stringBuilder.Append("\nAdd:\n");
                foreach (string s in linesToAdd)
                    stringBuilder.AppendFormat("{0}\n", s);
                return stringBuilder.ToString();
            }

            public readonly HashSet<int> linesToRemove = new HashSet<int>();
            public readonly List<string> linesToAdd = new List<string>();
        };


        private Dictionary<string, FormatTask> ParseOutput(string output)
        {
            Dictionary<string, FormatTask> fileTasks = new Dictionary<string, FormatTask>();
            FormatTask currentTask = null;
            bool removeCommands = true;

            var removeRegex = new Regex(@"^-\s+#include\s*[""<](.+)["">]\s+\/\/ lines (\d+)-(\d+)$");
            var addlineRegex = new Regex(@"(#include\s*[""<].+["">])|(^class.+;$)|(^struct.+;$)");
            //- #include <Core/Basics.h>  // lines 3-3

            // Parse what to do.
            var lines = Regex.Split(output, "\r\n|\r|\n");
            bool lastLineWasEmpty = false;
            foreach (string line in lines)
            {
                if (line.Length == 0)
                {
                    if (lastLineWasEmpty)
                    {
                        currentTask = null;
                    }

                    lastLineWasEmpty = true;
                    continue;
                }
                
                int i = line.IndexOf(" should add these lines:");
                if (i < 0)
                {
                    i = line.IndexOf(" should remove these lines:");
                    if (i >= 0)
                        removeCommands = true;
                }
                else
                {
                    removeCommands = false;
                }

                if (i >= 0)
                {
                    string file = line.Substring(0, i);

                    if (!fileTasks.TryGetValue(file, out currentTask))
                    {
                        currentTask = new FormatTask();
                        fileTasks.Add(file, currentTask);
                    }
                }
                else if (currentTask != null)
                {
                    if (removeCommands)
                    {
                        var match = removeRegex.Match(line);
                        if (match.Success)
                        {
                            int removeStart, removeEnd;
                            if (int.TryParse(match.Groups[2].Value, out removeStart) &&
                                int.TryParse(match.Groups[3].Value, out removeEnd))
                            {
                                for (int lineIdx = removeStart; lineIdx <= removeEnd; ++lineIdx)
                                    currentTask.linesToRemove.Add(lineIdx);
                            }
                        }
                        else if (lastLineWasEmpty)
                        {
                            currentTask = null;
                        }
                    }
                    else
                    {
                        var match = addlineRegex.Match(line);
                        if (match.Success)
                        {
                            currentTask.linesToAdd.Add(line);
                        }
                        else if (lastLineWasEmpty)
                        {
                            currentTask = null;
                        }
                    }
                }

                lastLineWasEmpty = false;
            }

            return fileTasks;
        }

        void ApplyTasks(Dictionary<string, FormatTask> tasks)
        {
            EnvDTE.DTE dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            var textManager = this.ServiceProvider.GetService(typeof(SVsTextManager)) as IVsTextManager;

            foreach (KeyValuePair<string, FormatTask> entry in tasks)
            {
                string filename = entry.Key.Replace('/', '\\'); // Classy. But Necessary.
                EnvDTE.Window fileWindow = dte.ItemOperations.OpenFile(filename);
                if (fileWindow == null)
                {
                    Output.Instance.ErrorMsg("Failed to open File {0}", filename);
                    continue;
                }
                fileWindow.Activate();

                var viewHost = VSUtils.GetCurrentTextViewHost();                
                using (var edit = viewHost.TextView.TextBuffer.CreateEdit())
                {
                    var originalLines = edit.Snapshot.Lines.ToArray();

                    // Add lines.
                    {
                        // Find last include.
                        // Will find even if commented out, but we don't care.
                        int lastIncludeLine = -1;
                        for(int line=originalLines.Length - 1; line>=0; --line)
                        {
                            if (originalLines[line].GetText().Contains("#include"))
                            {
                                lastIncludeLine = line;
                                break;
                            }
                        }

                        // Insert lines.
                        int insertPosition = 0;
                        if (lastIncludeLine >= 0 && lastIncludeLine < originalLines.Length)
                        {
                            insertPosition = originalLines[lastIncludeLine].EndIncludingLineBreak;
                        }
                        StringBuilder stringToInsert = new StringBuilder();
                        
                        foreach (string lineToAdd in entry.Value.linesToAdd)
                        {
                            stringToInsert.Clear();
                            stringToInsert.Append(lineToAdd);
                            stringToInsert.Append("\n"); // todo: Consistent new lines?
                            edit.Insert(insertPosition, stringToInsert.ToString());
                        }
                    }

                    // Remove lines.
                    // It should safe to do that last since we added includes at the bottom, this way there is no confusion with the text snapshot.
                    {
                        foreach (int lineToRemove in entry.Value.linesToRemove.Reverse())
                        {
                            edit.Delete(originalLines[lineToRemove].ExtentIncludingLineBreak);
                        }
                    }

                    edit.Apply();
                }

                // For Debugging:
                //Output.Instance.WriteLine("");
                //Output.Instance.WriteLine(entry.Key);
                //Output.Instance.WriteLine(entry.Value.ToString());
            }
        }

        private string RunIncludeWhatYouUse(string fullFileName, EnvDTE.Project project, IncludeWhatYouUseOptionsPage settings)
        {
            var compilerTool = VSUtils.GetVCppCompilerTool(project);
            if (compilerTool == null)
                return "";

            string output = "";
            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.FileName = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "include-what-you-use.exe");
                process.StartInfo.Arguments = "";

                // Includes and Preprocessor.
                var includeEntries = VSUtils.GetProjectIncludeDirectories(project, false);
                process.StartInfo.Arguments = includeEntries.Aggregate("", (current, inc) => current + ("-I \"" + inc + "\" "));
                process.StartInfo.Arguments = compilerTool.PreprocessorDefinitions.
                    Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).
                    Aggregate(process.StartInfo.Arguments, (current, def) => current + ("-D" + def + " "));
                process.StartInfo.Arguments += " -DM_X64 -DM_AMD64 ";// TODO!!!

                // Clang options
                process.StartInfo.Arguments += "-w -x c++ -std=c++14 -fcxx-exceptions -fexceptions -fms-compatibility -fms-extensions -fmsc-version=1900 -ferror-limit=0 -Wno-invalid-token-paste "; // todo fmsc-version!
                // icwyu options
                {
                    process.StartInfo.Arguments += "-Xiwyu --verbose=" + settings.LogVerbosity + " ";
                    for (int i = 0; i < settings.MappingFiles.Length; ++i)
                        process.StartInfo.Arguments += "-Xiwyu --mapping_file=" + settings.MappingFiles[i] + " ";
                    if (settings.NoDefaultMappings)
                        process.StartInfo.Arguments += "-Xiwyu --no_default_mappings ";
                    if (settings.PCHInCode)
                        process.StartInfo.Arguments += "-Xiwyu --pch_in_code ";
                    switch (settings.PrefixHeaderIncludes)
                    {
                        case IncludeWhatYouUseOptionsPage.PrefixHeaderMode.Add:
                            process.StartInfo.Arguments += "-Xiwyu --prefix_header_includes=add ";
                            break;
                        case IncludeWhatYouUseOptionsPage.PrefixHeaderMode.Remove:
                            process.StartInfo.Arguments += "-Xiwyu --prefix_header_includes=remove ";
                            break;
                        case IncludeWhatYouUseOptionsPage.PrefixHeaderMode.Keep:
                            process.StartInfo.Arguments += "-Xiwyu --prefix_header_includes=keep ";
                            break;
                    }
                    if (settings.TransitiveIncludesOnly)
                        process.StartInfo.Arguments += "-Xiwyu --transitive_includes_only ";
                }


                // Finally, the file itself.
                process.StartInfo.Arguments += "\"";
                process.StartInfo.Arguments += fullFileName;
                process.StartInfo.Arguments += "\"";

                Output.Instance.Write("Running command '{0}' with following arguments:\n{1}\n\n", process.StartInfo.FileName, process.StartInfo.Arguments);

                // Start the child process.
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += (s, args) =>
                {
                    Output.Instance.WriteLine(args.Data);
                    output += args.Data + "\n";
                };
                process.ErrorDataReceived += (s, args) =>
                {
                    Output.Instance.WriteLine(args.Data);
                    output += args.Data + "\n";
                };
                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                process.CancelOutputRead();
                process.CancelErrorRead();
            }

            return output;
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

            string output = RunIncludeWhatYouUse(document.FullName, project, settings);
            if (settings.ApplyProposal)
            {
                var tasks = ParseOutput(output);
                ApplyTasks(tasks);
            }

            dialog?.EndWaitDialog();
        }
    }
}
