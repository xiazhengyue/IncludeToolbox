using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace IncludeFormatter
{
    /// <summary>
    /// Option page.
    /// Settings are automatically persisted, since this class is registered with ProvideProfile at the package.
    /// </summary>
    [Guid("B822F53B-32C0-4560-9A84-2F9DA7AB0E4C")]
    public class OptionsPage : DialogPage
    {
        public const string Category = "Include Formatter";
        public const string SubCategory = "General";
        private const string collectionName = "IncludeFormatter";

        public enum DelimiterMode
        {
            Unchanged,
            Acutes,
            Quotations,
        }
        [Category("Formatting")]
        [DisplayName("Delimiter Mode")]
        [Description("Changes all delimiters to the given type.")]
        public DelimiterMode DelimiterFormatting { get; set; } = DelimiterMode.Unchanged;

        public enum SlashMode
        {
            Unchanged,
            ForwardSlash,
            BackSlash,
        }
        [Category("Formatting")]
        [DisplayName("Slash Mode")]
        [Description("Changes all slashes to the given type.")]
        public SlashMode SlashFormatting { get; set; } = SlashMode.ForwardSlash;

        [Category("Sorting")]
        [DisplayName("Precedence Regexes")]
        [Description("Earlier match means higher sorting priority.")]
        public string[] PrecedenceRegexes {
            get { return precedenceRegexes; }
            set { precedenceRegexes = value.Where(x => x.Length > 0).ToArray(); } // Remove empty lines.
        }
        private string[] precedenceRegexes = new string[0];


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

            var value = string.Join("\n", PrecedenceRegexes);
            settingsStore.SetString(collectionName, nameof(PrecedenceRegexes), value);
            settingsStore.SetInt32(collectionName, nameof(DelimiterFormatting), (int)DelimiterFormatting);
            settingsStore.SetInt32(collectionName, nameof(SlashFormatting), (int)SlashFormatting);
        }

        public override void LoadSettingsFromStorage()
        {
            var settingsStore = GetSettingsStore();


            if (settingsStore.PropertyExists(collectionName, nameof(PrecedenceRegexes)))
            {
                var value = settingsStore.GetString(collectionName, nameof(PrecedenceRegexes));
                PrecedenceRegexes = value.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries);
            }

            if (settingsStore.PropertyExists(collectionName, nameof(DelimiterFormatting)))
                DelimiterFormatting = (DelimiterMode) settingsStore.GetInt32(collectionName, nameof(DelimiterFormatting));

            if (settingsStore.PropertyExists(collectionName, nameof(SlashFormatting)))
                SlashFormatting = (SlashMode)settingsStore.GetInt32(collectionName, nameof(SlashFormatting));
        }
    }
}
