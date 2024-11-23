using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace NTerm
{
    [Serializable]
    public class Config// : SettingsCore
    {
        #region Properties - persisted editable
        [DisplayName("...")]
        [Description("...")]
        [Browsable(true)]
        public string Protocol { get; set; } = "???";

        public string Host { get; set; } ="???";

        public int Port { get; set; } = 0;

        public bool ShowStatus { get; set; } = false;

        // maybe { "hotkeys": ["space", "ctrl+w"] }
        public string HotKeys { get; set; } = "";

        #endregion

        //     #region Properties - internal
        //     [Browsable(false)]
        //     public bool WordWrap { get; set; } = false;

        //     [Browsable(false)]
        //     public bool MonitorRcv { get; set; } = false;

        //     [Browsable(false)]
        //     public bool MonitorSnd { get; set; } = false;
        //     #endregion
    }
}
