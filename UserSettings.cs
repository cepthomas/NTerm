using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace NTerm
{
    [Serializable]
    public class UserSettings : SettingsCore
    {
        [DisplayName("Current Config")]
        [Description("Playing now.")]
        [Category("NTerm")]
        [Browsable(true)]
        [TypeConverter(typeof(SettingsListConverter))]
        public string CurrentConfig { get; set; } = "";

        [DisplayName("Configs")]
        [Description("All your favorites.")]
        [Category("NTerm")]
        [Browsable(true)]
        public List<Config> Configs { get; set; } = [];

        [DisplayName("Prompt")]
        [Description("CLI prompt.")]
        [Category("NTerm")]
        [Browsable(true)]
        public string Prompt { get; set; } = "# ";

        [DisplayName("Hot Key Mod")]
        [Description("Modifier for your hotkeys.")]
        [Category("NTerm")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public KeyMod HotKeyMod { get; set; } = KeyMod.Ctrl;

        [DisplayName("Meta Marker")]
        [Description("Marker for application functions.")]
        [Category("NTerm")]
        [Browsable(true)]
        public char MetaMarker { get; set; } = '!';

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        [Category("NTerm")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("Notif Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        [Category("NTerm")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Info;
    }

    /// <summary>What are we doing today?</summary>
    [Serializable]
    public sealed class Config
    {
        [DisplayName("Name")]
        [Description("Name")]
        [Category("Config")]
        [Browsable(true)]
        public string Name { get; set; } = "???";

        [DisplayName("Comm Type")]
        [Description("Talk like this.")]
        [Category("Config")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CommType CommType { get; set; } = CommType.Null;

        [DisplayName("Comm Args")]
        [Description("Type specific args. See README.md.")]
        [Category("Config")]
        [Browsable(true)]
        public List<string> Args { get; set; } = [];

        [DisplayName("Resp Time")]
        [Description("Server must reply in msec.")]
        [Browsable(true)]
        public int ResponseTime { get; set; } = 1000;

        [DisplayName("Hot Keys")]
        [Description("Hot key definitions. See README.md.")]
        [Category("Config")]
        [Browsable(true)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        public List<string> HotKeys { get; set; } = [];

        [DisplayName("Matchers")]
        [Description("All the match specs.")]
        [Category("Config")]
        [Browsable(true)]
        public List<Matcher> Matchers { get; set; } = [];
    }

    /// <summary>Spec for one phrase match.</summary>
    [Serializable]
    public sealed class Matcher
    {
        [DisplayName("Text")]
        [Description("Text to match")]
        [Category("Matcher")]
        [Browsable(true)]
        public string Text { get; set; } = "";

        [DisplayName("Fore Color")]
        [Description("Optional color")]
        [Category("Matcher")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConsoleColorEx ForeColor { get; set; } = ConsoleColorEx.None;

        [DisplayName("Back Color")]
        [Description("Optional color")]
        [Category("Matcher")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConsoleColorEx BackColor { get; set; } = ConsoleColorEx.None;
    }

    /// <summary>Converter for providing property options.</summary>
    public class SettingsListConverter : TypeConverter
    {
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
        {
            var settings = (UserSettings)context!.Instance!;
            return context!.PropertyDescriptor!.Name switch
            {
                "CurrentConfig" => new StandardValuesCollection(settings.Configs.Select(x => x.Name).ToList()),
                _ => new StandardValuesCollection(null),
            };
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) { return true; }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) { return true; }
    }
}
