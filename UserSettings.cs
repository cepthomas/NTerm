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

//    public sealed class UserSettings : SettingsCore
//
//        ?? valid, 
//        public string CurrentConfig { get; set; } = "";
//
//        ?? current still valid - removed, 
//        public List<Config> Configs { get; set; } = new();
//
//
//        restart:
//        public string Prompt { get; set; } = ">";
//        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;
//        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;
//        dc:
//        public bool OpenLastConfig { get; set; } = true;
//
//
//    public sealed class Config
//        all: reload this config if current
//        public string Name { get; set; } = "???";
//        public CommType CommType { get; set; } = CommType.None;
//        public string Args { get; set; } = "???";
//        public List<string> HotKeyDefs { get; set; } = new();

namespace NTerm
{
    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        [DisplayName("Current Configuration")]
        [Description("Playing now.")]
        [Browsable(true)]
        [JsonIgnore]
        [Editor(typeof(ConfigSelector), typeof(UITypeEditor))]
        public string CurrentConfig { get; set; } = "";

        [DisplayName("Configurations")]
        [Description("All your favorites.")]
        [Browsable(true)]
        //[Editor(typeof(XXXed), typeof(UITypeEditor))]
        public List<Config> Configs { get; set; } = [];

        [DisplayName("Open Last Config")]
        [Description("Open last config on start.")]
        [Browsable(true)]
        public bool OpenLastConfig { get; set; } = true;

        [DisplayName("Prompt")]
        [Description("CLI prompt.")]
        [Browsable(true)]
        //[Editor(typeof(XXXed), typeof(UITypeEditor))]
        public string Prompt { get; set; } = ">";

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        //[Editor(typeof(XXXed), typeof(UITypeEditor))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("Notification Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        //[Editor(typeof(XXXed), typeof(UITypeEditor))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;

        #region Properties - internal


        [JsonIgnore]
        [Browsable(false)]
        public bool Dirty { get; set; }

        /// <summary>Edited flag - restart required.</summary>
        [JsonIgnore]
        [Browsable(false)]
        public bool Restart { get; private set; } = false;

        /// <summary>Edited flag - reload config required.</summary>
        [JsonIgnore]
        [Browsable(false)]
        public bool ReloadConfig { get; private set; } = false;



        /// <summary>The Config.Id.</summary>
        [Browsable(false)]
        public int LastConfig { get; set; } = 0;
        #endregion





        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        //public void Edit()
        //{
        //    PropertyGrid pgMain = new()
        //    {
        //        //Dock = DockStyle.Fill,
        //        PropertySort = PropertySort.NoSort,
        //        SelectedObject = this
        //    };

        //    PropertyGrid pgConfig = new()
        //    {
        //        //Dock = DockStyle.Fill,
        //        PropertySort = PropertySort.NoSort,
        //        //SelectedObject = this
        //    };

        //    using Form f = new()
        //    {
        //        Text = "User Settings",
        //        AutoScaleMode = AutoScaleMode.None,
        //        Location = Cursor.Position,
        //        StartPosition = FormStartPosition.Manual,
        //        FormBorderStyle = FormBorderStyle.SizableToolWindow,
        //        ShowIcon = false,
        //        ShowInTaskbar = false
        //    };
        //    f.ClientSize = new(450, 600); // must do after construction
        //    f.Controls.Add(pgMain);
        //    pgMain.ExpandAllGridItems();

        //    // Detect changes of interest. TODO more smart
        //    pgMain.PropertyValueChanged += (sdr, args) =>
        //    {
        //        var name = args.ChangedItem!.PropertyDescriptor!.Name;
        //        var cat = args.ChangedItem!.PropertyDescriptor!.Category;
        //        Console.WriteLine($"+++ PropertyValueChanged {name} {cat}");
        //        //Dirty = true;
        //    };

