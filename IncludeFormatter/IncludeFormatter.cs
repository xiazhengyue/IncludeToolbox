using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IncludeFormatter
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class IncludeFormatter
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("aef3a531-8af4-4b7b-800a-e32503dfc6e2");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeFormatter"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private IncludeFormatter(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static IncludeFormatter Instance
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
            Instance = new IncludeFormatter(package);
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

        struct LineInfo
        {
            public enum Type
            {
                INCLUDE_QUOT,
                INCLUDE_ACUTE,
                NO_INCLUDE
            }

            public void UpdateTextFromIncludeContent()
            {
                Text.Remove(Delimiter0 + 1, Delimiter1 - Delimiter0 - 1);
                Text.Insert(Delimiter0, IncludeContent);
            }

            public Type LineType;
            public string Text;
            public string IncludeContent;
            public int Delimiter0, Delimiter1;
        }

        private LineInfo[] ParseSelection(string selection)
        {
            var selectedCodeLines = selection.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var outInfo = new LineInfo[selectedCodeLines.Length];

            // Simplistic parsing.
            // "//" comments are intentionally ignored
            // Todo: Handle multi line comments gracefully
            for (int line = 0; line < selectedCodeLines.Length; ++line)
            {
                outInfo[line].Text = selectedCodeLines[line];
                outInfo[line].LineType = LineInfo.Type.NO_INCLUDE;

                int occurence = selectedCodeLines[line].IndexOf("#include");
                if (occurence == -1)
                    continue;

                outInfo[line].Delimiter0 = selectedCodeLines[line].IndexOf('\"', occurence + "#include".Length);
                if (outInfo[line].Delimiter0 == -1)
                {
                    outInfo[line].Delimiter0 = selectedCodeLines[line].IndexOf('<', occurence + "#include".Length);
                    if (outInfo[line].Delimiter0 == -1)
                        continue;
                    outInfo[line].Delimiter1 = selectedCodeLines[line].IndexOf('>', outInfo[line].Delimiter0 + 1);
                    outInfo[line].LineType = LineInfo.Type.INCLUDE_ACUTE;
                }
                else
                {
                    outInfo[line].Delimiter1 = selectedCodeLines[line].IndexOf('\"', outInfo[line].Delimiter0 + 1);
                    outInfo[line].LineType = LineInfo.Type.INCLUDE_QUOT;
                }
                if (outInfo[line].Delimiter1 == -1)
                    continue;

                outInfo[line].IncludeContent = selectedCodeLines[line].Substring(outInfo[line].Delimiter0 + 1, outInfo[line].Delimiter1 - outInfo[line].Delimiter0 - 1);
            }

            return outInfo;
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

        public class IncludeComparer : IComparer<string>
        {
            public IncludeComparer(string[] precedenceRegexes)
            {
                this.precedenceRegexes = precedenceRegexes;
            }

            private readonly string[] precedenceRegexes;

            public int Compare(string lineA, string lineB)
            {
                int precedenceA = 0;
                for (; precedenceA < precedenceRegexes.Length; ++precedenceA)
                {
                    if (Regex.Match(lineA, precedenceRegexes[precedenceA]).Success)
                        break;
                }
                int precedenceB = 0;
                for (; precedenceB < precedenceRegexes.Length; ++precedenceB)
                {
                    if (Regex.Match(lineB, precedenceRegexes[precedenceB]).Success)
                        break;
                }

                if (precedenceA == precedenceB)
                    return lineA.CompareTo(lineB);
                else
                    return precedenceA.CompareTo(precedenceB);
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
            // Read.
            var viewHost = GetCurrentViewHost();
            var selectionSpan = GetSelectionSpan(viewHost);
            var lines = ParseSelection(selectionSpan.GetText());


            // Format.
            // First means, higher sorting importance.
            var precedenceRegexes = new string[]
            {
                @"^YourSpecialFolder(/|\\)",
            };

            var comparer = new IncludeComparer();
            lines = lines.OrderBy(x => x.IncludeContent, comparer).ToArray();

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
