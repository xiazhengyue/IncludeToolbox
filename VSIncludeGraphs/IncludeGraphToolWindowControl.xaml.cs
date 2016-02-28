//------------------------------------------------------------------------------
// <copyright file="IncludeGraphToolWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;

namespace VSIncludeGraphs
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for IncludeGraphToolWindowControl.
    /// </summary>
    public partial class IncludeGraphToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeGraphToolWindowControl"/> class.
        /// </summary>
        public IncludeGraphToolWindowControl()
        {
            this.InitializeComponent();
        }

        public void SetData(string fileName)
        {
            FileNameLabel.Content = fileName;
        }
    }
}