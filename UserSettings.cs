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


namespace NTerm
{
    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        [DisplayName("Current Configuration")]
        [Description("Playing now.")]
        [Browsable(true)]
        [JsonIgnore]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string CurrentConfig { get; set; } = "";

        [DisplayName("Configurations")]
        [Description("All your favorites.")]
        [Browsable(true)]
        public List<Config> Configs { get; set; } = [];

        [DisplayName("Prompt")]
        [Description("CLI prompt.")]
        [Browsable(true)]
        public string Prompt { get; set; } = ">";

        [DisplayName("ANSI Color")]
        [Description("Colorize output.")]
        [Browsable(true)]
        public bool AnsiColor { get; set; } = true;

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("Notification Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;

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
        [Description("Type specific args.\n\"127.0.0.1 59120\"\n\"COM1 9600 E|O|N 6|7|8 0|1|1.5\"")]
        [Browsable(true)]
        public string Args { get; set; } = "???";

        [DisplayName("Hot Keys")]
        [Description("Hot key definitions.\n\"key=command\"")] // like "k=do something"  "o=send me"
        [Browsable(true)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        public List<string> HotKeys { get; set; } = [];

        #region Properties - internal
        [Browsable(false)]
        public uint Id { get; private set; } = (uint)Guid.NewGuid().GetHashCode();
        #endregion
    }
}