        //    f.ShowDialog();
        //}
    }

    /// <summary>What are we doing today?</summary>
    [Serializable]
    public sealed class Config
    {
        [DisplayName("Name")]
        [Description("Name")]
        [Browsable(true)]
        //[Editor(typeof(XXXed), typeof(UITypeEditor))]
        public string Name { get; set; } = "???";

        [DisplayName("Communication Type")]
        [Description("Talk like this.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Browsable(true)]
        //[Editor(typeof(XXXed), typeof(UITypeEditor))]
        public CommType CommType { get; set; } = CommType.None;

        [DisplayName("Communication Arguments")] // TODO could get fancier later.
        [Description("Type specific args.\n\"127.0.0.1 59120\"\n\"COM1 9600 E-O-N 6-7-8 0-1-1.5\"")]
        [Browsable(true)]
        //[Editor(typeof(XXXed), typeof(UITypeEditor))]
        public string Args { get; set; } = "???";

        //[DisplayName("Encoding")]
        //[Description("Encoding.")]
        //[JsonConverter(typeof(JsonStringEnumConverter))]
        //[Browsable(true)]
        //public Encoding Encoding { get; set; } = Encoding.UTF8; TODO? not an enum

        [DisplayName("Hot Keys")]
        [Description("Hot key definitions.\n\"key=command\"")] // like "k=do something"  "o=send me"
        [Browsable(true)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        //[Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        //public string HotKeyDefs { get; set; } = "";
        public List<string> HotKeyDefs { get; set; } = [];

        #region Properties - internal
        [JsonIgnore]
        [Browsable(false)]
        public bool Dirty { get; set; }

        [JsonIgnore]
        [Browsable(false)]
        public Dictionary<string, string> HotKeys { get { return _hotKeys; } }
        Dictionary<string, string> _hotKeys = [];
        #endregion

        public int Id { get; private set; }

        public Config()
        {
            Id = Guid.NewGuid().GetHashCode();
        }
    }

//    public static class UtilsNot
//    {
//        /// <summary>
//        /// Edit the common options in a property grid.
//        /// </summary>
//        public static void EditSettings(UserSettings settings)
//        {
//            PropertyGrid pg = new()
//            {
//                Dock = DockStyle.Fill,
//                //PropertySort = PropertySort.Categorized,
//                SelectedObject = settings
//            };

//            using Form f = new()
//            {
//                Text = "User Settings",
//                AutoScaleMode = AutoScaleMode.None,
//                Location = Cursor.Position,
//                StartPosition = FormStartPosition.Manual,
//                FormBorderStyle = FormBorderStyle.SizableToolWindow,
//                ShowIcon = false,
//                ShowInTaskbar = false
//            };
//            f.ClientSize = new(450, 600); // do after construction
//            f.Controls.Add(pg);
//            pg.ExpandAllGridItems();

//            // Detect changes of interest. TODO more smart
//            pg.PropertyValueChanged += (sdr, args) =>
//            {
//                var name = args.ChangedItem!.PropertyDescriptor!.Name;
//                var cat = args.ChangedItem!.PropertyDescriptor!.Category;
//                Console.WriteLine($"+++ PropertyValueChanged {name} {cat}");
////                Dirty = true;
//            };

//            f.ShowDialog();
//        }
//    }



    ///// <summary>Generic property editor for lists of strings.</summary>
    //public class StringListEditor : UITypeEditor
    //{
    //    IWindowsFormsEditorService? _service = null;

    //    public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
    //    {
    //        List<string> ls = value is null ? new() : (List<string>)value;

    //        TextBox tb = new()
    //        {
    //            Multiline = true,
    //            ReadOnly = false,
    //            AcceptsReturn = true,
    //            ScrollBars = ScrollBars.Both,
    //            Height = 100,
    //            Text = string.Join(Environment.NewLine, ls)
    //        };
    //        tb.Select(0, 0);

    //        _service = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
    //        _service?.DropDownControl(tb);
    //        ls = tb.Text.SplitByToken(Environment.NewLine);

    //        return ls;
    //    }

    //    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { return UITypeEditorEditStyle.DropDown; }
    //}

    ///// <summary>Select config.</summary>
    //public class ConfigSelector : UITypeEditor
    //{
    //    IWindowsFormsEditorService? _service = null;

    //    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }

    //    public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
    //    {
    //        var name = (string)value!;
    //        _service = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
    //        var settings = (UserSettings)context!.Instance!;
    //        var ls = settings.Configs.Select(x => x.Name).ToList();
    //        var ind = ls.IndexOf(name);

    //        var lb = new ListBox { SelectionMode = SelectionMode.One };
    //        ls.ForEach(s => { lb.Items.Add(s); });
    //        lb.SelectedIndex = ind < 0 ? 0 : ind;
    //        lb.SelectedIndexChanged += (_, __) =>
    //        {
    //            value = lb.SelectedItem!.ToString();
    //            _service!.CloseDropDown();
    //        };

    //        // Drop the list control.
    //        _service!.DropDownControl(lb);
    //        // Done.
    //        return value;
    //    }
    //}

    //public class XXXed : UITypeEditor
    //{
    //    IWindowsFormsEditorService? _service = null;

    //    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }

    //    public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
    //    {
    //        Console.WriteLine($"+++ EditValue {value} {context.Instance.GetType()}");
    //        return value;
    //    }
    //}
}
