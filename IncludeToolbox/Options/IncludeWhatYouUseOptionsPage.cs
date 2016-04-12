using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace IncludeToolbox
{
    /// <summary>
    /// Option page.
    /// Settings are automatically persisted, since this class is registered with ProvideProfile at the package.
    /// </summary>
    [Guid("69CFD797-2E2B-497E-9231-334BCDC41407")]
    public class IncludeWhatYouUseOptionsPage : DialogPage
    {
        public const string SubCategory = "Include-What-You-Use";
        private const string collectionName = "IncludeFormatter";

        [Category("iwyu options")]
        [DisplayName("Log Verbosity")]
        [Description("The higher the level, the more output. A level lower 1 might disable automatic include replacing (--verbose)")]
        public int LogVerbosity { get; set; } = 2;

        [Category("iwyu options")]
        [DisplayName("Mapping File")]
        [Description("Gives iwyu a mapping file. (--mapping_file)")]
        public string[] MappingFiles { get; set; } = new string[0];

        [Category("iwyu options")]
        [DisplayName("No Default Mappings")]
        [Description("Do not add iwyu's default mappings. (--no_default_mappings)")]
        public bool NoDefaultMappings { get; set; } = false;

        [Category("iwyu options")]
        [DisplayName("PCH in Code")]
        [Description("Mark the first include in a translation unit as a precompiled header. Use to prevent IWYU from removing necessary PCH includes. Though Clang forces PCHs to be listed as prefix headers, the PCH in code pattern can be used with GCC and is standard practice on MSVC (e.g.stdafx.h). (--pch_in_code)")]
        public bool PCHInCode { get; set; } = true;

        public enum PrefixHeaderMode
        {
            Add,
            Keep,
            Remove
        }
        [Category("iwyu options")]
        [DisplayName("Prefix Header Include Mode")]
        [Description("Tells iwyu what to do with in-source includes and forward declarations involving prefix headers. Prefix header is a file included via command-line option -include. If prefix header makes include or forward declaration obsolete, presence of such include can be controlled with the following values:\nAdd: new lines are added\nKeep: new lines aren't added, existing are kept intact\nRemove: new lines aren't added, existing are removed. (--prefix_header_includes)")]
        public PrefixHeaderMode PrefixHeaderIncludes { get; set; } = PrefixHeaderMode.Add;

        [Category("iwyu options")]
        [DisplayName("Transitive Includes Only")]
        [Description("Do not suggest that a file add foo.h unless foo.h is already visible in the file's transitive includes. (--transitive_includes_only)")]
        public bool TransitiveIncludesOnly { get; set; } = true;


        // In theory the whole save/load mechanism should be done automatically.
        // Bute *something* is broken there.
        // see http://stackoverflow.com/questions/32751040/store-array-in-options-using-dialogpage


        private WritableSettingsStore GetSettingsStore()
        {
            var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            return settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }

        public override void SaveSettingsToStorage()
        {
            var settingsStore = GetSettingsStore();

            if (!settingsStore.CollectionExists(collectionName))
                settingsStore.CreateCollection(collectionName);

            settingsStore.SetInt32(collectionName, nameof(LogVerbosity), LogVerbosity);

            var value = string.Join("\n", MappingFiles);
            settingsStore.SetString(collectionName, nameof(MappingFiles), value);

            settingsStore.SetBoolean(collectionName, nameof(NoDefaultMappings), NoDefaultMappings);
            settingsStore.SetBoolean(collectionName, nameof(PCHInCode), PCHInCode);
            settingsStore.SetInt32(collectionName, nameof(PrefixHeaderIncludes), (int)PrefixHeaderIncludes);
            settingsStore.SetBoolean(collectionName, nameof(TransitiveIncludesOnly), TransitiveIncludesOnly);
        }

        public override void LoadSettingsFromStorage()
        {
            var settingsStore = GetSettingsStore();

            if (settingsStore.PropertyExists(collectionName, nameof(LogVerbosity)))
                LogVerbosity = settingsStore.GetInt32(collectionName, nameof(LogVerbosity));
            if (settingsStore.PropertyExists(collectionName, nameof(MappingFiles)))
            {
                var value = settingsStore.GetString(collectionName, nameof(MappingFiles));
                MappingFiles = value.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            }
            if (settingsStore.PropertyExists(collectionName, nameof(NoDefaultMappings)))
                NoDefaultMappings = settingsStore.GetBoolean(collectionName, nameof(NoDefaultMappings));
            if (settingsStore.PropertyExists(collectionName, nameof(PCHInCode)))
                PCHInCode = settingsStore.GetBoolean(collectionName, nameof(PCHInCode));
            if (settingsStore.PropertyExists(collectionName, nameof(PrefixHeaderIncludes)))
                PrefixHeaderIncludes = (PrefixHeaderMode)settingsStore.GetInt32(collectionName, nameof(PrefixHeaderIncludes));
            if (settingsStore.PropertyExists(collectionName, nameof(TransitiveIncludesOnly)))
                TransitiveIncludesOnly = settingsStore.GetBoolean(collectionName, nameof(TransitiveIncludesOnly));
        }
    }
}
