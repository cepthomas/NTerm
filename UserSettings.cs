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
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;


namespace NTerm
{
    [Serializable]
    public class UserSettings : SettingsCore
    {
        [DisplayName("Current Config")]
        [Description("Playing now.")]
        [Browsable(true)]
        [TypeConverter(typeof(SettingsListConverter))]
        public string CurrentConfig { get; set; } = "";

        [DisplayName("Configs")]
        [Description("All your favorites.")]
        [Browsable(true)]
        public List<Config> Configs { get; set; } = [];

        [DisplayName("Prompt")]
        [Description("CLI prompt.")]
        [Browsable(true)]
        public string Prompt { get; set; } = "# ";

        [DisplayName("Hot Key Mod")]
        [Description("Modifier for your hotkeys.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public KeyMod HotKeyMod { get; set; } = KeyMod.Ctrl;

        [DisplayName("Meta Key Mod")]
        [Description("Modifier for application functions ; exit/help/etc.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public KeyMod MetaKeyMod { get; set; } = KeyMod.CtrlShift;

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("Notif Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Info;

        #region Properties - internal
        #endregion
    }

    /// <summary>What are we doing today?</summary>
    [Serializable]
    public sealed class Config //TODO these appear alphabetically.
    {
        [DisplayName("Name")]
        [Description("Name")]
        [Browsable(true)]
        public string Name { get; set; } = "???";

        [DisplayName("Comm Type")]
        [Description("Talk like this.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CommType CommType { get; set; } = CommType.Null;

        [DisplayName("Comm Args")]
        [Description("Type specific args. See README.md.")]
        [Browsable(true)]
        public List<string> Args { get; set; } = [];

        [DisplayName("Resp Time")]
        [Description("Server must connect or reply to commands in msec.")]
        [Browsable(true)]
        public int ResponseTime { get; set; } = 0;

        [DisplayName("Buff Size")]
        [Description("R/W buffer size.")]
        [Browsable(true)]
        public int BufferSize { get; set; } = 4096;

        [DisplayName("Hot Keys")]
        [Description("Hot key definitions. See README.md.")]
        [Browsable(true)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        public List<string> HotKeys { get; set; } = [];

        //[DisplayName("Color Mode")]
        //[Description("Colorize Mode.")]
        //[Browsable(true)]
        //[JsonConverter(typeof(JsonStringEnumConverter))]
        //public ColorMode ColorMode { get; set; } = ColorMode.None;

        [DisplayName("Matchers")]
        [Description("All the match specs.")]
        [Browsable(true)]
        public List<Matcher> Matchers { get; set; } = [];

        #region Properties - internal
        [Browsable(false)]
        public uint Id { get; private set; } = (uint)Guid.NewGuid().GetHashCode();
        #endregion
    }

    /// <summary>Spec for one phrase match.</summary>
    [Serializable]
    public sealed class Matcher
    {
        [DisplayName("Text")]
        [Description("Text to match")]
        [Browsable(true)]
        public string Text { get; set; } = "";

        [DisplayName("Whole Word")]
        [Description("Match whole word")]
        [Browsable(true)]
        public bool WholeWord { get; set; } = false;

        [DisplayName("Whole Line")]
        [Description("Color whole line or just word")]
        [Browsable(true)]
        public bool WholeLine { get; set; } = true;

        [DisplayName("Fore Color")]
        [Description("Optional color")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConsoleColor ForeColor { get; set; } = ConsoleColor.White; // TODO support default/none - special editor, maybe color.

        [DisplayName("Back Color")]
        [Description("Optional color")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConsoleColor BackColor { get; set; } = ConsoleColor.Black;
    }

    /// <summary>Converter for providing property options.</summary>
    public class SettingsListConverter : TypeConverter
    {
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
        {
            var settings = (UserSettings)context!.Instance!;
            switch (context!.PropertyDescriptor!.Name)
            {
                case "CurrentConfig": return new StandardValuesCollection(settings.Configs.Select(x => x.Name).ToList());
                default: return new StandardValuesCollection(null);
            }
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) { return true; }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) { return true; }
    }
}
