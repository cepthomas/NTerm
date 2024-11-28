using System.ComponentModel;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace NTerm
{
    [Serializable]
    public sealed class UserSettings : SettingsCore // TODO which are useful?
    {
        #region Properties - persisted editable
        [DisplayName("Script Path")]
        [Description("Default location for user scripts.")]
        [Browsable(true)]
        public string ScriptPath { get; set; } = "";

        [DisplayName("Open Last File")]
        [Description("Open last file on start.")]
        [Browsable(true)]
        public bool OpenLastFile { get; set; } = true;

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

        [DisplayName("Background Color")]
        [Description("The color used for overall background.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color BackColor { get; set; } = Color.LightYellow;
        #endregion

        #region Properties - internal
        [Browsable(false)]
        public bool WordWrap { get; set; } = false;

        [Browsable(false)]
        public bool MonitorRcv { get; set; } = false;

        [Browsable(false)]
        public bool MonitorSnd { get; set; } = false;
        #endregion
    }
}
