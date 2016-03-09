using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.VCProjectEngine;

namespace IncludeFormatter.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class FormatIncludes
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormatIncludes"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private FormatIncludes(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet.Guid, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static FormatIncludes Instance
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
            Instance = new FormatIncludes(package);
        }

        private IWpfTextViewHost GetCurrentViewHost()
        {
            var textManager = this.ServiceProvider.GetService(typeof(SVsTextManager)) as IVsTextManager;

            IVsTextView textView = null;
            int mustHaveFocus = 1;
            textManager.GetActiveView(mustHaveFocus, null, out textView);

            var userData = textView as IVsUserData;
            if (userData == null)
            {
                return null;
            }
            else
            {
                Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
                object holder;
                userData.GetData(ref guidViewHost, out holder);
                var viewHost = (IWpfTextViewHost)holder;

                return viewHost;
            }
        }

        /// <summary>
        /// Returns process selection range - whole lines!
        /// </summary>
        SnapshotSpan GetSelectionSpan(IWpfTextViewHost viewHost)
        {
            var sel = viewHost.TextView.Selection.StreamSelectionSpan;
            var start = new SnapshotPoint(viewHost.TextView.TextSnapshot, sel.Start.Position).GetContainingLine().Start;
            var end = new SnapshotPoint(viewHost.TextView.TextSnapshot, sel.End.Position).GetContainingLine().End;

            return new SnapshotSpan(start, end);
        }

        private EnvDTE.Document GetActiveDocument()
        {
            EnvDTE.DTE dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            if (dte == null)
                return null;

            return dte.ActiveDocument;
        }

        Uri[] GetIncludeDirectories()
        {
            var document = GetActiveDocument();
            var project = document.ProjectItem.ContainingProject;
            VCProject vcProject = project.Object as VCProject;
            if (vcProject == null)
            {
                Output.Error("The given project is not a VC++ Project");
                return null;
            }
            VCConfiguration activeConfiguration = vcProject.ActiveConfiguration;
            var tools = activeConfiguration.Tools;
            VCCLCompilerTool compilerTool = null;
            foreach (var tool in activeConfiguration.Tools)
            {
                compilerTool = tool as VCCLCompilerTool;
                if (compilerTool != null)
                    break;
            }

            if (compilerTool != null)
            {
                string[] pathStrings = compilerTool.FullIncludePath.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);


                Uri[] pathUris = new Uri[pathStrings.Length];
                for (int i = 0; i < pathStrings.Length; ++i)
                {
                    pathStrings[i] += "/"; // is this safe?
                    pathUris[i] = new Uri(pathStrings[i], UriKind.Absolute);
                }
                return pathUris;
            }
            else
            {
                Output.Error("Couldn't file a VCCLCompilerTool.");
                return null;
            }
        }

        private void FormatPaths(OptionsPage.PathMode pathformat, bool ignoreFileRelative, IncludeLineInfo[] lines)
        {
            if (pathformat == OptionsPage.PathMode.Unchanged)
                return;

            var directories = new List<Uri>();
            var document = GetActiveDocument();
            directories.Add(new Uri(document.Path, UriKind.Absolute));
            directories.AddRange(GetIncludeDirectories());

            foreach (var line in lines)
            {
                if (line.LineType == IncludeLineInfo.Type.NoInclude)
                    continue;

                // Try to resolve include path to an existing file.
                Uri absoluteIncludePath = null;
                foreach (Uri dir in directories)
                {
                    string candidate = Path.Combine(dir.OriginalString, line.IncludeContent);
                    if (File.Exists(candidate))
                    {
                        absoluteIncludePath = new Uri(candidate, UriKind.Absolute);
                        break;
                    }
                }

                // todo: Ignore std library files.
                if (absoluteIncludePath != null)
                {
                    int bestLength = Int32.MaxValue;
                    string bestCandidate = null;

                    int i = ignoreFileRelative ? 1 : 0; // Ignore first one
                    for(; i<directories.Count; ++i)
                    {
                        string raw = directories[i].MakeRelativeUri(absoluteIncludePath).ToString();
                        string proposal = Uri.UnescapeDataString(raw);

                        if (proposal.Length < bestLength)
                        {
                            if (pathformat == OptionsPage.PathMode.Shortest || proposal.IndexOf("../") < 0)
                            {
                                bestCandidate = proposal;
                                bestLength = proposal.Length;
                            }
                        }
                    }

                    if (bestCandidate != null)
                    {
                        line.IncludeContent = bestCandidate;
                    }
                }
            }
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
            var settings = (OptionsPage)package.GetDialogPage(typeof(OptionsPage));

            // Read.
            var viewHost = GetCurrentViewHost();
            var selectionSpan = GetSelectionSpan(viewHost);
            var lines = IncludeLineInfo.ParseIncludes(selectionSpan.GetText(), settings.RemoveEmptyLines);

            // Manipulate paths.
            FormatPaths(settings.PathFormat, settings.IgnoreFileRelative, lines);

            // Format.
            switch (settings.DelimiterFormatting)
            {
                case OptionsPage.DelimiterMode.AngleBrackets:
                    foreach (var line in lines)   
                        line.SetLineType(IncludeLineInfo.Type.IncludeAcute);
                    break;
                case OptionsPage.DelimiterMode.Quotes:
                    foreach (var line in lines)
                        line.SetLineType(IncludeLineInfo.Type.IncludeQuot);
                    break;
            }
            switch (settings.SlashFormatting)
            {
                case OptionsPage.SlashMode.ForwardSlash:
                    foreach (var line in lines)
                        line.IncludeContent = line.IncludeContent.Replace('\\', '/');
                    break;
                case OptionsPage.SlashMode.BackSlash:
                    foreach (var line in lines)
                        line.IncludeContent = line.IncludeContent.Replace('/', '\\');
                    break;
            }

            // Apply changes so far.
            foreach (var line in lines)
                line.UpdateTextWithIncludeContent();

            // Sorting. Ignores non-include lines.
            var comparer = new IncludeComparer(settings.PrecedenceRegexes);
            var sortedIncludes = lines.Where(x => x.LineType != IncludeLineInfo.Type.NoInclude).OrderBy(x => x.IncludeContent, comparer).ToArray();
            int incIdx = 0;
            for (int allIdx = 0; allIdx < lines.Length && incIdx < sortedIncludes.Length; ++allIdx)
            {
                if (lines[allIdx].LineType != IncludeLineInfo.Type.NoInclude)
                {
                    lines[allIdx] = sortedIncludes[incIdx];
                    ++incIdx;
                }
            }


            // Overwrite.
            string replaceText = string.Join(Environment.NewLine, lines.Select(x => x.Text));
            using (var edit = viewHost.TextView.TextBuffer.CreateEdit())
            {
                edit.Replace(selectionSpan, replaceText);
                edit.Apply();
            }
        }
    }
}
