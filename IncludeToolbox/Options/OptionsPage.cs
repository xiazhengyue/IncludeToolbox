using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace IncludeToolbox
{
    /// <summary>
    /// Base class for all option pages.
    /// </summary>
    public abstract class OptionsPage : DialogPage
    {
        // This is only necessary when using Microsoft.VisualStudio.Shell.15.0 which we can't if we want to stay compatible with vs2015
        /*
        /// <summary>
        /// Initializes either with a in place created TaskContext for tests or, if our VS package is acutally active with the standard context.
        /// </summary>
        public OptionsPage() : base(IncludeToolboxPackage.Instance == null ? 
            new Microsoft.VisualStudio.Threading.JoinableTaskContext() : ThreadHelper.JoinableTaskContext)
        { }
        */

        // In theory the whole save/load mechanism should be done automatically.
        // But *something* is or was broken there.
        // see http://stackoverflow.com/questions/32751040/store-array-in-options-using-dialogpage

        static protected WritableSettingsStore GetSettingsStore()
        {
            var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            return settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }

        abstract public override void SaveSettingsToStorage();
        abstract public override void LoadSettingsFromStorage();
    }
}
