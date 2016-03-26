//------------------------------------------------------------------------------
// <copyright file="IncludeViewerToolWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCCodeModel;

namespace IncludeViewers
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

        private void AddIncludes(ItemCollection target, EnvDTE.CodeElements includes)
        {
            foreach (var elem in includes)
            {
                VCCodeInclude include = elem as VCCodeInclude;
                if (include == null)
                {
                    continue;
                }

                var newItem = new TreeViewItem()
                {
                    Header = include.FullName,
                    ToolTip = include.File,
                    // Todo: Styling should be part of XAML, but there were some exceptions I don't understand yet
                    Foreground = GetSolidBrush(EnvironmentColors.ToolWindowTextBrushKey),
                    Background = GetSolidBrush(EnvironmentColors.DropDownPopupBackgroundEndColorKey),
                    BorderBrush = GetSolidBrush(EnvironmentColors.DropDownPopupBorderBrushKey),
                };

                target.Add(newItem);

                // How to access includes recursively? Is that even possible? How much does VCCodeModel know about those files?
                //AddIncludes(newItem.Items, include.);
            }
        }

        public void SetData(string name, VCFileCodeModel fileCodeModel)
        {
            FileNameLabel.Content = name;

            IncludeTree.Items.Clear();
            AddIncludes(IncludeTree.Items, fileCodeModel.Includes);
        }
    }
}