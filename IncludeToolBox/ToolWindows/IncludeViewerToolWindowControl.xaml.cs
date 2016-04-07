//------------------------------------------------------------------------------
// <copyright file="IncludeViewerToolWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows.Media;
using IncludeToolbox;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace IncludeViewer
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for IncludeViewerToolWindowControl.
    /// </summary>
    public partial class IncludeViewerToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeViewerToolWindowControl"/> class.
        /// </summary>
        public IncludeViewerToolWindowControl()
        {
            this.InitializeComponent();
        }

        private static Brush GetSolidBrush(ThemeResourceKey themeResourceKey)
        {
            var color = VSColorTheme.GetThemedColor(themeResourceKey);
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        private int numIncludes = 0;
        private HashSet<string> uniqueIncludes = new HashSet<string>();

        private void AddIncludes(ItemCollection target, IEnumerable<IncludeParser.IncludeTreeItem> includes)
        {
            foreach (var elem in includes)
            {
                ++numIncludes;
                uniqueIncludes.Add(elem.Filename);

                var newItem = new TreeViewItem()
                {
                    Header = elem.Filename, // todo: as found first time in include directive
                    ToolTip = elem.Filename,
                    // Todo: Styling should be part of XAML, but there were some exceptions I don't understand yet
                    Foreground = GetSolidBrush(EnvironmentColors.ToolWindowTextBrushKey),
                    Background = GetSolidBrush(EnvironmentColors.DropDownPopupBackgroundEndColorKey),
                    BorderBrush = GetSolidBrush(EnvironmentColors.DropDownPopupBorderBrushKey),
                };

                target.Add(newItem);
                
                if (elem.Children != null)
                    AddIncludes(newItem.Items, elem.Children);
            }
        }

        public void SetData(string name, IncludeParser.IncludeTreeItem treeRoot, int lineCount, int processedLineCount)
        {
            FileNameLabel.Content = name;

            numIncludes = 0;
            uniqueIncludes.Clear();
            IncludeTree.Items.Clear();
            AddIncludes(IncludeTree.Items, treeRoot.Children);

            NumIncludes.Content = numIncludes.ToString();
            NumUniqueIncludes.Content = uniqueIncludes.Count.ToString();
            LocBeforePreProcessor.Content = lineCount.ToString();
            LocAfterPreProcessor.Content = processedLineCount.ToString();
        }
    }
}