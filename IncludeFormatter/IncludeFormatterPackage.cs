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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

namespace IncludeFormatter
{
    [Guid("0ef7bd3a-65d2-41e6-89af-27ef58296075")]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(OptionsPage), OptionsPage.Category, OptionsPage.SubCategory, 1000, 1001, true)]
    [ProvideProfile(typeof(OptionsPage), OptionsPage.Category, OptionsPage.SubCategory, 1000, 1001, true)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [InstalledProductRegistration("#110", "#112", "0.2", IconResourceID = 400)]
    public sealed class IncludeFormatterPackage : Package
    {
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            IncludeFormatter.Initialize(this);
            base.Initialize();
        }

        #endregion
    }
}
