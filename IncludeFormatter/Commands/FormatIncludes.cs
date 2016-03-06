using System;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

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

            // Format.
            switch (settings.DelimiterFormatting)
            {
                case OptionsPage.DelimiterMode.Acutes:
                    foreach (var line in lines)   
                        line.SetLineType(IncludeLineInfo.Type.IncludeAcute);
                    break;
                case OptionsPage.DelimiterMode.Quotations:
                    foreach (var line in lines)
                        line.SetLineType(IncludeLineInfo.Type.IncludeQuot);
                    break;
            }
            switch (settings.SlashFormatting)
            {
                case OptionsPage.SlashMode.ForwardSlash:
                    foreach (var line in lines)
                        line.ReplaceIncludeContent(line.IncludeContent.Replace('\\', '/'));
                    break;
                case OptionsPage.SlashMode.BackSlash:
                    foreach (var line in lines)
                        line.ReplaceIncludeContent(line.IncludeContent.Replace('/', '\\'));
                    break;
            }


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
