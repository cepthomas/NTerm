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
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;


namespace NTerm
{
    [Serializable]
    public class UserSettings : SettingsCore
    {
        [DisplayName("Current Configuration")]
        [Description("Playing now.")]
        [Browsable(true)]
        [TypeConverter(typeof(SettingsListConverter))]
        public string CurrentConfig { get; set; } = "";

        [DisplayName("Configurations")]
        [Description("All your favorites.")]
        [Browsable(true)]
        public List<Config> Configs { get; set; } = [];

        //[DisplayName("Prompt")]
        //[Description("CLI prompt.")]
        //[Browsable(true)]
        //public string Prompt { get; set; } = ">";

        [DisplayName("Color Mode")] // TODOF probably per config
        [Description("Colorize Mode.")]
        [Browsable(true)]
        public ColorMode ColorMode { get; set; } = ColorMode.None;

        [DisplayName("Matchers")] // TODOF probably per config
        [Description("All the match specs.")]
        [Browsable(true)]
        public List<Matcher> Matchers { get; set; } = [];

        [DisplayName("Back Color")]
        [Description("Colorize controls.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color BackColor { get; set; } = Color.SeaShell;

        [DisplayName("Monospace Font")]
        [Description("Select Monospace Font.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonFontConverter))]
        [Editor(typeof(MonospaceFontEditor), typeof(UITypeEditor))]
        public Font Font { get; set; } = new("Cascadia Code", 8);

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("Notification Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Info;

        #region Properties - internal
        #endregion
    }

    /// <summary>What are we doing today?</summary>
    [Serializable]
    public sealed class Config
    {
        [DisplayName("Name")]
        [Description("Name")]
        [Browsable(true)]
        public string Name { get; set; } = "???";

        [DisplayName("Communication Type")]
        [Description("Talk like this.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Browsable(true)]
        public CommType CommType { get; set; } = CommType.Null;

        [DisplayName("Communication Arguments")]
        [Description("Type specific args. See README.md.")]
        [Browsable(true)]
        public List<string> Args { get; set; } = [];

        [DisplayName("Communication Mode")]
        [Description("Block or poll.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Browsable(true)]
        public CommMode CommMode { get; set; } = CommMode.CmdResp;

        [DisplayName("Hot Keys")]
        [Description("Hot key definitions. See README.md.")]
        [Browsable(true)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        public List<string> HotKeys { get; set; } = [];

        [DisplayName("Response Time")]
        [Description("Server must connect or reply to commands in msec.")]
        [Browsable(true)]
        public int ResponseTime { get; set; } = 0;

        [DisplayName("Buffer Size")]
        [Description("R/W buffer size.")]
        [Browsable(true)]
        public int BufferSize { get; set; } = 4096;

        #region Properties - internal
        [Browsable(false)]
        public uint Id { get; private set; } = (uint)Guid.NewGuid().GetHashCode();
        #endregion
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
