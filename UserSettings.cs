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
        [DisplayName("Prompt")]
        [Description("CLI prompt.")]
        [Category("NTerm")]
        [Browsable(true)]
        public string Prompt { get; set; } = "# ";

        [DisplayName("Meta Marker")]
        [Description("Indicator for application functions.")]
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

        [DisplayName("Err Color")]
        [Description("Color for error notifications")]
        [Category("NTerm")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConsoleColorEx ErrColor { get; set; } = ConsoleColorEx.Red;

        [DisplayName("Int Color")]
        [Description("Color for internal notifications")]
        [Category("NTerm")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConsoleColorEx IntColor { get; set; } = ConsoleColorEx.Green;
    }
}
