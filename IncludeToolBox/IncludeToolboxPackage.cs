//------------------------------------------------------------------------------
// <copyright file="IncludeFormatterPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using IncludeToolbox.Commands;
using IncludeViewer;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

namespace IncludeToolbox
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(OptionsPage), OptionsPage.Category, OptionsPage.SubCategory, 1000, 1001, true)]
    [ProvideProfile(typeof(OptionsPage), OptionsPage.Category, OptionsPage.SubCategory, 1000, 1001, true)]
    [ProvideToolWindow(typeof(IncludeViewerToolWindow))]
    [Guid(IncludeToolboxPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [InstalledProductRegistration("#110", "#112", "0.2", IconResourceID = 400)]
    public sealed class IncludeToolboxPackage : Package
    {
        /// <summary>
        /// IncludeToolboxPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "5c2743c4-1b3f-4edd-b6a0-4379f867d47f";

        public IncludeToolboxPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            IncludeParser.Init();
            IncludeViewerToolWindowCommand.Initialize(this);
            FormatIncludes.Initialize(this);
            PurgeIncludes.Initialize(this);
            base.Initialize();            
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            IncludeParser.Exit();
        }

        #endregion
    }
}
